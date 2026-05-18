using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class AuthorPage : Page
    {
        private List<AuthorBookCard> _books = new List<AuthorBookCard>();

        public AuthorPage()
        {
            InitializeComponent();
            Loaded += AuthorPage_Loaded;
            LoadGenres();
        }

        private void AuthorPage_Loaded(object sender, RoutedEventArgs e)
        {
            string roleName = RoleNames.Normalize(Application.Current.Properties["RoleName"] as string);
            if (roleName != RoleNames.Author)
            {
                MessageBox.Show("Страница автора доступна только автору.");
                return;
            }
            LoadBooks();
        }

        private void LoadGenres()
        {
            GenresListBox.ItemsSource = Core.Context.Genres.ToList();
        }

        private void LoadBooks()
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null) return;

            int userId = DbUtil.Int(currentUser, "UserId", "Id");
            var reviews = Core.Context.Reviews.ToList();

            _books = Core.Context.Books.ToList()
                .Where(b => DbUtil.Int(b, "AuthorUserId", "AuthorId", "UserId", "OwnerId", "Author") == userId)
                .Select(b => new AuthorBookCard
                {
                    BookId = DbUtil.Int(b, "BookId", "Id"),
                    Title = DbUtil.Str(b, "Title", "Name"),
                    CoverEmoji = GetEmoji(DbUtil.Str(b, "CoverImagePath", "CoverPath", "Cover", "ImagePath")),
                    StatusText = DbUtil.Bool(b, "IsFrozen", "Frozen", "Blocked") ? "Заморожена" : "Опубликована",
                    FreezeText = DbUtil.Str(b, "FreezeReason", "Reason"),
                    Rating = ResolveRating(reviews, DbUtil.Int(b, "BookId", "Id"))
                }).ToList();

            foreach (var item in _books)
                item.RatingText = $"Рейтинг: {item.Rating:0.00}";

            AuthorBooksItems.ItemsSource = _books;
        }

        private string GetEmoji(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Length > 2)
                return "📘";
            return raw;
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            CreateForm.Visibility = Visibility.Visible;
        }

        private void CancelCreate_Click(object sender, RoutedEventArgs e)
        {
            CreateForm.Visibility = Visibility.Collapsed;
        }

        private void SaveBook_Click(object sender, RoutedEventArgs e)
        {
            if (!(Application.Current.Properties["CurrentUser"] is Users currentUser))
            {
                MessageBox.Show("Вы не авторизованы.");
                return;
            }

            string title = TitleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Введите название.");
                return;
            }

            int currentUserId = DbUtil.Int(currentUser, "UserId", "Id");
            if (currentUserId <= 0)
            {
                MessageBox.Show("Ошибка идентификации пользователя.");
                return;
            }

            var userExists = Core.Context.Users.ToList().Any(u => DbUtil.Int(u, "UserId", "Id") == currentUserId);
            if (!userExists)
            {
                MessageBox.Show("Пользователь не найден в базе.");
                return;
            }

            var newBook = new Books();
            DbUtil.Set(newBook, title, "Title");
            DbUtil.Set(newBook, DescriptionBox.Text.Trim(), "Description");
            DbUtil.Set(newBook, ContentBox.Text.Trim(), "TextContent");
            string emoji = EmojiBox.Text.Trim();
            DbUtil.Set(newBook, string.IsNullOrEmpty(emoji) ? "📘" : emoji, "CoverImagePath", "CoverPath", "Cover");
            DbUtil.Set(newBook, currentUserId, "AuthorUserId");
            DbUtil.Set(newBook, 0, "Rating");
            DbUtil.Set(newBook, false, "IsFrozen");
            DbUtil.Set(newBook, DateTime.Now, "PublishedAt");

            Core.Context.Books.Add(newBook);
            Core.Context.SaveChanges();

            int newBookId = DbUtil.Int(newBook, "BookId", "Id");

            foreach (var genreObj in GenresListBox.SelectedItems)
            {
                int genreId = DbUtil.Int(genreObj, "GenreId");
                var bg = new BookGenres();
                DbUtil.Set(bg, newBookId, "BookId");
                DbUtil.Set(bg, genreId, "GenreId");
                Core.Context.BookGenres.Add(bg);
            }

            Core.Context.SaveChanges();

            MessageBox.Show("Книга создана.");
            CreateForm.Visibility = Visibility.Collapsed;
            LoadBooks();

            var shell = Window.GetWindow(this) as ShellWindow;
            shell?.NavigateToBook(newBookId);
        }

        private void Reload_Click(object sender, RoutedEventArgs e) => LoadBooks();

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int bookId)
            {
                var shell = Window.GetWindow(this) as ShellWindow;
                shell?.NavigateToBook(bookId);
            }
        }

        private void EditBook_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int bookId)
            {
                var shell = Window.GetWindow(this) as ShellWindow;
                shell?.Navigate(new BookEditPage(bookId));
            }
        }

        private double ResolveRating(IEnumerable<object> reviews, int bookId)
        {
            var values = reviews
                .Where(r => DbUtil.Int(r, "BookId", "IdBook") == bookId)
                .Select(r => GetDouble(DbUtil.Get(r, "Rating", "Score")))
                .ToList();
            return values.Count == 0 ? 0 : values.Average();
        }

        private static double GetDouble(object raw)
        {
            if (raw == null) return 0;
            try { return Convert.ToDouble(raw, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private class AuthorBookCard
        {
            public int BookId { get; set; }
            public string Title { get; set; }
            public string CoverEmoji { get; set; }
            public string StatusText { get; set; }
            public string FreezeText { get; set; }
            public double Rating { get; set; }
            public string RatingText { get; set; }
        }
    }
}