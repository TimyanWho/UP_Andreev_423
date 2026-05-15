using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UP_Andreev_423.Pages
{
    public partial class ListsPage : Page
    {
        private List<ListBookCard> _allItems = new List<ListBookCard>();

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
            var readingLists = Core.Context.ReadingLists.ToList();
            var books = Core.Context.Books.ToList();
            var users = Core.Context.Users.ToList();
            var reviews = Core.Context.Reviews.ToList();

            var query = readingLists.Select(rl =>
            {
                int listId = GetInt(rl, "ReadingListId", "Id");
                int bookId = GetInt(rl, "BookId", "IdBook");
                int userId = GetInt(rl, "UserId", "OwnerId");

                var book = books.FirstOrDefault(b => GetInt(b, "BookId", "Id") == bookId);
                if (book == null)
                    return null;

                string status = GetString(rl, "Status", "ListStatus", "Type");
                if (string.IsNullOrWhiteSpace(status))
                    status = "В планах";

                string title = GetString(book, "Title", "Name");
                string author = ResolveAuthorName(users, GetInt(book, "AuthorId", "UserId", "OwnerId", "Author"));
                string genreText = ResolveGenres(book);
                double rating = ResolveRating(reviews, bookId);

                return new ListBookCard
                {
                    EntryId = listId,
                    BookId = bookId,
                    Title = title,
                    Author = author,
                    Genres = string.IsNullOrWhiteSpace(genreText) ? "Жанры не указаны" : genreText,
                    RatingText = $"Рейтинг: {rating:0.00}",
                    Status = status,
                    CoverImage = LoadImage(GetString(book, "CoverPath", "Cover", "ImagePath"))
                };
            })
            .Where(x => x != null)
            .ToList();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(x =>
                    x.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.Author.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            _allItems = query;

            AbandonedItems.ItemsSource = _allItems.Where(x => string.Equals(x.Status, "Заброшено", StringComparison.OrdinalIgnoreCase)).ToList();
            PlanItems.ItemsSource = _allItems.Where(x => string.Equals(x.Status, "В планах", StringComparison.OrdinalIgnoreCase)).ToList();
            ReadingItems.ItemsSource = _allItems.Where(x => string.Equals(x.Status, "Читаю", StringComparison.OrdinalIgnoreCase)).ToList();
            FinishedItems.ItemsSource = _allItems.Where(x => string.Equals(x.Status, "Прочитано", StringComparison.OrdinalIgnoreCase)).ToList();
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
            if (sender is Button btn && btn.Tag is ListBookCard card)
            {
                var shell = Window.GetWindow(this) as ShellWindow;
                shell?.NavigateToBook(card.BookId);
            }
        }

        private void MoveStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ListBookCard card)
            {
                var entry = Core.Context.ReadingLists.ToList()
                    .FirstOrDefault(x => GetInt(x, "ReadingListId", "Id") == card.EntryId);

                if (entry == null)
                    return;

                string next = NextStatus(card.Status);
                SetValue(entry, next, "Status", "ListStatus", "Type");
                Core.Context.SaveChanges();

                LoadCards(SearchBox.Text.Trim());
            }
        }

        private static string NextStatus(string current)
        {
            if (string.Equals(current, "Заброшено", StringComparison.OrdinalIgnoreCase))
                return "В планах";
            if (string.Equals(current, "В планах", StringComparison.OrdinalIgnoreCase))
                return "Читаю";
            if (string.Equals(current, "Читаю", StringComparison.OrdinalIgnoreCase))
                return "Прочитано";
            return "Заброшено";
        }

        private static string ResolveAuthorName(IEnumerable<object> users, int authorId)
        {
            var user = users.FirstOrDefault(u => GetInt(u, "UserId", "Id", "ID") == authorId);
            return GetString(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
        }

        private static string ResolveGenres(object book)
        {
            var nav = GetValue(book, "Genres", "Genre", "BookGenres", "Genres1");
            if (nav is IEnumerable enumerable)
            {
                var names = new List<string>();

                foreach (var item in enumerable)
                {
                    string name = GetString(item, "GenreName", "Name", "Title");
                    if (!string.IsNullOrWhiteSpace(name))
                        names.Add(name);
                }

                if (names.Count > 0)
                    return string.Join(", ", names);
            }

            return GetString(book, "GenresText", "GenreText", "GenreName");
        }

        private static double ResolveRating(IEnumerable<object> reviews, int bookId)
        {
            var values = reviews
                .Where(r => GetInt(r, "BookId", "IdBook") == bookId)
                .Select(r => (double?)GetDouble(r, "Rating", "Score"))
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            return values.Count == 0 ? 0 : values.Average();
        }

        private static ImageSource LoadImage(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                string fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                    return null;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private static object GetValue(object obj, params string[] names)
        {
            if (obj == null) return null;

            foreach (var name in names)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                    return prop.GetValue(obj);
            }

            return null;
        }

        private static string GetString(object obj, params string[] names)
        {
            return GetValue(obj, names)?.ToString() ?? string.Empty;
        }

        private static int GetInt(object obj, params string[] names)
        {
            var value = GetValue(obj, names);
            if (value == null) return 0;

            try { return Convert.ToInt32(value); }
            catch { return 0; }
        }

        private static double GetDouble(object obj, params string[] names)
        {
            var value = GetValue(obj, names);
            if (value == null) return 0;

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                try { return Convert.ToDouble(value); }
                catch { return 0; }
            }
        }

        private static void SetValue(object obj, object value, params string[] names)
        {
            if (obj == null)
                return;

            foreach (var name in names)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        if (value == null)
                        {
                            prop.SetValue(obj, null);
                            return;
                        }

                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        prop.SetValue(obj, Convert.ChangeType(value, targetType));
                        return;
                    }
                    catch
                    {
                    }
                }
            }
        }

        public class ListBookCard
        {
            public int EntryId { get; set; }
            public int BookId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Genres { get; set; }
            public string RatingText { get; set; }
            public string Status { get; set; }
            public ImageSource CoverImage { get; set; }
        }
    }
}