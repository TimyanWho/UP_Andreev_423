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
    public partial class CatalogPage : Page
    {
        private List<BookCard> _allBooks = new List<BookCard>();

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

            _allBooks = books.Select(b =>
            {
                int bookId = DbUtil.Int(b, "BookId", "Id");
                int authorId = DbUtil.Int(b, "AuthorId", "UserId", "OwnerId", "Author");

                string authorName = users
                    .FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == authorId)
                    is object authorObj
                    ? DbUtil.Str(authorObj, "DisplayName", "Name", "FullName", "Nickname", "Login")
                    : "Неизвестно";

                string cover = DbUtil.Str(b, "CoverPath", "Cover", "ImagePath");
                string title = DbUtil.Str(b, "Title", "Name");
                string genres = ResolveGenres(b);
                double rating = ResolveRating(reviews, bookId);

                return new BookCard
                {
                    BookId = bookId,
                    Title = title,
                    Author = authorName,
                    Genres = string.IsNullOrWhiteSpace(genres) ? "Жанры не указаны" : genres,
                    RatingText = $"Рейтинг: {rating:0.00}",
                    CoverPath = cover
                };
            }).ToList();

            BooksItems.ItemsSource = _allBooks.Select(x => new
            {
                x.BookId,
                x.Title,
                x.Author,
                x.Genres,
                x.RatingText,
                CoverImage = LoadImage(x.CoverPath)
            }).ToList();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string q = SearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(q))
            {
                LoadBooks();
                return;
            }

            BooksItems.ItemsSource = _allBooks
                .Where(b => b.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            b.Author.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(x => new
                {
                    x.BookId,
                    x.Title,
                    x.Author,
                    x.Genres,
                    x.RatingText,
                    CoverImage = LoadImage(x.CoverPath)
                })
                .ToList();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            LoadBooks();
        }

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int bookId)
            {
                var shell = Window.GetWindow(this) as ShellWindow;
                shell?.NavigateToBook(bookId);
            }
        }

        private void AddToList_Click(object sender, RoutedEventArgs e)
        {
            if (!(Application.Current.Properties["CurrentUser"] is Users currentUser))
            {
                MessageBox.Show("Сначала войдите в аккаунт.");
                return;
            }

            if (sender is Button btn && btn.Tag is int bookId)
            {
                var lists = Core.Context.ReadingLists.ToList();
                var entry = lists.FirstOrDefault(x =>
                    DbUtil.Int(x, "UserId", "OwnerId") == DbUtil.Int(currentUser, "UserId", "Id") &&
                    DbUtil.Int(x, "BookId", "IdBook") == bookId);

                if (entry == null)
                {
                    entry = new ReadingLists();
                    DbUtil.Set(entry, DbUtil.Int(currentUser, "UserId", "Id"), "UserId", "OwnerId");
                    DbUtil.Set(entry, bookId, "BookId", "IdBook");
                    DbUtil.Set(entry, "В планах", "ListState", "Status");
                    DbUtil.Set(entry, DateTime.Now, "AddedAt", "CreatedAt");
                    Core.Context.ReadingLists.Add(entry);
                }
                else
                {
                    DbUtil.Set(entry, "В планах", "ListState", "Status");
                }

                Core.Context.SaveChanges();
                MessageBox.Show("Книга добавлена в список 'В планах'.");
            }
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

        private double ResolveRating(IEnumerable<object> reviews, int bookId)
        {
            var values = reviews
                .Where(r => DbUtil.Int(r, "BookId", "IdBook") == bookId)
                .Select(r => (double?)GetDouble(DbUtil.Get(r, "Rating", "Score")))
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();

            return values.Count == 0 ? 0 : values.Average();
        }

        private static double GetDouble(object value)
        {
            if (value == null) return 0;

            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch
            {
                try { return Convert.ToDouble(value); }
                catch { return 0; }
            }
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

        private class BookCard
        {
            public int BookId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Genres { get; set; }
            public string RatingText { get; set; }
            public string CoverPath { get; set; }
        }
    }
}