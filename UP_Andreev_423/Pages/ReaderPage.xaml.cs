using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class ReaderPage : Page
    {
        private readonly int _bookId;

        public ReaderPage(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            Loaded += ReaderPage_Loaded;
        }

        private void ReaderPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var book = Core.Context.Books.ToList()
                .FirstOrDefault(b => GetInt(b, "BookId", "Id") == _bookId);

            if (book == null)
                return;

            var users = Core.Context.Users.ToList();
            int authorId = GetInt(book, "AuthorId", "UserId", "OwnerId", "Author");
            string author = ResolveAuthorName(users, authorId);

            BookTitleText.Text = GetString(book, "Title", "Name");
            BookAuthorText.Text = $"Автор: {author}";
            BookTextBlock.Text = GetString(book, "ContentText", "TextContent", "BookText", "Content");
        }

        private static string ResolveAuthorName(IEnumerable<object> users, int authorId)
        {
            var user = users.FirstOrDefault(u => GetInt(u, "UserId", "Id", "ID") == authorId);
            return GetString(user, "DisplayName", "Name", "FullName", "Nickname", "Login");
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

            try { return System.Convert.ToInt32(value); }
            catch { return 0; }
        }
    }
}