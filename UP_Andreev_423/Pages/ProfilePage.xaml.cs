using System;
using System.Collections.Generic;
using System.Linq;
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
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
                return;

            int userId = DbUtil.Int(currentUser, "UserId", "Id");

            NameValueText.Text = DbUtil.Str(currentUser, "DisplayName", "Name", "FullName", "Nickname", "Login");
            LoginValueText.Text = DbUtil.Str(currentUser, "Login", "UserLogin");
            EmailValueText.Text = DbUtil.Str(currentUser, "Email", "Mail", "EMail");
            RoleValueText.Text = DbUtil.Str(currentUser, "RoleName", "Role", "RoleTitle");

            if (DbUtil.Bool(currentUser, "IsFrozen", "Frozen", "Blocked"))
            {
                FrozenBlock.Visibility = Visibility.Visible;
                string reason = DbUtil.Str(currentUser, "FreezeReason", "Reason", "FreezeReasonText");
                FrozenReasonText.Text = string.IsNullOrWhiteSpace(reason)
                    ? "Причина заморозки не указана."
                    : $"Причина: {reason}";
            }

            var reviews = Core.Context.Reviews.ToList();

            var myReviews = reviews
                .Where(r => DbUtil.Int(r, "UserId", "ReviewerId", "AuthorId") == userId)
                .Select(r => new ReviewCard
                {
                    BookId = DbUtil.Int(r, "BookId", "IdBook"),
                    BookTitle = ResolveBookTitle(DbUtil.Int(r, "BookId", "IdBook")),
                    RatingText = $"Оценка: {DbUtil.Int(r, "Rating", "Score")}",
                    Text = DbUtil.Str(r, "ReviewText", "Text", "Comment"),
                    CreatedAt = DbUtil.Str(r, "CreatedAt", "DateCreated")
                })
                .ToList();

            ProfileStatsText.Text = $"Мои отзывы: {myReviews.Count}";
            UserReviewsItems.ItemsSource = myReviews;
        }

        private void SendAuthorRequest_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
                return;

            int userId = DbUtil.Int(currentUser, "UserId", "Id");
            string motivation = AuthorRequestReasonBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(motivation))
            {
                MessageBox.Show("Введите причину заявки.");
                return;
            }

            var roles = Core.Context.Roles.ToList();
            var authorRole = roles.FirstOrDefault(r => DbUtil.Str(r, "RoleName", "Name", "Title") == "Автор");
            if (authorRole == null)
            {
                MessageBox.Show("В таблице Roles не найдена роль 'Автор'.");
                return;
            }

            var request = new RoleRequests();
            DbUtil.Set(request, userId, "UserId");
            DbUtil.Set(request, DbUtil.Int(authorRole, "RoleId", "Id"), "RequestedRoleId", "RoleId");
            DbUtil.Set(request, motivation, "Motivation", "Reason", "Comment");
            DbUtil.Set(request, "Новая", "Status");
            DbUtil.Set(request, DateTime.Now, "CreatedAt", "DateCreated");

            try
            {
                Core.Context.RoleRequests.Add(request);
                Core.Context.SaveChanges();
                MessageBox.Show("Заявка на роль автора отправлена.");
                AuthorRequestReasonBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.InnerException?.Message ?? ex.Message, "Ошибка заявки");
            }
        }

        private void SendUnfreezeRequest_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
                return;

            int userId = DbUtil.Int(currentUser, "UserId", "Id");
            string reason = UnfreezeReasonBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Введите причину обращения.");
                return;
            }

            var request = new UnfreezeRequests();
            DbUtil.Set(request, userId, "RequesterUserId", "UserId");
            DbUtil.Set(request, userId, "TargetUserId", "FrozenUserId");
            DbUtil.Set(request, null, "TargetBookId", "BookId");
            DbUtil.Set(request, reason, "Reason", "Motivation", "Comment");
            DbUtil.Set(request, "Новая", "Status");
            DbUtil.Set(request, DateTime.Now, "CreatedAt", "DateCreated");

            try
            {
                Core.Context.UnfreezeRequests.Add(request);
                Core.Context.SaveChanges();
                MessageBox.Show("Обращение на разморозку отправлено.");
                UnfreezeReasonBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException?.InnerException?.Message ?? ex.Message, "Ошибка обращения");
            }
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
            var book = Core.Context.Books.FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == bookId);
            return DbUtil.Str(book, "Title", "Name");
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