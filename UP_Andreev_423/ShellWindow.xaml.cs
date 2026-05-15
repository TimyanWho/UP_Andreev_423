using System;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423
{
    public partial class ShellWindow : Window
    {
        public ShellWindow()
        {
            InitializeComponent();
            Loaded += ShellWindow_Loaded;
        }

        private void ShellWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "Читай, Пиши и не спиши";

            string displayName = GetString("DisplayName", "Пользователь");
            string role = GetString("Role", "Читатель");
            bool isAuthor = GetBool("IsAuthor");
            bool isFrozen = GetBool("IsFrozen");

            UserInfoText.Text = $"{displayName} | {role}";

            AdminButton.Visibility = role.IndexOf("админ", StringComparison.OrdinalIgnoreCase) >= 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            AuthorButton.Visibility = isAuthor || role.IndexOf("автор", StringComparison.OrdinalIgnoreCase) >= 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            FrozenButton.Visibility = isFrozen ? Visibility.Visible : Visibility.Collapsed;

            MainFrame.Navigate(new Pages.CatalogPage());
        }

        public void NavigateToBook(int bookId)
        {
            MainFrame.Navigate(new Pages.BookPage(bookId));
        }

        public void NavigateToReader(int bookId)
        {
            MainFrame.Navigate(new Pages.ReaderPage(bookId));
        }

        private void OpenAdmin_Click(object sender, RoutedEventArgs e)
        {
            string role = GetString("Role", "Читатель");
            if (role.IndexOf("админ", StringComparison.OrdinalIgnoreCase) < 0)
            {
                MessageBox.Show("Доступно только администратору.");
                return;
            }

            var win = new AdminWindow();
            win.Show();
        }

        private string GetString(string key, string fallback)
        {
            return Application.Current.Properties.Contains(key) && Application.Current.Properties[key] != null
                ? Application.Current.Properties[key].ToString()
                : fallback;
        }

        private bool GetBool(string key)
        {
            return Application.Current.Properties.Contains(key) &&
                   Application.Current.Properties[key] is bool value &&
                   value;
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
                }
            }
        }

        private void Frozen_Click(object sender, RoutedEventArgs e)
        {
            string reason = GetString("FreezeReason", "Причина не указана.");
            MessageBox.Show($"Аккаунт заморожен.\nПричина: {reason}", "Заморозка");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new AuthWindow().Show();
            Close();
        }
    }
}