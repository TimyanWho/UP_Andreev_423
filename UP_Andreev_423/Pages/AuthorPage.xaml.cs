using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class AuthorPage : Page
    {
        public AuthorPage()
        {
            InitializeComponent();
            Loaded += AuthorPage_Loaded;
        }

        private void AuthorPage_Loaded(object sender, RoutedEventArgs e)
        {
            object currentUser = Application.Current.Properties["CurrentUser"];
            int currentUserId = GetInt(currentUser, "Id", "UserId", "UserID");

            if (currentUserId == 0)
            {
                AuthorBooksGrid.ItemsSource = Core.Context.Books.ToList();
                return;
            }

            AuthorBooksGrid.ItemsSource = Core.Context.Books.ToList()
                .Where(b => GetInt(b, "AuthorId", "UserId", "OwnerId", "Author") == currentUserId)
                .ToList();
        }

        private static int GetInt(object obj, params string[] names)
        {
            if (obj == null) return 0;

            foreach (var name in names)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    if (value == null) continue;

                    try
                    {
                        return Convert.ToInt32(value);
                    }
                    catch
                    {
                    }
                }
            }

            return 0;
        }
    }
}