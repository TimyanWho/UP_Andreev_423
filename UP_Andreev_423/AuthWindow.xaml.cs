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

            var role = Core.Context.Roles
                .ToList()
                .FirstOrDefault(r =>
                    DbUtil.Int(r, "RoleId", "Id") == DbUtil.Int(user, "RoleId", "IdRole"));

            string roleName = DbUtil.Str(role, "RoleName", "Name", "Title");
            if (string.IsNullOrWhiteSpace(roleName))
                roleName = "Читатель";

            int userId = DbUtil.Int(user, "UserId", "Id");
            int roleId = DbUtil.Int(user, "RoleId", "IdRole");

            Application.Current.Properties["CurrentUser"] = user;
            Application.Current.Properties["UserId"] = userId;
            Application.Current.Properties["UserLogin"] = DbUtil.Str(user, "Login", "UserLogin");
            Application.Current.Properties["DisplayName"] = DbUtil.Str(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
            Application.Current.Properties["Email"] = DbUtil.Str(user, "Email", "Mail", "EMail");
            Application.Current.Properties["RoleId"] = roleId;
            Application.Current.Properties["RoleName"] = roleName;
            Application.Current.Properties["IsFrozen"] = DbUtil.Bool(user, "IsFrozen", "Frozen", "Blocked");
            Application.Current.Properties["FreezeReason"] = DbUtil.Str(user, "FreezeReason", "Reason", "FreezeReasonText");
            Application.Current.Properties["IsAuthor"] = roleName.IndexOf("автор", StringComparison.OrdinalIgnoreCase) >= 0;

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

            var newUser = new Users();

            DbUtil.Set(newUser, login, "Login", "UserLogin");
            DbUtil.Set(newUser, password, "Password", "UserPassword");
            DbUtil.Set(newUser, email, "Email", "Mail", "EMail");
            DbUtil.Set(newUser, displayName, "DisplayName", "Name", "FullName", "Nickname");
            DbUtil.Set(newUser, "Читатель", "RoleName", "Role", "RoleTitle");
            DbUtil.Set(newUser, false, "IsFrozen", "Frozen", "Blocked");
            DbUtil.Set(newUser, DateTime.Now, "CreatedAt", "CreateDate", "DateCreated");

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
            catch (DbEntityValidationException ex)
            {
                var sb = new StringBuilder();
                foreach (var entity in ex.EntityValidationErrors)
                {
                    foreach (var err in entity.ValidationErrors)
                    {
                        sb.AppendLine($"{err.PropertyName}: {err.ErrorMessage}");
                    }
                }

                MessageBox.Show(sb.Length > 0 ? sb.ToString() : ex.Message, "Ошибка регистрации");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка регистрации");
            }
        }
    }
}