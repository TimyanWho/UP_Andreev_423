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
    public partial class BookPage : Page
    {
        private readonly int _bookId;

        public BookPage(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            Loaded += BookPage_Loaded;
        }

        private void BookPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBook();

            string role = Application.Current.Properties.Contains("Role")
                ? Application.Current.Properties["Role"]?.ToString() ?? ""
                : "";

            AdminFreezeButton.Visibility = role.IndexOf("админ", StringComparison.OrdinalIgnoreCase) >= 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void LoadBook()
        {
            var book = Core.Context.Books.ToList()
                .FirstOrDefault(b => GetInt(b, "BookId", "Id") == _bookId);

            if (book == null)
                return;

            var users = Core.Context.Users.ToList();
            var reviews = Core.Context.Reviews.ToList();

            int authorId = GetInt(book, "AuthorId", "UserId", "OwnerId", "Author");
            string author = ResolveAuthorName(users, authorId);

            TitleText.Text = GetString(book, "Title", "Name");
            AuthorText.Text = $"Автор: {author}";
            GenreText.Text = $"Жанры: {ResolveGenres(book)}";
            RatingText.Text = $"Рейтинг: {ResolveRating(reviews, _bookId):0.00}";
            DescriptionText.Text = GetString(book, "Description", "Desc", "BookDescription");
            ContentPreviewText.Text = GetString(book, "ContentText", "TextContent", "BookText", "Content");
            CoverImage.Source = LoadImage(GetString(book, "CoverPath", "Cover", "ImagePath"));

            ReviewsGrid.ItemsSource = reviews
                .Where(r => GetInt(r, "BookId", "IdBook") == _bookId)
                .Select(r => new
                {
                    User = ResolveUserName(users, GetInt(r, "UserId", "ReviewerId", "AuthorId")),
                    Rating = GetInt(r, "Rating", "Score"),
                    Text = GetString(r, "ReviewText", "Text", "Comment"),
                    CreatedAt = GetString(r, "CreatedAt", "DateCreated")
                })
                .ToList();
        }

        private void Read_Click(object sender, RoutedEventArgs e)
        {
            var shell = Window.GetWindow(this) as ShellWindow;
            shell?.NavigateToReader(_bookId);
        }

        private void AddReview_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ReviewRatingBox.Text.Trim(), out int rating) || rating < 1 || rating > 10)
            {
                MessageBox.Show("Оценка должна быть от 1 до 10.");
                return;
            }

            string text = ReviewTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Введите текст отзыва.");
                return;
            }

            if (!(Application.Current.Properties["CurrentUser"] is object currentUser))
            {
                MessageBox.Show("Пользователь не найден.");
                return;
            }

            int userId = GetInt(currentUser, "UserId", "Id", "ID");
            if (userId == 0)
            {
                MessageBox.Show("Не удалось определить пользователя.");
                return;
            }

            var review = new Reviews();
            SetValue(review, _bookId, "BookId", "IdBook");
            SetValue(review, userId, "UserId", "ReviewerId", "AuthorId");
            SetValue(review, text, "ReviewText", "Text", "Comment");
            SetValue(review, rating, "Rating", "Score");
            SetValue(review, false, "IsFrozen", "Frozen", "Blocked");
            SetValue(review, DateTime.Now, "CreatedAt", "DateCreated");

            Core.Context.Reviews.Add(review);
            Core.Context.SaveChanges();

            ReviewTextBox.Clear();
            ReviewRatingBox.Text = "10";
            LoadBook();
        }

        private static string ResolveAuthorName(IEnumerable<object> users, int authorId)
        {
            var user = users.FirstOrDefault(u => GetInt(u, "UserId", "Id", "ID") == authorId);
            return GetString(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
        }

        private static string ResolveUserName(IEnumerable<object> users, int userId)
        {
            var user = users.FirstOrDefault(u => GetInt(u, "UserId", "Id", "ID") == userId);
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
    }
}