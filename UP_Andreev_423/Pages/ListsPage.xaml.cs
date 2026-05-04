using System.Linq;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class ListsPage : Page
    {
        public ListsPage()
        {
            InitializeComponent();
            Loaded += ListsPage_Loaded;
        }

        private void ListsPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ListsGrid.ItemsSource = Core.Context.ReadingLists.ToList();
        }
    }
}