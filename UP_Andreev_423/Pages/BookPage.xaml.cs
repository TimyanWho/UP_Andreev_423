using System;
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
        }

        private void LoadBook()
        {
            var book = Core.Context.Books
                .ToList()
                .FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == _bookId);

            if (book == null)
                return;

            var users = Core.Context.Users.ToList();
            var reviews = Core.Context.Reviews.ToList();

            int authorId = DbUtil.Int(book, "AuthorId", "UserId", "OwnerId", "Author");
            string authorName = ResolveAuthorName(users, authorId);

            TitleText.Text = DbUtil.Str(book, "Title", "Name");
            AuthorText.Text = "Автор: " + authorName;
            GenreText.Text = "Жанры: " + ResolveGenres(book);
            RatingText.Text = string.Format("Рейтинг: {0:0.00}", ResolveRating(reviews, _bookId));
            DescriptionText.Text = DbUtil.Str(book, "Description", "Desc", "BookDescription");
            ContentPreviewText.Text = DbUtil.Str(book, "ContentText", "TextContent", "BookText", "Content");

            CoverImage.Source = LoadImage(DbUtil.Str(book, "CoverPath", "Cover", "ImagePath"));

            var reviewCards = reviews
                .Where(r => DbUtil.Int(r, "BookId", "IdBook") == _bookId)
                .Select(r => new ReviewCard
                {
                    ReviewId = DbUtil.Int(r, "ReviewId", "Id"),
                    User = ResolveUserName(users, DbUtil.Int(r, "UserId", "ReviewerId", "AuthorId")),
                    Rating = "Оценка: " + DbUtil.Int(r, "Rating", "Score"),
                    Text = DbUtil.Str(r, "ReviewText", "Text", "Comment"),
                    CreatedAt = DbUtil.Str(r, "CreatedAt", "DateCreated")
                })
                .ToList();

            ReviewsItems.ItemsSource = reviewCards;
        }

        private void Read_Click(object sender, RoutedEventArgs e)
        {
            var shell = Window.GetWindow(this) as ShellWindow;
            if (shell != null)
                shell.NavigateToReader(_bookId);
        }

        private void AddReview_Click(object sender, RoutedEventArgs e)
        {
            int rating;
            if (!int.TryParse(ReviewRatingBox.Text.Trim(), out rating) || rating < 1 || rating > 10)
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

            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
            {
                MessageBox.Show("Сначала войдите в аккаунт.");
                return;
            }

            int userId = DbUtil.Int(currentUser, "UserId", "Id");
            if (userId == 0)
            {
                MessageBox.Show("Не удалось определить пользователя.");
                return;
            }

            var review = new Reviews();
            DbUtil.Set(review, _bookId, "BookId", "IdBook");
            DbUtil.Set(review, userId, "UserId", "ReviewerId", "AuthorId");
            DbUtil.Set(review, text, "ReviewText", "Text", "Comment");
            DbUtil.Set(review, rating, "Rating", "Score");
            DbUtil.Set(review, false, "IsFrozen", "Frozen", "Blocked");
            DbUtil.Set(review, DateTime.Now, "CreatedAt", "DateCreated");

            try
            {
                Core.Context.Reviews.Add(review);
                Core.Context.SaveChanges();

                ReviewTextBox.Clear();
                ReviewRatingBox.Text = "10";
                LoadBook();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException != null && ex.InnerException.InnerException != null
                    ? ex.InnerException.InnerException.Message
                    : ex.Message, "Ошибка добавления отзыва");
            }
        }

        private void ReportReview_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || btn.Tag == null)
            {
                MessageBox.Show("Не удалось определить отзыв.");
                return;
            }

            var review = btn.Tag as ReviewCard;
            if (review == null)
            {
                MessageBox.Show("Не удалось определить отзыв.");
                return;
            }

            MessageBox.Show("Жалоба на отзыв #" + review.ReviewId + " будет добавлена позже.");
        }

        private string ResolveAuthorName(IEnumerable<object> users, int authorId)
        {
            var user = users.FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id", "ID") == authorId);
            return DbUtil.Str(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
        }

        private string ResolveUserName(IEnumerable<object> users, int userId)
        {
            var user = users.FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id", "ID") == userId);
            return DbUtil.Str(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
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

        private double ResolveRating(IEnumerable<object> reviews, int bookId)
        {
            var values = reviews
                .Where(r => DbUtil.Int(r, "BookId", "IdBook") == bookId)
                .Select(r =>
                {
                    var raw = DbUtil.Get(r, "Rating", "Score");
                    if (raw == null) return 0d;

                    try
                    {
                        return Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        try
                        {
                            return Convert.ToDouble(raw);
                        }
                        catch
                        {
                            return 0d;
                        }
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

        public class ReviewCard
        {
            public int ReviewId { get; set; }
            public string User { get; set; }
            public string Rating { get; set; }
            public string Text { get; set; }
            public string CreatedAt { get; set; }
        }
    }
}