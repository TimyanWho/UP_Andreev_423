using System.Linq;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class AdminPage : Page
    {
        public AdminPage()
        {
            InitializeComponent();
            Loaded += AdminPage_Loaded;
        }

        private void AdminPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ComplaintsGrid.ItemsSource = Core.Context.Complaints.ToList();
            RequestsGrid.ItemsSource = Core.Context.UnfreezeRequests.ToList();
            AuthorRequestsGrid.ItemsSource = Core.Context.RoleRequests.ToList();
            FrozenUsersGrid.ItemsSource = Core.Context.Users.ToList();
            UsersGrid.ItemsSource = Core.Context.Users.ToList();
        }
    }
}