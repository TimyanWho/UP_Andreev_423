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
            LoadGenres();
            LoadBooks();
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

        private void LoadBooks()
        {
            var books = Core.Context.Books.ToList();
            var users = Core.Context.Users.ToList();
            var reviews = Core.Context.Reviews.ToList();

            _allBooks = books.Select(b =>
            {
                int bookId = DbUtil.Int(b, "BookId", "Id");
                int authorId = DbUtil.Int(b, "AuthorId", "UserId", "OwnerId", "Author");

                var author = users.FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == authorId);

                string title = DbUtil.Str(b, "Title", "Name");
                string coverPath = DbUtil.Str(b, "CoverPath", "Cover", "ImagePath");
                string authorName = author != null
                    ? DbUtil.Str(author, "DisplayName", "Name", "FullName", "Nickname", "Login")
                    : "Неизвестно";

                string genres = ResolveGenres(b);
                double rating = ResolveRating(reviews, bookId);

                return new BookCard
                {
                    BookId = bookId,
                    Title = title,
                    Author = authorName,
                    Genres = string.IsNullOrWhiteSpace(genres) ? "Жанры не указаны" : genres,
                    Rating = rating,
                    RatingText = string.Format("Рейтинг: {0:0.00}", rating),
                    CoverPath = coverPath,
                    CoverImage = LoadImage(coverPath)
                };
            }).ToList();
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<BookCard> query = _allBooks;

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

            BooksItems.ItemsSource = query.ToList();
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
            var card = (sender as Button)?.Tag as BookCard;
            if (card == null) return;

            var shell = Window.GetWindow(this) as ShellWindow;
            if (shell != null)
                shell.NavigateToBook(card.BookId);
        }

        private void AddToList_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
            {
                MessageBox.Show("Сначала войдите в аккаунт.");
                return;
            }

            var button = sender as Button;
            var card = button != null ? button.DataContext as BookCard : null;
            if (card == null) return;

            string state = button.Tag as string;
            if (string.IsNullOrWhiteSpace(state))
                state = "В планах";

            var entry = Core.Context.ReadingLists.ToList().FirstOrDefault(x =>
                DbUtil.Int(x, "UserId", "OwnerId") == DbUtil.Int(currentUser, "UserId", "Id") &&
                DbUtil.Int(x, "BookId", "IdBook") == card.BookId);

            if (entry == null)
            {
                entry = new ReadingLists();
                DbUtil.Set(entry, DbUtil.Int(currentUser, "UserId", "Id"), "UserId", "OwnerId");
                DbUtil.Set(entry, card.BookId, "BookId", "IdBook");
                DbUtil.Set(entry, state, "ListState", "Status");
                DbUtil.Set(entry, DateTime.Now, "AddedAt", "CreatedAt");
                Core.Context.ReadingLists.Add(entry);
            }
            else
            {
                DbUtil.Set(entry, state, "ListState", "Status");
            }

            try
            {
                Core.Context.SaveChanges();
                MessageBox.Show("Сохранено в списке: " + state);
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

        private class BookCard
        {
            public int BookId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Genres { get; set; }
            public double Rating { get; set; }
            public string RatingText { get; set; }
            public string CoverPath { get; set; }
            public ImageSource CoverImage { get; set; }
        }
    }
}