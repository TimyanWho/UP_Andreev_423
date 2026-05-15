using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            Loaded += ProfilePage_Loaded;
        }

        private void ProfilePage_Loaded(object sender, RoutedEventArgs e)
        {
            object currentUser = Application.Current.Properties["CurrentUser"];
            if (currentUser == null)
                return;

            int userId = GetInt(currentUser, "UserId", "Id", "ID");
            string displayName = GetString(currentUser, "DisplayName", "Name", "FullName", "Nickname", "Login");
            string login = GetString(currentUser, "Login", "UserLogin");
            string email = GetString(currentUser, "Email", "Mail", "EMail");
            string role = GetString(currentUser, "Role", "RoleName", "RoleTitle");
            if (string.IsNullOrWhiteSpace(role))
                role = "Читатель";

            NameValueText.Text = displayName;
            LoginValueText.Text = login;
            EmailValueText.Text = email;
            RoleValueText.Text = role;

            bool isFrozen = GetBool(currentUser, "IsFrozen", "Frozen", "Blocked");
            string reason = GetString(currentUser, "FreezeReason", "Reason", "FreezeReasonText");

            if (isFrozen)
            {
                FrozenBlock.Visibility = Visibility.Visible;
                FrozenReasonText.Text = string.IsNullOrWhiteSpace(reason)
                    ? "Причина заморозки не указана."
                    : $"Причина: {reason}";
            }

            var myReviews = Core.Context.Reviews.ToList()
                .Where(r => GetInt(r, "UserId", "ReviewerId", "AuthorId") == userId)
                .Select(r => new ReviewCard
                {
                    BookId = GetInt(r, "BookId", "IdBook"),
                    BookTitle = ResolveBookTitle(GetInt(r, "BookId", "IdBook")),
                    RatingText = $"Оценка: {GetInt(r, "Rating", "Score")}",
                    Text = GetString(r, "ReviewText", "Text", "Comment"),
                    CreatedAt = GetString(r, "CreatedAt", "DateCreated")
                })
                .ToList();

            ProfileStatsText.Text = $"Мои отзывы: {myReviews.Count}";
            UserReviewsItems.ItemsSource = myReviews;
        }

        private void SendAuthorRequest_Click(object sender, RoutedEventArgs e)
        {
            object currentUser = Application.Current.Properties["CurrentUser"];
            if (currentUser == null)
                return;

            int userId = GetInt(currentUser, "UserId", "Id", "ID");
            if (userId == 0)
                return;

            string reason = AuthorRequestReasonBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Введите причину заявки.");
                return;
            }

            var request = new RoleRequests();
            SetValue(request, userId, "UserId", "RequesterUserId");
            SetValue(request, reason, "Reason", "Motivation", "Comment");
            SetValue(request, "Новая", "Status");
            SetValue(request, DateTime.Now, "CreatedAt", "DateCreated");

            Core.Context.RoleRequests.Add(request);
            Core.Context.SaveChanges();

            MessageBox.Show("Заявка на роль автора отправлена.");
            AuthorRequestReasonBox.Clear();
        }

        private void SendUnfreezeRequest_Click(object sender, RoutedEventArgs e)
        {
            object currentUser = Application.Current.Properties["CurrentUser"];
            if (currentUser == null)
                return;

            int userId = GetInt(currentUser, "UserId", "Id", "ID");
            if (userId == 0)
                return;

            string reason = UnfreezeReasonBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Введите причину обращения.");
                return;
            }

            var request = new UnfreezeRequests();
            SetValue(request, userId, "RequesterUserId", "UserId");
            SetValue(request, userId, "TargetUserId", "FrozenUserId");
            SetValue(request, reason, "Reason", "Motivation", "Comment");
            SetValue(request, "Новая", "Status");
            SetValue(request, DateTime.Now, "CreatedAt", "DateCreated");

            Core.Context.UnfreezeRequests.Add(request);
            Core.Context.SaveChanges();

            MessageBox.Show("Обращение на разморозку отправлено.");
            UnfreezeReasonBox.Clear();
        }

        private void OpenBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ReviewCard card)
            {
                var shell = Window.GetWindow(this) as ShellWindow;
                shell?.NavigateToBook(card.BookId);
            }
        }

        private static string ResolveBookTitle(int bookId)
        {
            var book = Core.Context.Books.ToList()
                .FirstOrDefault(b => GetInt(b, "BookId", "Id") == bookId);

            return GetString(book, "Title", "Name");
        }

        private static object GetValue(object obj, params string[] names)
        {
            if (obj == null) return null;

            foreach (var name in names)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                    return prop.GetValue(obj);
            }

            return null;
        }

        private static string GetString(object obj, params string[] names)
        {
            return GetValue(obj, names)?.ToString() ?? string.Empty;
        }

        private static int GetInt(object obj, params string[] names)
        {
            var value = GetValue(obj, names);
            if (value == null) return 0;

            try { return Convert.ToInt32(value); }
            catch { return 0; }
        }

        private static bool GetBool(object obj, params string[] names)
        {
            var value = GetValue(obj, names);
            if (value == null) return false;

            if (value is bool b) return b;
            if (bool.TryParse(value.ToString(), out bool parsedBool)) return parsedBool;
            if (int.TryParse(value.ToString(), out int parsedInt)) return parsedInt != 0;
            return false;
        }

        private static void SetValue(object obj, object value, params string[] names)
        {
            if (obj == null)
                return;

            foreach (var name in names)
            {
                var prop = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        if (value == null)
                        {
                            prop.SetValue(obj, null);
                            return;
                        }

                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        prop.SetValue(obj, Convert.ChangeType(value, targetType));
                        return;
                    }
                    catch
                    {
                    }
                }
            }
        }

        public class ReviewCard
        {
            public int BookId { get; set; }
            public string BookTitle { get; set; }
            public string RatingText { get; set; }
            public string Text { get; set; }
            public string CreatedAt { get; set; }
        }
    }
}