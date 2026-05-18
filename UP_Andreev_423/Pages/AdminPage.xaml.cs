using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class AdminPage : Page
    {
        private List<ComplaintCard> _complaints = new List<ComplaintCard>();
        private List<RoleRequestCard> _roleRequests = new List<RoleRequestCard>();
        private List<UnfreezeRequestCard> _unfreezeRequests = new List<UnfreezeRequestCard>();
        private List<FrozenBookCard> _frozenBooks = new List<FrozenBookCard>();
        private List<FrozenUserCard> _frozenUsers = new List<FrozenUserCard>();
        private List<UserCard> _users = new List<UserCard>();

        public AdminPage()
        {
            InitializeComponent();
            Loaded += AdminPage_Loaded;
        }

        private void AdminPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAll();
        }

        private void LoadAll()
        {
            var users = Core.Context.Users.ToList();
            var roles = Core.Context.Roles.ToList();
            var books = Core.Context.Books.ToList();
            var reviews = Core.Context.Reviews.ToList();

            _complaints = Core.Context.Complaints.ToList().Select(c => new ComplaintCard
            {
                Entity = c,
                ComplaintId = DbUtil.Int(c, "ComplaintId", "Id"),
                TargetType = DbUtil.Str(c, "TargetType", "ComplaintType"),
                TargetId = DbUtil.Int(c, "TargetId", "ObjectId"),
                TargetTitle = ResolveComplaintTargetTitle(c, books, reviews, users),
                Reason = DbUtil.Str(c, "Reason", "ComplaintReason", "Text"),
                Status = DbUtil.Str(c, "Status", "State"),
                CreatedAt = DbUtil.Str(c, "CreatedAt", "DateCreated")
            }).ToList();

            _roleRequests = Core.Context.RoleRequests.ToList().Select(r => new RoleRequestCard
            {
                Entity = r,
                RequestId = DbUtil.Int(r, "RequestId", "Id"),
                UserId = DbUtil.Int(r, "UserId", "RequesterUserId"),
                UserLogin = ResolveUserLogin(users, DbUtil.Int(r, "UserId", "RequesterUserId")),
                RequestedRoleId = DbUtil.Int(r, "RequestedRoleId", "RoleId"),
                RequestedRole = ResolveRoleName(roles, DbUtil.Int(r, "RequestedRoleId", "RoleId")),
                Motivation = DbUtil.Str(r, "Motivation", "Reason", "Comment"),
                Status = DbUtil.Str(r, "Status", "State"),
                CreatedAt = DbUtil.Str(r, "CreatedAt", "DateCreated")
            }).ToList();

            _unfreezeRequests = Core.Context.UnfreezeRequests.ToList().Select(r => new UnfreezeRequestCard
            {
                Entity = r,
                RequestId = DbUtil.Int(r, "RequestId", "Id"),
                RequesterUserId = DbUtil.Int(r, "RequesterUserId", "UserId"),
                RequesterLogin = ResolveUserLogin(users, DbUtil.Int(r, "RequesterUserId", "UserId")),
                TargetUserId = DbUtil.Int(r, "TargetUserId", "FrozenUserId"),
                TargetBookId = DbUtil.Int(r, "TargetBookId", "BookId"),
                TargetName = ResolveUnfreezeTargetName(users, books, r),
                Reason = DbUtil.Str(r, "Reason", "Motivation", "Comment"),
                Status = DbUtil.Str(r, "Status", "State"),
                CreatedAt = DbUtil.Str(r, "CreatedAt", "DateCreated")
            }).ToList();

            _frozenBooks = books.Where(b => DbUtil.Bool(b, "IsFrozen", "Frozen", "Blocked")).Select(b => new FrozenBookCard
            {
                Entity = b,
                BookId = DbUtil.Int(b, "BookId", "Id"),
                Title = DbUtil.Str(b, "Title", "Name"),
                Author = ResolveBookAuthor(users, b),
                Reason = DbUtil.Str(b, "FreezeReason", "Reason"),
                IsFrozen = DbUtil.Bool(b, "IsFrozen", "Frozen", "Blocked")
            }).ToList();

            _frozenUsers = users.Where(u => DbUtil.Bool(u, "IsFrozen", "Frozen", "Blocked")).Select(u => new FrozenUserCard
            {
                Entity = u,
                UserId = DbUtil.Int(u, "UserId", "Id"),
                Login = DbUtil.Str(u, "Login", "UserLogin"),
                DisplayName = DbUtil.Str(u, "DisplayName", "Name", "FullName", "Nickname"),
                Reason = DbUtil.Str(u, "FreezeReason", "Reason"),
                IsFrozen = DbUtil.Bool(u, "IsFrozen", "Frozen", "Blocked")
            }).ToList();

            _users = users.Select(u => new UserCard
            {
                Entity = u,
                UserId = DbUtil.Int(u, "UserId", "Id"),
                Login = DbUtil.Str(u, "Login", "UserLogin"),
                DisplayName = DbUtil.Str(u, "DisplayName", "Name", "FullName", "Nickname"),
                Email = DbUtil.Str(u, "Email", "Mail", "EMail"),
                RoleName = NormalizeRoleName(ResolveUserRoleName(u, roles)),
                IsFrozen = DbUtil.Bool(u, "IsFrozen", "Frozen", "Blocked"),
                FreezeReason = DbUtil.Str(u, "FreezeReason", "Reason")
            }).ToList();

            ComplaintsItems.ItemsSource = _complaints;
            RoleRequestsItems.ItemsSource = _roleRequests;
            UnfreezeRequestsItems.ItemsSource = _unfreezeRequests;
            FrozenBooksItems.ItemsSource = _frozenBooks;
            FrozenUsersItems.ItemsSource = _frozenUsers;
            UsersItems.ItemsSource = _users;
        }

        private void AcceptComplaint_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as ComplaintCard;
            if (card == null) return;

            DbUtil.Set(card.Entity, "Принята", "Status", "State");
            SaveAndReload();
        }

        private void RejectComplaint_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as ComplaintCard;
            if (card == null) return;

            DbUtil.Set(card.Entity, "Отклонена", "Status", "State");
            SaveAndReload();
        }

        private void AcceptRoleRequest_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as RoleRequestCard;
            if (card == null) return;

            DbUtil.Set(card.Entity, "Принята", "Status", "State");

            var user = Core.Context.Users.ToList()
                .FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == card.UserId);

            if (user != null)
            {
                var roles = Core.Context.Roles.ToList();
                var authorRole = roles.FirstOrDefault(r =>
                    NormalizeRoleName(DbUtil.Str(r, "RoleName", "Name", "Title")) == RoleNames.Author);

                if (authorRole != null)
                {
                    DbUtil.Set(user, DbUtil.Int(authorRole, "RoleId", "Id"), "RoleId", "IdRole");
                    DbUtil.Set(user, RoleNames.Author, "RoleName", "Role", "RoleTitle");
                }
            }

            SaveAndReload();
        }

        private void RejectRoleRequest_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as RoleRequestCard;
            if (card == null) return;

            DbUtil.Set(card.Entity, "Отклонена", "Status", "State");
            SaveAndReload();
        }

        private void AcceptUnfreezeRequest_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as UnfreezeRequestCard;
            if (card == null) return;

            DbUtil.Set(card.Entity, "Принята", "Status", "State");

            if (card.TargetUserId > 0)
            {
                var user = Core.Context.Users.ToList().FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == card.TargetUserId);
                if (user != null)
                {
                    DbUtil.Set(user, false, "IsFrozen", "Frozen", "Blocked");
                    DbUtil.Set(user, string.Empty, "FreezeReason", "Reason");
                }
            }

            if (card.TargetBookId > 0)
            {
                var book = Core.Context.Books.ToList().FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == card.TargetBookId);
                if (book != null)
                {
                    DbUtil.Set(book, false, "IsFrozen", "Frozen", "Blocked");
                    DbUtil.Set(book, string.Empty, "FreezeReason", "Reason");
                }
            }

            SaveAndReload();
        }

        private void RejectUnfreezeRequest_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as UnfreezeRequestCard;
            if (card == null) return;

            DbUtil.Set(card.Entity, "Отклонена", "Status", "State");
            SaveAndReload();
        }

        private void UnfreezeBook_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as FrozenBookCard;
            if (card == null) return;

            DbUtil.Set(card.Entity, false, "IsFrozen", "Frozen", "Blocked");
            DbUtil.Set(card.Entity, string.Empty, "FreezeReason", "Reason");
            SaveAndReload();
        }

        private void KeepBookFrozen_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Книга оставлена в заморозке.");
        }

        private void UnfreezeUser_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as FrozenUserCard;
            if (card == null) return;

            DbUtil.Set(card.Entity, false, "IsFrozen", "Frozen", "Blocked");
            DbUtil.Set(card.Entity, string.Empty, "FreezeReason", "Reason");
            SaveAndReload();
        }

        private void KeepUserFrozen_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Пользователь оставлен в заморозке.");
        }

        private void SaveUser_Click(object sender, RoutedEventArgs e)
        {
            var card = (sender as Button)?.Tag as UserCard;
            if (card == null) return;

            string roleValue = NormalizeRoleName(card.RoleName);

            DbUtil.Set(card.Entity, card.Login, "Login", "UserLogin");
            DbUtil.Set(card.Entity, card.DisplayName, "DisplayName", "Name", "FullName", "Nickname");
            DbUtil.Set(card.Entity, card.Email, "Email", "Mail", "EMail");
            DbUtil.Set(card.Entity, roleValue, "RoleName", "Role", "RoleTitle");
            DbUtil.Set(card.Entity, card.IsFrozen, "IsFrozen", "Frozen", "Blocked");
            DbUtil.Set(card.Entity, card.FreezeReason ?? string.Empty, "FreezeReason", "Reason");

            SaveAndReload();
        }

        private void SaveAndReload()
        {
            try
            {
                Core.Context.SaveChanges();
                LoadAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException != null && ex.InnerException.InnerException != null
                    ? ex.InnerException.InnerException.Message
                    : ex.Message, "Ошибка сохранения");
            }
        }

        private static string ResolveUserLogin(IEnumerable<Users> users, int userId)
        {
            var user = users.FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == userId);
            return DbUtil.Str(user, "Login", "UserLogin");
        }

        private static string ResolveBookAuthor(IEnumerable<Users> users, object book)
        {
            int authorId = DbUtil.Int(book, "AuthorId", "UserId", "OwnerId", "Author");
            var user = users.FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == authorId);
            return DbUtil.Str(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
        }

        private static string ResolveUserRoleName(object user, IEnumerable<Roles> roles)
        {
            string roleName = DbUtil.Str(user, "RoleName", "Role", "RoleTitle");
            if (!string.IsNullOrWhiteSpace(roleName))
                return roleName;

            int roleId = DbUtil.Int(user, "RoleId", "IdRole");
            var role = roles.FirstOrDefault(r => DbUtil.Int(r, "RoleId", "Id") == roleId);
            return DbUtil.Str(role, "RoleName", "Name", "Title");
        }

        private static string ResolveRoleName(IEnumerable<Roles> roles, int roleId)
        {
            var role = roles.FirstOrDefault(r => DbUtil.Int(r, "RoleId", "Id") == roleId);
            return DbUtil.Str(role, "RoleName", "Name", "Title");
        }

        private static string ResolveComplaintTargetTitle(object complaint, List<Books> books, List<Reviews> reviews, List<Users> users)
        {
            string type = DbUtil.Str(complaint, "TargetType", "ComplaintType");
            int targetId = DbUtil.Int(complaint, "TargetId", "ObjectId");

            if (type.IndexOf("книг", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var book = books.FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == targetId);
                return DbUtil.Str(book, "Title", "Name");
            }

            if (type.IndexOf("отз", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var review = reviews.FirstOrDefault(r => DbUtil.Int(r, "ReviewId", "Id") == targetId);
                if (review != null)
                {
                    int bookId = DbUtil.Int(review, "BookId", "IdBook");
                    var book = books.FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == bookId);
                    return "Отзыв к книге: " + DbUtil.Str(book, "Title", "Name");
                }
            }

            return "Объект #" + targetId;
        }

        private static string ResolveUnfreezeTargetName(IEnumerable<Users> users, List<Books> books, object request)
        {
            int targetUserId = DbUtil.Int(request, "TargetUserId", "FrozenUserId");
            int targetBookId = DbUtil.Int(request, "TargetBookId", "BookId");

            if (targetUserId > 0)
            {
                var user = users.FirstOrDefault(u => DbUtil.Int(u, "UserId", "Id") == targetUserId);
                return "Пользователь: " + DbUtil.Str(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
            }

            if (targetBookId > 0)
            {
                var book = books.FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == targetBookId);
                return "Книга: " + DbUtil.Str(book, "Title", "Name");
            }

            return "Объект не указан";
        }

        private static string NormalizeRoleName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return RoleNames.Reader;

            if (roleName.Equals("Читатель", StringComparison.OrdinalIgnoreCase))
                return RoleNames.Reader;

            if (roleName.Equals("Автор", StringComparison.OrdinalIgnoreCase))
                return RoleNames.Author;

            if (roleName.Equals("Администратор", StringComparison.OrdinalIgnoreCase))
                return RoleNames.Admin;

            return roleName.Trim();
        }

        private class ComplaintCard
        {
            public object Entity { get; set; }
            public int ComplaintId { get; set; }
            public string TargetType { get; set; }
            public int TargetId { get; set; }
            public string TargetTitle { get; set; }
            public string Reason { get; set; }
            public string Status { get; set; }
            public string CreatedAt { get; set; }
        }

        private class RoleRequestCard
        {
            public object Entity { get; set; }
            public int RequestId { get; set; }
            public int UserId { get; set; }
            public string UserLogin { get; set; }
            public int RequestedRoleId { get; set; }
            public string RequestedRole { get; set; }
            public string Motivation { get; set; }
            public string Status { get; set; }
            public string CreatedAt { get; set; }
        }

        private class UnfreezeRequestCard
        {
            public object Entity { get; set; }
            public int RequestId { get; set; }
            public int RequesterUserId { get; set; }
            public string RequesterLogin { get; set; }
            public int TargetUserId { get; set; }
            public int TargetBookId { get; set; }
            public string TargetName { get; set; }
            public string Reason { get; set; }
            public string Status { get; set; }
            public string CreatedAt { get; set; }
        }

        private class FrozenBookCard
        {
            public object Entity { get; set; }
            public int BookId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Reason { get; set; }
            public bool IsFrozen { get; set; }

            public string IsFrozenText
            {
                get { return IsFrozen ? "Заморожена" : "Активна"; }
            }
        }

        private class FrozenUserCard
        {
            public object Entity { get; set; }
            public int UserId { get; set; }
            public string Login { get; set; }
            public string DisplayName { get; set; }
            public string Reason { get; set; }
            public bool IsFrozen { get; set; }

            public string IsFrozenText
            {
                get { return IsFrozen ? "Заморожен" : "Активен"; }
            }
        }

        private class UserCard
        {
            public object Entity { get; set; }
            public int UserId { get; set; }
            public string Login { get; set; }
            public string DisplayName { get; set; }
            public string Email { get; set; }
            public string RoleName { get; set; }
            public bool IsFrozen { get; set; }
            public string FreezeReason { get; set; }
        }
    }
}