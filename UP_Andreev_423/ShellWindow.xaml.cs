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
            string displayName = GetString("DisplayName", "Пользователь");
            string roleName = RoleNames.Normalize(GetString("RoleName", "Reader"));
            string roleDisplay = RoleNames.ToDisplay(roleName);
            bool isFrozen = GetBool("IsFrozen");
            bool isAuthor = roleName == RoleNames.Author;
            bool isAdmin = roleName == RoleNames.Admin;

            UserInfoText.Text = $"{displayName} | {roleDisplay}";

            AdminButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            AuthorButton.Visibility = isAuthor ? Visibility.Visible : Visibility.Collapsed;
            FrozenButton.Visibility = isFrozen ? Visibility.Visible : Visibility.Collapsed;

            MainFrame.Navigate(new Pages.CatalogPage());
        }

        private void OpenAdmin_Click(object sender, RoutedEventArgs e)
        {
            bool isAdmin =
                GetInt("RoleId", 0) == 3 ||
                GetString("Admin", "").Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                GetString("UserLogin", "").Equals("admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdmin)
            {
                MessageBox.Show("Доступно только администратору.");
                return;
            }

            MainFrame.Navigate(new Pages.AdminPage());
        }


        public void NavigateToBook(int bookId)
        {
            MainFrame.Navigate(new Pages.BookPage(bookId));
        }

        public void NavigateToReader(int bookId)
        {
            MainFrame.Navigate(new Pages.ReaderPage(bookId));
        }
        public void Navigate(Page page)
        {
            MainFrame.Navigate(page);
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

        private string GetString(string key, string fallback)
        {
            return Application.Current.Properties.Contains(key) && Application.Current.Properties[key] != null
                ? Application.Current.Properties[key].ToString()
                : fallback;
        }

        private int GetInt(string key, int fallback)
        {
            if (Application.Current.Properties.Contains(key) && Application.Current.Properties[key] != null)
            {
                try { return Convert.ToInt32(Application.Current.Properties[key]); }
                catch { }
            }
            return fallback;
        }

        private bool GetBool(string key)
        {
            return Application.Current.Properties.Contains(key) &&
                   Application.Current.Properties[key] is bool value &&
                   value;
        }
    }
}