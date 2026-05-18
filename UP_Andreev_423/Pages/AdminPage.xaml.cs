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
            if (ComplaintsItems == null || RoleRequestsItems == null || UnfreezeRequestsItems == null || FrozenBooksItems == null || FrozenUsersItems == null)
            {
                // Элементы ещё не созданы – попробуем перезагрузить страницу позже или просто выйдем
                return;
            }

            if (Core.Context == null)
            {
                MessageBox.Show("База данных ещё не готова. Попробуйте позже.");
                return;
            }
            LoadUsers();
            LoadComplaints();
            LoadRoleRequests();
            LoadUnfreezeRequests();
            LoadFrozenBooks();
            LoadFrozenUsers();
        }

        private void LoadUsers()
        {
            var users = Core.Context.Users?.ToList() ?? new List<Users>();
            UsersGrid.ItemsSource = users
                .Select(u => new
                {
                    UserId = u.UserId,
                    Login = u.Login,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleName = u.RoleName
                })
                .OrderBy(u => u.RoleName)
                .ToList();
        }

        private void LoadComplaints()
        {
            if (Core.Context == null || Core.Context.Complaints == null || ComplaintsItems == null)
                return;

            var complaints = Core.Context.Complaints.ToList();
            string filter = GetComboFilter(ComplaintStatusFilter);
            var query = complaints.AsEnumerable();
            if (filter != "Все") query = query.Where(c => c.Status == filter);

            ComplaintsItems.ItemsSource = query
                .Select(c => new
                {
                    ComplaintId = c.ComplaintId,
                    Reason = c.Reason,
                    StatusText = c.Status
                })
                .OrderByDescending(c => c.StatusText)
                .ToList();
        }

        private void LoadRoleRequests()
        {
            if (Core.Context == null || Core.Context.RoleRequests == null || RoleRequestsItems == null)
                return;

            var requests = Core.Context.RoleRequests.ToList();
            var users = Core.Context.Users?.ToList() ?? new List<Users>();
            string filter = GetComboFilter(RoleRequestStatusFilter);
            var query = requests.AsEnumerable();
            if (filter != "Все") query = query.Where(r => r.Status == filter);

            RoleRequestsItems.ItemsSource = query
                .Select(r =>
                {
                    var user = users.FirstOrDefault(u => u.UserId == r.UserId);
                    return new
                    {
                        RequestId = r.RequestId,
                        UserDisplay = user != null ? user.FullName : "Аноним",
                        Motivation = r.Motivation,
                        Status = r.Status
                    };
                })
                .OrderByDescending(r => r.Status)
                .ToList();
        }

        private void LoadUnfreezeRequests()
        {
            var requests = Core.Context.UnfreezeRequests.ToList();
            string filter = GetComboFilter(UnfreezeRequestStatusFilter);
            var query = requests;
            //if (filter != "Все") query = query.Where(r => r.Status == filter);
            UnfreezeRequestsItems.ItemsSource = query
                .Select(r => new
                {
                    RequestId = r.RequestId,
                    Reason = r.Reason,
                    Status = r.Status
                })
                .OrderByDescending(r => r.Status)
                .ToList();
        }

        private void LoadFrozenBooks()
        {
            var books = Core.Context.Books?.ToList() ?? new List<Books>();
            FrozenBooksItems.ItemsSource = books
                .Where(b => b.IsFrozen)
                .Select(b => new
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    FreezeReason = b.FreezeReason
                })
                .OrderBy(b => b.Title)
                .ToList();
        }

        private void LoadFrozenUsers()
        {
            var users = Core.Context.Users?.ToList() ?? new List<Users>();
            FrozenUsersItems.ItemsSource = users
                .Where(u => u.IsFrozen)
                .Select(u => new
                {
                    UserId = u.UserId,
                    Login = u.Login,
                    FreezeReason = u.FreezeReason
                })
                .OrderBy(u => u.Login)
                .ToList();
        }

        private string GetComboFilter(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item)
                return item.Content.ToString();
            return "Все";
        }

        private void ComplaintStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadComplaints();
        private void RoleRequestStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadRoleRequests();
        private void UnfreezeRequestStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadUnfreezeRequests();

        // Действия с пользователями
        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int userId)
            {
                string newPassword = ShowInputDialog("Введите новый пароль:");
                if (string.IsNullOrWhiteSpace(newPassword)) return;
                var user = Core.Context.Users.FirstOrDefault(u => u.UserId == userId);
                if (user != null)
                {
                    user.Password = newPassword;
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