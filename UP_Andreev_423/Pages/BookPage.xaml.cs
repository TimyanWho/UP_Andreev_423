using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class BookPage : Page
    {
        private int _bookId;

        public BookPage(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            Loaded += BookPage_Loaded;
        }

        private void BookPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBook();
            LoadReviews();
        }

        private void LoadBook()
        {
            var book = Core.Context.Books.ToList().FirstOrDefault(b => DbUtil.Int(b, "BookId") == _bookId);
            if (book == null)
            {
                MessageBox.Show("Книга не найдена.");
                return;
            }

            string cover = DbUtil.Str(book, "CoverImagePath", "CoverPath", "Cover", "ImagePath");
            CoverEmojiBlock.Text = string.IsNullOrEmpty(cover) || cover.Length > 2 ? "📘" : cover;
            TitleBlock.Text = DbUtil.Str(book, "Title");
            DescriptionBlock.Text = DbUtil.Str(book, "Description");
            RatingBlock.Text = $"Рейтинг: {DbUtil.Get(book, "Rating")}";

            int authorId = DbUtil.Int(book, "AuthorUserId");
            var users = Core.Context.Users.ToList();
            var author = users.FirstOrDefault(u => DbUtil.Int(u, "UserId") == authorId);
            AuthorBlock.Text = author != null ? DbUtil.Str(author, "FullName", "Login") : "Неизвестно";

            string genres = ResolveGenres(book);
            GenresBlock.Text = string.IsNullOrWhiteSpace(genres) ? "Жанры не указаны" : genres;

            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser != null)
            {
                int currentUserId = DbUtil.Int(currentUser, "UserId");
                bool isAuthor = authorId == currentUserId;
                bool isAdmin = DbUtil.Str(currentUser, "RoleName") == "Admin";

                EditButton.Visibility = (isAuthor || isAdmin) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                EditButton.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadReviews()
        {
            var reviews = Core.Context.Reviews.ToList().Where(r => DbUtil.Int(r, "BookId") == _bookId).ToList();
            var users = Core.Context.Users.ToList();

            ReviewsItems.ItemsSource = reviews.Select(r =>
            {
                int userId = DbUtil.Int(r, "UserId");
                var user = users.FirstOrDefault(u => DbUtil.Int(u, "UserId") == userId);
                string userName = user != null ? DbUtil.Str(user, "FullName", "Login") : "Аноним";
                int rating = Convert.ToInt32(DbUtil.Get(r, "Rating"));
                return new
                {
                    ReviewId = DbUtil.Int(r, "ReviewId"),
                    UserDisplay = userName,
                    RatingDisplay = "★ " + rating + " / 10",
                    Text = DbUtil.Str(r, "ReviewText")
                };
            }).ToList();
        }

        private string ResolveGenres(object book)
        {
            var nav = DbUtil.Items(book, "BookGenres");
            if (nav != null)
            {
                var names = nav.Cast<object>().Select(bg => {
                    int genreId = DbUtil.Int(bg, "GenreId");
                    var genreObj = Core.Context.Genres.ToList().FirstOrDefault(g => DbUtil.Int(g, "GenreId") == genreId);
                    return genreObj != null ? DbUtil.Str(genreObj, "GenreName") : null;
                }).Where(n => n != null);
                return string.Join(", ", names);
            }
            return null;
        }

        private void Read_Click(object sender, RoutedEventArgs e)
        {
            var shell = Window.GetWindow(this) as ShellWindow;
            shell?.NavigateToBook(_bookId);
        }

        private void AddToList_Click(object sender, RoutedEventArgs e)
        {
            if (!(Application.Current.Properties["CurrentUser"] is Users currentUser))
            {
                MessageBox.Show("Сначала войдите в аккаунт.");
                return;
            }

            var lists = Core.Context.ReadingLists.ToList();
            int currentUserId = DbUtil.Int(currentUser, "UserId");
            var entry = lists.FirstOrDefault(x =>
                DbUtil.Int(x, "UserId") == currentUserId &&
                DbUtil.Int(x, "BookId") == _bookId);

            if (entry == null)
            {
                entry = new ReadingLists();
                DbUtil.Set(entry, currentUserId, "UserId");
                DbUtil.Set(entry, _bookId, "BookId");
                DbUtil.Set(entry, "В планах", "ListState");
                DbUtil.Set(entry, DateTime.Now, "AddedAt");
                Core.Context.ReadingLists.Add(entry);
            }
            else
            {
                DbUtil.Set(entry, "В планах", "ListState");
            }

            Core.Context.SaveChanges();
            MessageBox.Show("Книга добавлена в список 'В планах'.");
        }

        private void ComplainBook_Click(object sender, RoutedEventArgs e)
        {
            if (!(Application.Current.Properties["CurrentUser"] is Users currentUser))
            {
                MessageBox.Show("Сначала войдите в аккаунт.");
                return;
            }

            string reason = ShowInputDialog("Укажите причину жалобы на книгу:");
            if (string.IsNullOrWhiteSpace(reason)) return;

            var complaint = new Complaints();
            DbUtil.Set(complaint, DbUtil.Int(currentUser, "UserId"), "ComplainerUserId");
            DbUtil.Set(complaint, _bookId, "TargetBookId");
            DbUtil.Set(complaint, null, "TargetReviewId");
            DbUtil.Set(complaint, reason, "Reason");
            DbUtil.Set(complaint, "Новая", "Status");
            DbUtil.Set(complaint, DateTime.Now, "CreatedAt");
            Core.Context.Complaints.Add(complaint);
            Core.Context.SaveChanges();

            MessageBox.Show("Жалоба отправлена.");
        }

        private void ComplainReview_Click(object sender, RoutedEventArgs e)
        {
            if (!(Application.Current.Properties["CurrentUser"] is Users currentUser))
            {
                MessageBox.Show("Сначала войдите в аккаунт.");
                return;
            }

            if (sender is Button btn && btn.Tag is int reviewId)
            {
                string reason = ShowInputDialog("Укажите причину жалобы на отзыв:");
                if (string.IsNullOrWhiteSpace(reason)) return;

                var complaint = new Complaints();
                DbUtil.Set(complaint, DbUtil.Int(currentUser, "UserId"), "ComplainerUserId");
                DbUtil.Set(complaint, null, "TargetBookId");
                DbUtil.Set(complaint, reviewId, "TargetReviewId");
                DbUtil.Set(complaint, reason, "Reason");
                DbUtil.Set(complaint, "Новая", "Status");
                DbUtil.Set(complaint, DateTime.Now, "CreatedAt");
                Core.Context.Complaints.Add(complaint);
                Core.Context.SaveChanges();

                MessageBox.Show("Жалоба на отзыв отправлена.");
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция редактирования пока не реализована.");
        }

        private string ShowInputDialog(string prompt)
        {
            Window window = new Window
            {
                Title = "Ввод",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.Margin = new Thickness(10);

            TextBlock promptText = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(promptText, 0);
            grid.Children.Add(promptText);

            TextBox inputBox = new TextBox { Margin = new Thickness(0, 0, 0, 8), Height = 24 };
            Grid.SetRow(inputBox, 1);
            grid.Children.Add(inputBox);

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button okBtn = new Button { Content = "OK", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
            Button cancelBtn = new Button { Content = "Отмена", Width = 60 };
            buttons.Children.Add(okBtn);
            buttons.Children.Add(cancelBtn);
            Grid.SetRow(buttons, 2);
            grid.Children.Add(buttons);

            string result = null;
            okBtn.Click += (s, ev) => { result = inputBox.Text; window.Close(); };
            cancelBtn.Click += (s, ev) => { result = null; window.Close(); };

            window.Content = grid;
            window.ShowDialog();
            return result;
        }
    }
}