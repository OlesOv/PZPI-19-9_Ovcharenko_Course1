using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Course
{
    public partial class AddDoctor
    {
        private class Speciality
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public Speciality(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        private readonly FbConnection fb;
        private readonly Window parent;
        private readonly FbTransaction fbt;
        private List<Speciality> spList;

        private int NextId()
        {
            var selectSql = new FbCommand("SELECT MAX(ID) FROM ACCOUNT;", fb) {Transaction = fbt};
            var reader = selectSql.ExecuteReader();
            var s = 0;
            try
            {
                while (reader.Read())
                {
                    s = reader.GetInt32(0);
                }
            }
            finally
            {
                reader.Close();
            }

            selectSql.Dispose();
            return s + 1;
        }

        public void LoadSpecialities()
        {
            var selectSql = new FbCommand("SELECT * FROM SPECIALTY;", fb);
            selectSql.Transaction = fbt;
            var reader = selectSql.ExecuteReader();
            var selectResult = new List<Speciality>();
            var s = new List<string>();
            try
            {
                while (reader.Read())
                {
                    s.Add(reader.GetString(1));
                    selectResult.Add(new Speciality(reader.GetInt32(0), s.Last()));
                }
            }
            finally
            {
                reader.Close();
            }

            selectSql.Dispose();
            spList = selectResult;
            SpecialityBox.DataContext = s;
        }

        public AddDoctor(Window parent, FbConnection pfb, FbTransaction pfbt)
        {
            this.parent = parent;
            fb = pfb;
            fbt = pfbt;
            InitializeComponent();
            LoadSpecialities();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            parent.Show();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var nxt = NextId();
            var s = "";
            var t = 0;
            while (DocName.Text.IndexOf('\'', t) > 0)
            {
                s += DocName.Text.Substring(t, DocName.Text.IndexOf('\'', t) - t + 1) + '\'';
                t = DocName.Text.IndexOf('\'', t) + 1;
            }

            s += DocName.Text.Substring(t, DocName.Text.Length - t);
            var insertSql = new FbCommand(
                $"INSERT INTO ACCOUNT (ID, NAME, PASSWORD) VALUES ({nxt}, '{s}', '{Pass.Text}');", fb,fbt);
            insertSql.ExecuteNonQuery();
            insertSql = new FbCommand(
                $"INSERT INTO DOCTOR (ID, KSPECIALTY) VALUES ({nxt},{spList[SpecialityBox.SelectedIndex].Id});", fb, fbt);
            insertSql.ExecuteNonQuery();
            insertSql.Dispose();
            MainWindow.LoadAccounts();
            Doctors.DocList.Add(new Doctor(nxt, DocName.Text, spList[SpecialityBox.SelectedIndex].Name));
            Close();
        }

        private void Pass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SaveButton_Click(this, e);
            }
        }
    }
}