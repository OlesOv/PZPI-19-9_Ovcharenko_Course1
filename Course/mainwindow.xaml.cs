using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FirebirdSql.Data.FirebirdClient;

namespace Course
{
    public class Account
    {
        public readonly int Id;
        public readonly string Login;
        public readonly string Password;
        public bool IsAdmin;

        public Account(int id, string login, string password)
        {
            Id = id;
            Login = login;
            Password = password;
        }
    }

    public partial class MainWindow : Window
    {
        private static FbConnection fb;
        private static FbTransaction fbt;
        private static ObservableCollection<Account> accList;

        public MainWindow()
        {
            var fbCon = new FbConnectionStringBuilder();
            fbCon.UserID = "sysdba"; //логін
            fbCon.Password = "1593572468"; //пароль
            fbCon.Database = "localhost:illness_catalogue"; //адреса БД
            fbCon.ServerType = 0;
            fb = new FbConnection(fbCon.ToString());
            fb.Open();
            fbt = fb.BeginTransaction();
            LoadAccounts();
            InitializeComponent();
            OkButton.Click += OKButtonClick;
            CancelButton.Click += CancelButtonClick;
        }


        public static void LoadAccounts()
        {
            var selectSql = new FbCommand("SELECT * FROM ACCOUNT", fb, fbt);
            var reader = selectSql.ExecuteReader();
            var selectResult = new ObservableCollection<Account>();
            try
            {
                while (reader.Read())
                {
                    selectResult.Add(new Account(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
                }
            }
            finally
            {
                reader.Close();
            }
            selectSql = new FbCommand("SELECT * FROM ADMINISTRATOR", fb, fbt);
            var nreader = selectSql.ExecuteReader();
            try
            {
                while (nreader.Read())
                {
                    foreach (var p in selectResult)
                    {
                        if (nreader.GetInt32(0) == p.Id) p.IsAdmin = true;
                    }
                }
            }
            finally
            {
                nreader.Close();
            }

            selectSql.Dispose();
            accList = selectResult;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OKButtonClick(object sender, RoutedEventArgs e)
        {
            var t = false;
            foreach (var p in accList)
            {
                if (Login.Text == p.Login && PasswordBox.Password == p.Password)
                {
                    t = true;
                    if (p.IsAdmin)
                    {
                        var adminMenu = new AdminMenu(this, fb, fbt);
                        Hide();
                        adminMenu.Show();
                    }
                    else
                    {
                        var diseases = new Diseases(this, fb, fbt, p);
                        Hide();
                        diseases.Show();
                    }
                }
            }

            if (!t) MessageBox.Show("Неправильне ім'я або пароль");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            fbt.Commit();
            fb.Close();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OKButtonClick(this, e);
            }
        }
    }
}