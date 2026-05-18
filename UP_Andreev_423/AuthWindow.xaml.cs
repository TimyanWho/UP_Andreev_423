using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Windows;

namespace UP_Andreev_423
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            Title = "Читай, Пиши и не спиши";
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }

            var user = Core.Context.Users
                .ToList()
                .FirstOrDefault(u =>
                    string.Equals(DbUtil.Str(u, "Login", "UserLogin"), login, StringComparison.OrdinalIgnoreCase) &&
                    DbUtil.Str(u, "Password", "UserPassword") == password);

            if (user == null)
            {
                MessageBox.Show("Неверный логин или пароль.");
                return;
            }

            string roleName = RoleNames.Normalize(DbUtil.Str(user, "RoleName", "Role", "RoleTitle"));

            Application.Current.Properties["CurrentUser"] = user;
            Application.Current.Properties["UserId"] = DbUtil.Int(user, "UserId", "Id");
            Application.Current.Properties["UserLogin"] = DbUtil.Str(user, "Login", "UserLogin");
            Application.Current.Properties["DisplayName"] = DbUtil.Str(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
            Application.Current.Properties["Email"] = DbUtil.Str(user, "Email", "Mail", "EMail");
            Application.Current.Properties["RoleName"] = roleName;
            Application.Current.Properties["RoleDisplay"] = RoleNames.ToDisplay(roleName);
            Application.Current.Properties["IsFrozen"] = DbUtil.Bool(user, "IsFrozen", "Frozen", "Blocked");
            Application.Current.Properties["FreezeReason"] = DbUtil.Str(user, "FreezeReason", "Reason", "FreezeReasonText");
            Application.Current.Properties["IsAuthor"] = roleName == RoleNames.Author;
            Application.Current.Properties["IsAdmin"] = roleName == RoleNames.Admin;

            var shell = new ShellWindow();
            shell.Show();
            Close();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string displayName = RegNameBox.Text.Trim();
            string login = RegLoginBox.Text.Trim();
            string email = RegEmailBox.Text.Trim();
            string password = RegPasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(displayName) ||
                string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заполните все поля регистрации.");
                return;
            }

            var users = Core.Context.Users.ToList();

            if (users.Any(u => DbUtil.Str(u, "Login", "UserLogin").Equals(login, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Такой логин уже существует.");
                return;
            }

            if (users.Any(u => DbUtil.Str(u, "Email", "Mail", "EMail").Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Такой email уже существует.");
                return;
            }

            var newUser = new Users
            {
                Login = login,
                Email = email,
                Password = password,
                FullName = displayName,
                RoleName = RoleNames.Reader,
                IsFrozen = false,
                CreatedAt = DateTime.Now
            };

            try
            {
                Core.Context.Users.Add(newUser);
                Core.Context.SaveChanges();

                MessageBox.Show("Пользователь зарегистрирован.");

                RegNameBox.Clear();
                RegLoginBox.Clear();
                RegEmailBox.Clear();
                RegPasswordBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException != null && ex.InnerException.InnerException != null
                    ? ex.InnerException.InnerException.Message
                    : ex.Message, "Ошибка регистрации");
            }
        }
    }
}