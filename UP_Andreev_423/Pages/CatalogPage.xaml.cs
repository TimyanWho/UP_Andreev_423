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
    public partial class CatalogPage : Page
    {
        public CatalogPage()
        {
            InitializeComponent();
            Loaded += CatalogPage_Loaded;
        }

        private void CatalogPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBooks();
        }

        private void LoadBooks()
        {
            var books = Core.Context.Books.ToList();
            var users = Core.Context.Users.ToList();
            var reviews = Core.Context.Reviews.ToList();

            var items = books.Select(b =>
            {
                int bookId = GetInt(b, "BookId", "Id");
                int authorId = GetInt(b, "AuthorId", "UserId", "OwnerId", "Author");

                string title = GetString(b, "Title", "Name");
                string coverPath = GetString(b, "CoverPath", "Cover", "ImagePath");
                string authorName = ResolveAuthorName(users, authorId);
                string genreText = ResolveGenres(b);
                double rating = ResolveRating(reviews, bookId);

                return new BookCardItem
                {
                    BookId = bookId,
                    Title = title,
                    Author = authorName,
                    Genres = string.IsNullOrWhiteSpace(genreText) ? "Жанры не указаны" : genreText,
                    RatingText = $"Рейтинг: {rating:0.00}",
                    CoverImage = LoadImage(coverPath)
                };
            }).ToList();

            BooksItems.ItemsSource = items;
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

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int bookId))
            {
                var shell = Window.GetWindow(this) as ShellWindow;
                shell?.NavigateToBook(bookId);
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

        public class BookCardItem
        {
            public int BookId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Genres { get; set; }
            public string RatingText { get; set; }
            public ImageSource CoverImage { get; set; }
        }
    }
}