using System.Windows;

namespace UP_Andreev_423
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
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

            // Пока это прототип.
            // Для удобства роли можно проверять по логину:
            // admin -> администратор
            // author -> автор
            // frozen -> замороженный пользователь
            // всё остальное -> обычный пользователь

            string role = "Пользователь";
            bool isFrozen = false;
            bool isAuthor = false;

            if (login.Equals("admin", System.StringComparison.OrdinalIgnoreCase))
                role = "Администратор";
            else if (login.Equals("author", System.StringComparison.OrdinalIgnoreCase))
            {
                role = "Автор";
                isAuthor = true;
            }
            else if (login.Equals("frozen", System.StringComparison.OrdinalIgnoreCase))
            {
                isFrozen = true;
            }

            var shell = new ShellWindow(role, isAuthor, isFrozen);
            shell.Show();
            Close();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RegNameBox.Text) ||
                string.IsNullOrWhiteSpace(RegLoginBox.Text) ||
                string.IsNullOrWhiteSpace(RegEmailBox.Text) ||
                string.IsNullOrWhiteSpace(RegPasswordBox.Password))
            {
                MessageBox.Show("Заполните все поля регистрации.");
                return;
            }

            MessageBox.Show("Пользователь зарегистрирован. В прототипе это пока заглушка.");
        }
    }
}