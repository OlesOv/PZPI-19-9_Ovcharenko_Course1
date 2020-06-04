using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Course
{
    public class Doctor
    {
        public Doctor(int id, string name, string speciality)
        {
            Id = id;
            Name = name;
            Speciality = speciality;
        }

        public int Id { get; }
        public string Name { get; set; }
        public string Speciality { get; set; }
    }

    public partial class Doctors : Window
    {
        private readonly FbConnection fb;
        private readonly Window parent;
        private readonly FbTransaction fbt;
        public static ObservableCollection<Doctor> DocList;

        public void LoadDoctors()
        {
            var selectSql =
                new FbCommand(
                    "SELECT * FROM DOCTOR LEFT JOIN SPECIALTY SP ON KSPECIALTY = SP.ID LEFT JOIN ACCOUNT AC ON DOCTOR.ID = AC.ID;",
                    fb);
            selectSql.Transaction = fbt;
            var reader = selectSql.ExecuteReader();
            var selectResult = new ObservableCollection<Doctor>();
            try
            {
                while (reader.Read())
                {
                    selectResult.Add(new Doctor(reader.GetInt32(0), reader.GetString(5), reader.GetString(3)));
                }
            }
            finally
            {
                reader.Close();
            }

            selectSql.Dispose();
            DocList = selectResult;
        }

        public Doctors(Window parent, FbConnection pfb, FbTransaction pfbt)
        {
            this.parent = parent;
            fb = pfb;
            fbt = pfbt;
            InitializeComponent();
            LoadDoctors();
            Doctors1.DataContext = DocList;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            parent.Show();
        }

        private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var removing = DocList[Doctors1.SelectedIndex];
            foreach (var p in DocList)
                if (Equals(((Doctor) Doctors1.SelectedCells[0].Item).Name, p.Name))
                {
                    removing = p;
                }
            var deleteSql = new FbCommand($"DELETE FROM DOCTOR WHERE DOCTOR.ID = {removing.Id}; ", fb);
            deleteSql.Transaction = fbt;
            deleteSql.ExecuteNonQuery();
            deleteSql = new FbCommand($"DELETE FROM ACCOUNT WHERE ACCOUNT.ID = {removing.Id};", fb);
            deleteSql.Transaction = fbt;
            deleteSql.ExecuteNonQuery();
            deleteSql.Dispose();
            DocList.Remove(removing);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var addDoctor = new AddDoctor(this, fb, fbt);
            addDoctor.Show();
            Hide();
        }
    }
}