using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadDemoData();
            ShowAuth();
        }

        private void ShowAuth()
        {
            AuthView.Visibility = Visibility.Visible;
            MainView.Visibility = Visibility.Collapsed;
        }

        private void ShowMain()
        {
            AuthView.Visibility = Visibility.Collapsed;
            MainView.Visibility = Visibility.Visible;
            ShowPage("Catalog");
        }

        private void ShowPage(string page)
        {
            CatalogPage.Visibility = Visibility.Collapsed;
            ListsPage.Visibility = Visibility.Collapsed;
            ProfilePage.Visibility = Visibility.Collapsed;
            AuthorPage.Visibility = Visibility.Collapsed;
            AdminPage.Visibility = Visibility.Collapsed;

            switch (page)
            {
                case "Catalog":
                    CatalogPage.Visibility = Visibility.Visible;
                    break;
                case "Lists":
                    ListsPage.Visibility = Visibility.Visible;
                    break;
                case "Profile":
                    ProfilePage.Visibility = Visibility.Visible;
                    break;
                case "Author":
                    AuthorPage.Visibility = Visibility.Visible;
                    break;
                case "Admin":
                    AdminPage.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBoxAuth.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }

            // Заглушка для прототипа.
            // Позже сюда можно подключить проверку через БД.
            ShowMain();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RegNameBox.Text) ||
                string.IsNullOrWhiteSpace(RegLoginBox.Text) ||
                string.IsNullOrWhiteSpace(RegEmailBox.Text) ||
                string.IsNullOrWhiteSpace(RegPasswordBox.Password))
            {
                MessageBox.Show("Заполните все поля регистрации.");
                return;
            }

            MessageBox.Show("Пользователь зарегистрирован. Для прототипа это заглушка.");
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                ShowPage(tag);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LoginBox.Clear();
            PasswordBoxAuth.Clear();
            ShowAuth();
        }

        private void LoadDemoData()
        {
            BooksGrid.ItemsSource = new List<object>
            {
                new
                {
                    Title = "Тихая книга",
                    Author = "А. Листьев",
                    Genre = "Фэнтези",
                    Rating = 4.8
                },
                new
                {
                    Title = "Город после дождя",
                    Author = "М. Север",
                    Genre = "Драма",
                    Rating = 4.5
                },
                new
                {
                    Title = "Песни пустой башни",
                    Author = "И. Лесной",
                    Genre = "Приключения",
                    Rating = 4.2
                }
            };

            ListsGrid.ItemsSource = new List<object>
            {
                new { Title = "Книга 1", Author = "Автор 1", Status = "Читаю", Rating = 4.0 },
                new { Title = "Книга 2", Author = "Автор 2", Status = "В планах", Rating = 4.6 }
            };

            UserReviewsGrid.ItemsSource = new List<object>
            {
                new { Book = "Тихая книга", Rating = 5, Text = "Очень понравилось." },
                new { Book = "Город после дождя", Rating = 4, Text = "Хорошая атмосфера." }
            };

            AuthorBooksGrid.ItemsSource = new List<object>
            {
                new { Title = "Первая книга", Status = "Опубликована", Rating = 4.7 },
                new { Title = "Вторая книга", Status = "Заморожена", Rating = 4.1 }
            };

            ComplaintsGrid.ItemsSource = new List<object>
            {
                new { Type = "Жалоба на книгу", ObjectName = "Тихая книга", Status = "На рассмотрении" },
                new { Type = "Жалоба на отзыв", ObjectName = "Отзыв #12", Status = "На рассмотрении" }
            };

            RequestsGrid.ItemsSource = new List<object>
            {
                new { Type = "Роль автора", User = "user1", Status = "На рассмотрении" },
                new { Type = "Снятие заморозки", User = "user2", Status = "На рассмотрении" }
            };

            FrozenUsersGrid.ItemsSource = new List<object>
            {
                new { Login = "frozen_user", Reason = "Нарушение правил", Date = "2026-04-20" }
            };

            UsersGrid.ItemsSource = new List<object>
            {
                new { Name = "Админ", Login = "admin", Role = "Администратор" },
                new { Name = "Автор", Login = "author", Role = "Автор" },
                new { Name = "Читатель", Login = "reader", Role = "Пользователь" }
            };
        }
    }
}