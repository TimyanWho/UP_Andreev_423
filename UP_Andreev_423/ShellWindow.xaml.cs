using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423
{
    public partial class ShellWindow : Window
    {
        private readonly string _role;
        private readonly bool _isAuthor;
        private readonly bool _isFrozen;

        public ShellWindow(string role, bool isAuthor, bool isFrozen)
        {
            InitializeComponent();

            _role = role;
            _isAuthor = isAuthor;
            _isFrozen = isFrozen;

            Loaded += ShellWindow_Loaded;
        }

        private void ShellWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UserInfoText.Text = $"Роль: {_role}";
            MainFrame.Navigate(new Pages.CatalogPage());

            AdminButton.Visibility = _role == "Администратор" ? Visibility.Visible : Visibility.Collapsed;
            AuthorButton.Visibility = _isAuthor ? Visibility.Visible : Visibility.Collapsed;
            FrozenButton.Visibility = _isFrozen ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                switch (tag)
                {
                    case "Catalog":
                        MainFrame.Navigate(new Pages.CatalogPage());
                        break;
                    case "Lists":
                        MainFrame.Navigate(new Pages.ListsPage());
                        break;
                    case "Profile":
                        MainFrame.Navigate(new Pages.ProfilePage());
                        break;
                    case "Author":
                        MainFrame.Navigate(new Pages.AuthorPage());
                        break;
                    case "Admin":
                        MainFrame.Navigate(new Pages.AdminPage());
                        break;
                }
            }
        }

        private void Frozen_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Аккаунт заморожен. Причина и кнопка оспаривания будут показаны на странице профиля.");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new AuthWindow().Show();
            Close();
        }
    }
}