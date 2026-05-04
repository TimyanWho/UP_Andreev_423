using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Reflection;
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

            var user = Core.Context.Users.ToList().FirstOrDefault(u =>
                string.Equals(GetString(u, "Login", "UserLogin", "Логин"), login, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(GetString(u, "Password", "UserPassword", "Пароль"), password, StringComparison.Ordinal));

            if (user == null)
            {
                MessageBox.Show("Неверный логин или пароль.");
                return;
            }

            string roleName = ResolveRoleName(user);

            Application.Current.Properties["CurrentUser"] = user;
            Application.Current.Properties["UserLogin"] = GetString(user, "Login", "UserLogin");
            Application.Current.Properties["DisplayName"] = GetString(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
            Application.Current.Properties["Email"] = GetString(user, "Email", "Mail", "EMail");
            Application.Current.Properties["Role"] = roleName;
            Application.Current.Properties["IsAuthor"] = roleName.IndexOf("автор", StringComparison.OrdinalIgnoreCase) >= 0;
            Application.Current.Properties["IsFrozen"] = GetBool(user, "IsFrozen", "Frozen", "Blocked");
            Application.Current.Properties["FreezeReason"] = GetString(user, "FreezeReason", "Reason", "FreezeReasonText");

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

            if (Core.Context.Users.ToList().Any(u =>
                    string.Equals(GetString(u, "Login", "UserLogin"), login, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Такой логин уже существует.");
                return;
            }

            if (Core.Context.Users.ToList().Any(u =>
                    string.Equals(GetString(u, "Email", "Mail", "EMail"), email, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Такой email уже существует.");
                return;
            }

            var readerRole = Core.Context.Roles.ToList()
                .FirstOrDefault(r => string.Equals(GetString(r, "Name", "RoleName", "Title"), "Читатель", StringComparison.OrdinalIgnoreCase));

            if (readerRole == null)
            {
                MessageBox.Show("В таблице ролей не найдена роль 'Читатель'.");
                return;
            }

            var newUser = new Users();

            SetValue(newUser, login, "Login", "UserLogin");
            SetValue(newUser, password, "Password", "UserPassword");
            SetValue(newUser, email, "Email", "Mail", "EMail");
            SetValue(newUser, displayName, "DisplayName", "Name", "FullName", "Nickname");
            SetValue(newUser, false, "IsFrozen", "Frozen", "Blocked");
            SetValue(newUser, DateTime.Now, "CreatedAt", "CreateDate", "DateCreated");

            int roleId = GetInt(readerRole, "Id", "RoleId", "RoleID");
            if (roleId != 0)
            {
                SetValue(newUser, roleId, "RoleId", "RoleID", "IdRole", "Role");
            }

            SetValue(newUser, readerRole, "Role", "Roles", "RoleNavigation");

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

        private static string ResolveRoleName(object user)
        {
            if (user == null)
                return "Читатель";

            object roleNavigation = GetPropertyValue(user, "Role", "Roles", "RoleNavigation", "Role1");
            if (roleNavigation != null && !(roleNavigation is string))
            {
                string navName = GetString(roleNavigation, "Name", "RoleName", "Title");
                if (!string.IsNullOrWhiteSpace(navName))
                    return navName;
            }

            int roleId = GetInt(user, "RoleId", "RoleID", "IdRole", "Role");
            if (roleId != 0)
            {
                var role = Core.Context.Roles.ToList()
                    .FirstOrDefault(r => GetInt(r, "Id", "RoleId", "RoleID") == roleId);

                string roleName = GetString(role, "Name", "RoleName", "Title");
                if (!string.IsNullOrWhiteSpace(roleName))
                    return roleName;
            }

            string directRole = GetString(user, "Role", "RoleName", "Title");
            if (!string.IsNullOrWhiteSpace(directRole))
                return directRole;

            return "Читатель";
        }

        private static object GetPropertyValue(object obj, params string[] names)
        {
            if (obj == null)
                return null;

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
            var value = GetPropertyValue(obj, names);
            return value?.ToString() ?? string.Empty;
        }

        private static int GetInt(object obj, params string[] names)
        {
            var value = GetPropertyValue(obj, names);
            if (value == null)
                return 0;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private static bool GetBool(object obj, params string[] names)
        {
            var value = GetPropertyValue(obj, names);
            if (value == null)
                return false;

            if (value is bool b)
                return b;

            if (bool.TryParse(value.ToString(), out bool parsedBool))
                return parsedBool;

            if (int.TryParse(value.ToString(), out int parsedInt))
                return parsedInt != 0;

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
    }
}