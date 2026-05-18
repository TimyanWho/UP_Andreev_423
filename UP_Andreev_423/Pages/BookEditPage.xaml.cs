using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP_Andreev_423.Pages
{
    public partial class BookEditPage : Page
    {
        private readonly int? _bookId;

        public BookEditPage()
        {
            InitializeComponent();
            Loaded += BookEditPage_Loaded;
        }

        public BookEditPage(int bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            Loaded += BookEditPage_Loaded;
        }

        private void BookEditPage_Loaded(object sender, RoutedEventArgs e)
        {
            string roleName = RoleNames.Normalize(Application.Current.Properties["RoleName"] as string);
            if (roleName != RoleNames.Author)
            {
                MessageBox.Show("Страница доступна только автору.");
                return;
            }

            if (_bookId.HasValue)
            {
                var book = Core.Context.Books.ToList()
                    .FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == _bookId.Value);

                if (book != null)
                {
                    TitleBox.Text = DbUtil.Str(book, "Title", "Name");
                    DescriptionBox.Text = DbUtil.Str(book, "Description", "Desc", "BookDescription");
                    CoverPathBox.Text = DbUtil.Str(book, "CoverPath", "Cover", "ImagePath");
                    ContentBox.Text = DbUtil.Str(book, "ContentText", "TextContent", "BookText", "Content");
                    GenresBox.Text = DbUtil.Str(book, "GenresText", "GenreText");
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = Application.Current.Properties["CurrentUser"] as Users;
            if (currentUser == null)
            {
                MessageBox.Show("Сначала войдите в аккаунт.");
                return;
            }

            int authorId = DbUtil.Int(currentUser, "UserId", "Id");

            if (string.IsNullOrWhiteSpace(TitleBox.Text) ||
                string.IsNullOrWhiteSpace(DescriptionBox.Text) ||
                string.IsNullOrWhiteSpace(ContentBox.Text))
            {
                MessageBox.Show("Заполните минимум название, описание и текст книги.");
                return;
            }

            Books book;
            bool isNew = !_bookId.HasValue;

            if (isNew)
            {
                book = new Books();
                DbUtil.Set(book, authorId, "AuthorId", "UserId", "OwnerId", "Author");
                DbUtil.Set(book, false, "IsFrozen", "Frozen", "Blocked");
                DbUtil.Set(book, DateTime.Now, "CreatedAt", "DateCreated");
                Core.Context.Books.Add(book);
            }
            else
            {
                book = Core.Context.Books.ToList()
                    .FirstOrDefault(b => DbUtil.Int(b, "BookId", "Id") == _bookId.Value);

                if (book == null)
                {
                    MessageBox.Show("Книга не найдена.");
                    return;
                }
            }

            DbUtil.Set(book, TitleBox.Text.Trim(), "Title", "Name");
            DbUtil.Set(book, DescriptionBox.Text.Trim(), "Description", "Desc", "BookDescription");
            DbUtil.Set(book, CoverPathBox.Text.Trim(), "CoverPath", "Cover", "ImagePath");
            DbUtil.Set(book, ContentBox.Text.Trim(), "ContentText", "TextContent", "BookText", "Content");
            DbUtil.Set(book, GenresBox.Text.Trim(), "GenresText", "GenreText");

            try
            {
                Core.Context.SaveChanges();
                MessageBox.Show("Книга сохранена.");

                var shell = Window.GetWindow(this) as ShellWindow;
                if (shell != null)
                    shell.Navigate(new AuthorPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException != null && ex.InnerException.InnerException != null
                    ? ex.InnerException.InnerException.Message
                    : ex.Message, "Ошибка сохранения");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            var shell = Window.GetWindow(this) as ShellWindow;
            if (shell != null)
                shell.Navigate(new AuthorPage());
        }
    }
}