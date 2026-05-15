using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UP_Andreev_423.Pages
{
    public partial class ListsPage : Page
    {
        private List<ListCard> _allCards = new List<ListCard>();

        public ListsPage()
        {
            InitializeComponent();
            Loaded += ListsPage_Loaded;
        }

        private void ListsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCards();
        }

        private void LoadCards(string filter = null)
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
                return;

            int currentUserId = DbUtil.Int(currentUser, "UserId", "Id");

            var source = Core.Context.ReadingLists.ToList();

            _allCards = source
                .Where(x => DbUtil.Int(x, "UserId", "OwnerId") == currentUserId)
                .Select(rl =>
                {
                    int bookId = DbUtil.Int(rl, "BookId", "IdBook");
                    var book = Core.Context.Books.FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == bookId);
                    if (book == null)
                        return null;

                    int authorId = DbUtil.Int(book, "AuthorId", "UserId", "OwnerId", "Author");
                    var author = Core.Context.Users.FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == authorId);

                    string authorName = author != null
                        ? DbUtil.Str(author, "DisplayName", "Name", "FullName", "Nickname", "Login")
                        : "Неизвестно";

                    string genres = ResolveGenres(book);
                    double rating = ResolveRating(bookId);

                    return new ListCard
                    {
                        EntryId = DbUtil.Int(rl, "ReadingListId", "Id"),
                        BookId = bookId,
                        Title = DbUtil.Str(book, "Title", "Name"),
                        Author = authorName,
                        Genres = string.IsNullOrWhiteSpace(genres) ? "Жанры не указаны" : genres,
                        RatingText = $"Рейтинг: {rating:0.00}",
                        Status = DbUtil.Str(rl, "ListState", "Status"),
                        CoverPath = DbUtil.Str(book, "CoverPath", "Cover", "ImagePath")
                    };
                })
                .Where(x => x != null)
                .ToList();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                _allCards = _allCards
                    .Where(x => x.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                x.Author.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            AbandonedItems.ItemsSource = _allCards.Where(x => x.Status == "Заброшено").Select(ToView).ToList();
            PlanItems.ItemsSource = _allCards.Where(x => x.Status == "В планах").Select(ToView).ToList();
            ReadingItems.ItemsSource = _allCards.Where(x => x.Status == "Читаю").Select(ToView).ToList();
            FinishedItems.ItemsSource = _allCards.Where(x => x.Status == "Прочитано").Select(ToView).ToList();
        }

        private object ToView(ListCard x)
        {
            return new
            {
                x.BookId,
                x.EntryId,
                x.Title,
                x.Author,
                x.Genres,
                x.RatingText,
                x.Status,
                CoverImage = LoadImage(x.CoverPath)
            };
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            LoadCards(SearchBox.Text.Trim());
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            LoadCards();
        }

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int bookId = DbUtil.Int(btn.Tag, "BookId");
                var shell = Window.GetWindow(this) as ShellWindow;
                shell?.NavigateToBook(bookId);
            }
        }

        private void MoveStatus_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
                return;

            int currentUserId = DbUtil.Int(currentUser, "UserId", "Id");

            if (sender is Button btn && btn.Tag != null)
            {
                int bookId = DbUtil.Int(btn.Tag, "BookId");
                var entry = Core.Context.ReadingLists.FirstOrDefault(x =>
                    DbUtil.Int(x, "UserId", "OwnerId") == currentUserId &&
                    DbUtil.Int(x, "BookId", "IdBook") == bookId);

                if (entry == null)
                    return;

                string next = NextState(DbUtil.Str(entry, "ListState", "Status"));
                DbUtil.Set(entry, next, "ListState", "Status");
                Core.Context.SaveChanges();

                LoadCards(SearchBox.Text.Trim());
            }
        }

        private static string NextState(string current)
        {
            if (current == "Заброшено") return "В планах";
            if (current == "В планах") return "Читаю";
            if (current == "Читаю") return "Прочитано";
            return "Заброшено";
        }

        private string ResolveGenres(object book)
        {
            var nav = DbUtil.Items(book, "Genres", "Genre", "BookGenres");
            if (nav != null)
            {
                var names = new List<string>();
                foreach (var item in nav)
                {
                    string name = DbUtil.Str(item, "GenreName", "Name", "Title");
                    if (!string.IsNullOrWhiteSpace(name))
                        names.Add(name);
                }

                if (names.Count > 0)
                    return string.Join(", ", names);
            }

            return DbUtil.Str(book, "GenresText", "GenreText", "GenreName");
        }

        private double ResolveRating(int bookId)
        {
            var values = Core.Context.Reviews
                .ToList()
                .Where(r => DbUtil.Int(r, "BookId", "IdBook") == bookId)
                .Select(r =>
                {
                    var v = DbUtil.Get(r, "Rating", "Score");
                    if (v == null) return 0d;

                    try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); }
                    catch
                    {
                        try { return Convert.ToDouble(v); }
                        catch { return 0d; }
                    }
                })
                .ToList();

            return values.Count == 0 ? 0 : values.Average();
        }

        private static ImageSource LoadImage(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return null;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private class ListCard
        {
            public int EntryId { get; set; }
            public int BookId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Genres { get; set; }
            public string RatingText { get; set; }
            public string Status { get; set; }
            public string CoverPath { get; set; }
        }
    }
}