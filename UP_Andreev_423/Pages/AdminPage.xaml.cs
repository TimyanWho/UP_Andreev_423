using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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

        private void AdminPage_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => RefreshAll()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void RefreshAll()
        {
            LoadUsers();
            LoadComplaints();
            LoadRoleRequests();
            LoadUnfreezeRequests();
            LoadFrozenBooks();
            LoadFrozenUsers();
        }

        private void LoadUsers()
        {
            if (UsersGrid == null) return;
            var users = Core.Context.Users.ToList();
            UsersGrid.ItemsSource = users.Select(u => new
            {
                u.UserId,
                u.Login,
                u.FullName,
                u.Email,
                u.RoleName
            }).OrderBy(u => u.RoleName).ToList();
        }

        private void LoadComplaints()
        {
            if (ComplaintsItems == null) return;
            string filter = GetComboFilter(ComplaintStatusFilter);
            var complaints = Core.Context.Complaints.ToList();
            if (filter != "Все")
                complaints = complaints.Where(c => c.Status == filter).ToList();
            ComplaintsItems.ItemsSource = complaints.Select(c => new
            {
                c.ComplaintId,
                c.Reason,
                StatusText = c.Status
            }).OrderByDescending(c => c.StatusText).ToList();
        }

        private void LoadRoleRequests()
        {
            if (RoleRequestsItems == null) return;
            string filter = GetComboFilter(RoleRequestStatusFilter);
            var requests = Core.Context.RoleRequests.ToList();
            if (filter != "Все")
                requests = requests.Where(r => r.Status == filter).ToList();
            var users = Core.Context.Users.ToList();
            RoleRequestsItems.ItemsSource = requests.Select(r =>
            {
                var user = users.FirstOrDefault(u => u.UserId == r.UserId);
                return new
                {
                    r.RequestId,
                    UserDisplay = user?.FullName ?? "Аноним",
                    r.Motivation,
                    r.Status
                };
            }).OrderByDescending(r => r.Status).ToList();
        }

        private void LoadUnfreezeRequests()
        {
            if (UnfreezeRequestsItems == null) return;
            string filter = GetComboFilter(UnfreezeRequestStatusFilter);
            var requests = Core.Context.UnfreezeRequests.ToList();
            if (filter != "Все")
                requests = requests.Where(r => r.Status == filter).ToList();
            UnfreezeRequestsItems.ItemsSource = requests.Select(r => new
            {
                r.RequestId,
                r.Reason,
                r.Status
            }).OrderByDescending(r => r.Status).ToList();
        }

        private void LoadFrozenBooks()
        {
            if (FrozenBooksItems == null) return;
            var books = Core.Context.Books.ToList().Where(b => b.IsFrozen).ToList();
            FrozenBooksItems.ItemsSource = books.Select(b => new
            {
                b.BookId,
                b.Title,
                b.FreezeReason
            }).OrderBy(b => b.Title).ToList();
        }

        private void LoadFrozenUsers()
        {
            if (FrozenUsersItems == null) return;
            var users = Core.Context.Users.ToList().Where(u => u.IsFrozen).ToList();
            FrozenUsersItems.ItemsSource = users.Select(u => new
            {
                u.UserId,
                u.Login,
                u.FreezeReason
            }).OrderBy(u => u.Login).ToList();
        }

        private string GetComboFilter(ComboBox combo)
        {
            return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все";
        }

        private void ComplaintStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadComplaints();
        private void RoleRequestStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadRoleRequests();
        private void UnfreezeRequestStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadUnfreezeRequests();

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int userId)
            {
                var newPass = ShowInputDialog("Введите новый пароль:");
                if (string.IsNullOrWhiteSpace(newPass)) return;
                var user = Core.Context.Users.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    user.Password = newPass;
                    Core.Context.SaveChanges();
                    MessageBox.Show("Пароль изменён.");
                    LoadUsers();
                }
            }
        }

        private void FreezeUser_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int userId)
            {
                var user = Core.Context.Users.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    user.IsFrozen = true;
                    user.FreezeReason = "Заморожен администратором";
                    Core.Context.SaveChanges();
                    LoadUsers();
                }
            }
        }

        private void UnfreezeUser_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int userId)
            {
                var user = Core.Context.Users.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    user.IsFrozen = false;
                    user.FreezeReason = null;
                    Core.Context.SaveChanges();
                    LoadUsers();
                }
            }
        }

        private void UnfreezeUserFromList_Click(object sender, RoutedEventArgs e) => UnfreezeUser_Click(sender, e);

        private void UnfreezeBook_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int bookId)
            {
                var book = Core.Context.Books.FirstOrDefault(b => b.BookId == bookId);
                if (book != null)
                {
                    book.IsFrozen = false;
                    book.FreezeReason = null;
                    Core.Context.SaveChanges();
                    LoadFrozenBooks();
                }
            }
        }

        private void AcceptComplaint_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int complaintId)
            {
                var complaint = Core.Context.Complaints.FirstOrDefault(c => c.ComplaintId == complaintId);
                if (complaint != null)
                {
                    complaint.Status = "Принята";
                    Core.Context.SaveChanges();
                    LoadComplaints();
                }
            }
        }

        private void RejectComplaint_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int complaintId)
            {
                var complaint = Core.Context.Complaints.FirstOrDefault(c => c.ComplaintId == complaintId);
                if (complaint != null)
                {
                    complaint.Status = "Отклонена";
                    Core.Context.SaveChanges();
                    LoadComplaints();
                }
            }
        }

        private void AcceptRoleRequest_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int requestId)
            {
                var request = Core.Context.RoleRequests.FirstOrDefault(r => r.RequestId == requestId);
                if (request != null)
                {
                    request.Status = "Принята";
                    var user = Core.Context.Users.FirstOrDefault(u => u.UserId == request.UserId);
                    if (user != null) user.RoleName = "Author";
                    Core.Context.SaveChanges();
                    LoadRoleRequests();
                    LoadUsers();
                }
            }
        }

        private void RejectRoleRequest_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int requestId)
            {
                var request = Core.Context.RoleRequests.FirstOrDefault(r => r.RequestId == requestId);
                if (request != null)
                {
                    request.Status = "Отклонена";
                    Core.Context.SaveChanges();
                    LoadRoleRequests();
                }
            }
        }

        private void AcceptUnfreezeRequest_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int requestId)
            {
                var request = Core.Context.UnfreezeRequests.FirstOrDefault(r => r.RequestId == requestId);
                if (request != null)
                {
                    request.Status = "Принята";
                    Core.Context.SaveChanges();
                    LoadUnfreezeRequests();
                }
            }
        }

        private void RejectUnfreezeRequest_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int requestId)
            {
                var request = Core.Context.UnfreezeRequests.FirstOrDefault(r => r.RequestId == requestId);
                if (request != null)
                {
                    request.Status = "Отклонена";
                    Core.Context.SaveChanges();
                    LoadUnfreezeRequests();
                }
            }
        }

        private string ShowInputDialog(string prompt)
        {
            var window = new Window
            {
                Title = "Ввод",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };
            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var promptText = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(promptText, 0);
            grid.Children.Add(promptText);

            var inputBox = new TextBox { Height = 24, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(inputBox, 1);
            grid.Children.Add(inputBox);

            var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okBtn = new Button { Content = "OK", Width = 60, Margin = new Thickness(0, 0, 5, 0) };
            var cancelBtn = new Button { Content = "Отмена", Width = 60 };
            buttonsPanel.Children.Add(okBtn);
            buttonsPanel.Children.Add(cancelBtn);
            Grid.SetRow(buttonsPanel, 2);
            grid.Children.Add(buttonsPanel);

            string result = null;
            okBtn.Click += (s, ev) => { result = inputBox.Text; window.Close(); };
            cancelBtn.Click += (s, ev) => { result = null; window.Close(); };

            window.Content = grid;
            window.ShowDialog();
            return result;
        }
    }
}