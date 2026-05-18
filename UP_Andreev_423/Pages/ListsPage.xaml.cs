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
            LoadGenres();
            LoadCards();
            ApplyFilters();
        }

        private void LoadGenres()
        {
            var genreNames = new List<string> { "Все жанры" };

            foreach (var g in Core.Context.Genres.ToList())
            {
                string name = DbUtil.Str(g, "GenreName", "Name", "Title");
                if (!string.IsNullOrWhiteSpace(name) && !genreNames.Contains(name))
                    genreNames.Add(name);
            }

            GenreBox.ItemsSource = genreNames;
            GenreBox.SelectedIndex = 0;
            SortBox.SelectedIndex = 0;
        }

        private void LoadCards()
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
                return;

            int currentUserId = DbUtil.Int(currentUser, "UserId", "Id");
            var books = Core.Context.Books.ToList();
            var users = Core.Context.Users.ToList();
            var reviews = Core.Context.Reviews.ToList();

            _allCards = Core.Context.ReadingLists.ToList()
                .Where(x => DbUtil.Int(x, "UserId", "OwnerId") == currentUserId)
                .Select(rl =>
                {
                    int bookId = DbUtil.Int(rl, "BookId", "IdBook");
                    var book = books.FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == bookId);
                    if (book == null)
                        return null;

                    int authorId = DbUtil.Int(book, "AuthorId", "UserId", "OwnerId", "Author");
                    var author = users.FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == authorId);

                    string authorName = author != null
                        ? DbUtil.Str(author, "DisplayName", "Name", "FullName", "Nickname", "Login")
                        : "Неизвестно";

                    string genres = ResolveGenres(book);
                    double rating = ResolveRating(reviews, bookId);
                    string status = DbUtil.Str(rl, "ListState", "Status");

                    return new ListCard
                    {
                        EntryId = DbUtil.Int(rl, "ReadingListId", "Id"),
                        BookId = bookId,
                        Title = DbUtil.Str(book, "Title", "Name"),
                        Author = authorName,
                        Genres = string.IsNullOrWhiteSpace(genres) ? "Жанры не указаны" : genres,
                        Rating = rating,
                        RatingText = string.Format("Рейтинг: {0:0.00}", rating),
                        Status = status,
                        CoverPath = DbUtil.Str(book, "CoverPath", "Cover", "ImagePath"),
                        CoverImage = LoadImage(DbUtil.Str(book, "CoverPath", "Cover", "ImagePath"))
                    };
                })
                .Where(x => x != null)
                .ToList();
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<ListCard> query = _allCards;

            string search = SearchBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.Title.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    b.Author.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            string selectedGenre = GenreBox.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedGenre) && selectedGenre != "Все жанры")
            {
                query = query.Where(b => b.Genres.IndexOf(selectedGenre, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            switch (SortBox.SelectedIndex)
            {
                case 1:
                    query = query.OrderByDescending(b => b.Rating).ThenBy(b => b.Title);
                    break;
                default:
                    query = query.OrderBy(b => b.Title);
                    break;
            }

            var list = query.ToList();

            AbandonedItems.ItemsSource = list.Where(x => x.Status == "Заброшено").ToList();
            PlanItems.ItemsSource = list.Where(x => x.Status == "В планах").ToList();
            ReadingItems.ItemsSource = list.Where(x => x.Status == "Читаю").ToList();
            FinishedItems.ItemsSource = list.Where(x => x.Status == "Прочитано").ToList();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            GenreBox.SelectedIndex = 0;
            SortBox.SelectedIndex = 0;
            ApplyFilters();
        }

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as ListCard;
            if (card == null) return;

            var shell = Window.GetWindow(this) as ShellWindow;
            if (shell != null)
                shell.NavigateToBook(card.BookId);
        }

        private void MoveStatus_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
                return;

            var button = sender as Button;
            if (button == null || button.DataContext == null)
                return;

            var card = button.DataContext as ListCard;
            if (card == null)
                return;

            string newStatus = button.Tag as string;
            if (string.IsNullOrWhiteSpace(newStatus))
                return;

            var entry = Core.Context.ReadingLists.ToList().FirstOrDefault(x =>
                DbUtil.Int(x, "UserId", "OwnerId") == DbUtil.Int(currentUser, "UserId", "Id") &&
                DbUtil.Int(x, "BookId", "IdBook") == card.BookId);

            if (entry == null)
                return;

            DbUtil.Set(entry, newStatus, "ListState", "Status");

            try
            {
                Core.Context.SaveChanges();
                LoadCards();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException != null && ex.InnerException.InnerException != null
                    ? ex.InnerException.InnerException.Message
                    : ex.Message, "Ошибка");
            }
        }

        private string ResolveGenres(object book)
        {
            var nav = DbUtil.Items(book, "Genres", "Genre", "BookGenres", "Genres1");
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

        private double ResolveRating(IEnumerable<Reviews> reviews, int bookId)
        {
            var values = reviews
                .Where(r => DbUtil.Int(r, "BookId", "IdBook") == bookId)
                .Select(r =>
                {
                    object raw = DbUtil.Get(r, "Rating", "Score");
                    if (raw == null) return 0d;

                    try { return Convert.ToDouble(raw, CultureInfo.InvariantCulture); }
                    catch
                    {
                        try { return Convert.ToDouble(raw); }
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
            public double Rating { get; set; }
            public string RatingText { get; set; }
            public string Status { get; set; }
            public string CoverPath { get; set; }
            public ImageSource CoverImage { get; set; }
        }
    }
}