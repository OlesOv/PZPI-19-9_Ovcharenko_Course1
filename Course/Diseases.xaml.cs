using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FirebirdSql.Data.FirebirdClient;

namespace Course
{
    public class Symptom
    {
        public Symptom(string name, int id)
        {
            Id = id;
            Name = name;
        }
        public bool Checked { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Disease
    {
        public Disease(string name, int id, string treatment, string manipulations)
        {
            Name = name;
            Id = id;
            Treatment = treatment;
            Manipulations = manipulations;
        }
        public bool Checked { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Treatment { get; set; }
        public string Manipulations { get; set; }
    }
    public partial class Diseases : Window
    {
        private readonly FbConnection fb;
        private readonly Window parent;
        private readonly FbTransaction fbt;
        private readonly Account curDoc;
        private ObservableCollection<Symptom> symptomList;
        private readonly ObservableCollection<Symptom> filteredSymptomList;

        private ObservableCollection<Disease> diseasesList;

        //робода з БД
        public string FbSelectString(string command, int id)
        {
            var selectSql = new FbCommand(string.Format(command, id), fb);
            selectSql.Transaction = fbt;
            var
                reader = selectSql
                    .ExecuteReader();
            var selectResult = "";
            try
            {
                while (reader.Read())
                {
                    selectResult += reader.GetString(0) + ", ";
                }
            }
            finally
            {
                reader.Close();
            }

            selectSql.Dispose();
            return selectResult;
        }

        public FbDataReader FbSelectList(string command)
        {
            var selectSql = new FbCommand(command, fb);
            selectSql.Transaction = fbt;
            var
                reader = selectSql
                    .ExecuteReader();
            return reader;
        }

        private string DManipulations(int id)
        {
            return FbSelectString(
                "SELECT TR.NAME, TH.AMOUNT, TH.INSTRUCTION FROM ILLNESSTHERAPY ILTH LEFT JOIN ILLNESS IL ON ILTH.KILLNESS = IL.ID LEFT JOIN THERAPY TH ON ILTH.KTHERAPY = TH.ID LEFT JOIN MANIPULATION MN ON TH.ID = MN.ID LEFT JOIN TREATMENT TR on MN.ID = TR.ID WHERE(ILTH.KILLNESS = {0}) AND(MN.ID IS NOT NULL);",
                id);
        }

        private string DTreatment(int id)
        {
            return FbSelectString(
                "SELECT MD.NAME, (TH.AMOUNT / IG.AMOUNT) DOSAGE, TH.INSTRUCTION, WH.AMOUNT FROM ILLNESSTHERAPY ILTH LEFT JOIN ILLNESS IL ON ILTH.KILLNESS = IL.ID LEFT JOIN THERAPY TH ON ILTH.KTHERAPY = TH.ID LEFT JOIN SUBSTANCE SB ON TH.ID = SB.ID LEFT JOIN TREATMENT TR on SB.ID = TR.ID LEFT JOIN INGREDIENT IG on SB.ID = IG.KSUBSTANCE LEFT JOIN MEDICINE MD on MD.ID = IG.KMEDICINE LEFT JOIN WAREHOUSE WH on MD.ID = WH.KMEDICINE WHERE(ILTH.KILLNESS = {0}) AND(SB.ID IS NOT NULL) AND(WH.AMOUNT > 0);",
                id);
        }

        private void LoadDiseases()
        {
            var reader = FbSelectList(
                $"SELECT IL.ID, MAX(IL.NAME), COUNT(IL.ID) CNT FROM ILLNESSSYMPTOM ILSY LEFT JOIN ILLNESS IL ON ILSY.KILLNESS = IL.ID WHERE ILSY.KSYMPTOM in {CheckedSymptoms()} GROUP BY IL.ID ORDER BY CNT DESC; ");
            var
                selectResult =
                    new ObservableCollection<Disease>();
            try
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    selectResult.Add(new Disease(reader.GetString(1), id, DTreatment(id), DManipulations(id)));
                }
            }
            finally
            {
                reader.Close();
            }

            Diseases1.DataContext = selectResult;
            diseasesList = selectResult;
        }

        private void LoadSymptoms()
        {
            var reader = FbSelectList("SELECT * FROM SYMPTOM");
            var
                selectResult =
                    new ObservableCollection<Symptom>();
            try
            {
                while (reader.Read())
                {
                    selectResult.Add(new Symptom(reader.GetString(1), reader.GetInt32(0)));
                }
            }
            finally
            {
                reader.Close();
            }

            symptomList = selectResult;
        }

        public Diseases(Window parent, FbConnection pfb, FbTransaction pfbt, Account curDoc)
        {
            this.parent = parent;
            fb = pfb;
            fbt = pfbt;
            this.curDoc = curDoc;
            InitializeComponent();
            LoadSymptoms();
            filteredSymptomList = new ObservableCollection<Symptom>(symptomList);
            Symptoms.DataContext = filteredSymptomList;
            RecipeButton.Click += RecipeButton_Click;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            parent.Show();
        }

        private void RecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var ids = (from p in diseasesList where p.Checked select p.Id).ToList();

            var recipe = new Recipe(this, ids, fb, fbt, curDoc);
            Hide();
            recipe.Show();
        }

        private string CheckedSymptoms()
        {
            var res = "(";
            foreach (var p in symptomList)
            {
                if (p.Checked)
                {
                    res += p.Id + ", ";
                }
            }

            if (res.Length > 2) return res.Substring(0, res.Length - 2) + ")";
            else return "(0)";
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            LoadDiseases();
        }

        private void SearchTermTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            filteredSymptomList.Clear();
            foreach (var p in symptomList)
            {
                if (p.Name.ToLower().IndexOf(SearchTermTextBox.Text.ToLower()) > -1)
                {
                    filteredSymptomList.Add(p);
                }
            }
        }
    }
}