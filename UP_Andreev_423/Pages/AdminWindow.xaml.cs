using System.Windows;

namespace UP_Andreev_423
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            Loaded += AdminWindow_Loaded;
        }

        private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AdminFrame.Navigate(new Pages.AdminPage());
        }
    }
}