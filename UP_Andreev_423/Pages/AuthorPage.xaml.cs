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
    public partial class AuthorPage : Page
    {
        private List<AuthorBookCard> _books = new List<AuthorBookCard>();

        public AuthorPage()
        {
            InitializeComponent();
            Loaded += AuthorPage_Loaded;
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

        private void LoadBooks()
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
                return;

            int userId = DbUtil.Int(currentUser, "UserId", "Id");
            var reviews = Core.Context.Reviews.ToList();

            _books = Core.Context.Books.ToList()
                .Where(b => DbUtil.Int(b, "AuthorId", "UserId", "OwnerId", "Author") == userId)
                .Select(b => new AuthorBookCard
                {
                    BookId = DbUtil.Int(b, "BookId", "Id"),
                    Title = DbUtil.Str(b, "Title", "Name"),
                    CoverPath = DbUtil.Str(b, "CoverPath", "Cover", "ImagePath"),
                    StatusText = DbUtil.Bool(b, "IsFrozen", "Frozen", "Blocked") ? "Заморожена" : "Опубликована",
                    FreezeText = DbUtil.Str(b, "FreezeReason", "Reason"),
                    Rating = ResolveRating(reviews, DbUtil.Int(b, "BookId", "Id"))
                })
                .ToList();

            foreach (var item in _books)
                item.RatingText = string.Format("Рейтинг: {0:0.00}", item.Rating);

            AuthorBooksItems.ItemsSource = _books;
        }

        private void AddBook_Click(object sender, RoutedEventArgs e)
        {
            var shell = Window.GetWindow(this) as ShellWindow;
            if (shell != null)
                shell.Navigate(new BookEditPage());
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            LoadBooks();
        }

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as AuthorBookCard;
            if (card == null) return;

            var shell = Window.GetWindow(this) as ShellWindow;
            if (shell != null)
                shell.NavigateToBook(card.BookId);
        }

        private void EditBook_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as AuthorBookCard;
            if (card == null) return;

            var shell = Window.GetWindow(this) as ShellWindow;
            if (shell != null)
                shell.Navigate(new BookEditPage(card.BookId));
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

        private class AuthorBookCard
        {
            public int BookId { get; set; }
            public string Title { get; set; }
            public string CoverPath { get; set; }
            public string StatusText { get; set; }
            public string FreezeText { get; set; }
            public double Rating { get; set; }
            public string RatingText { get; set; }
            public ImageSource CoverImage
            {
                get { return LoadImage(CoverPath); }
            }
        }
    }
}