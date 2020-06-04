using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Course
{
    public class DataGridNumericColumn : DataGridTextColumn
    {
        protected override object PrepareCellForEdit(FrameworkElement editingElement,
            RoutedEventArgs editingEventArgs)
        {
            if (editingElement is TextBox edit) edit.PreviewTextInput += OnPreviewTextInput;

            return base.PrepareCellForEdit(editingElement, editingEventArgs);
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Convert.ToInt32(e.Text);
            }
            catch
            {
                e.Handled = true;
            }
        }
    }

    public class Medicine
    {
        public int Id;
        public List<string> IllnessList;
        public List<string> SubstanceList;

        public Medicine(string name, string illness, string substance, int amount, int id)
        {
            Name = name;
            IllnessList = new List<string>();
            IllnessList.Add(illness);
            SubstanceList = new List<string>();
            SubstanceList.Add(substance);
            Amount = amount;
            Id = id;
        }
        public string Name { get; set; }
        public string Illness => string.Join(", ", IllnessList);
        public string Substance => string.Join(", ", SubstanceList);
        public int Amount { get; set; }
    }

    public partial class Warehouse
    {
        private readonly FbConnection fb;
        private readonly Window parent;
        private readonly FbTransaction fbt;
        private ObservableCollection<Medicine> medList;
        private static ObservableCollection<Medicine> filteredMedList;

        public Warehouse(Window parent, FbConnection pfb, FbTransaction pfbt)
        {
            this.parent = parent;
            fb = pfb;
            fbt = pfbt;
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            UpdateDb();
            parent.Show();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            filteredMedList.Clear();
            foreach (var p in medList)
            {
                if (p.Name.ToLower().IndexOf(SearchBox.Text.ToLower()) > -1)
                {
                    filteredMedList.Add(p);
                }

                if (p.Substance.ToLower().IndexOf(SearchBox.Text.ToLower()) > -1 && !filteredMedList.Contains(p))
                {
                    filteredMedList.Add(p);
                }
            }
        }

        private void AddMedicineButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDb();
            var addMedicine = new AddMedicine(this, fb, fbt, parent);
            Hide();
            addMedicine.Show();
        }

        private void UpdateDb()
        {
            foreach (var p in medList)
            {
                var updateSql =
                    new FbCommand(
                        $"UPDATE WAREHOUSE SET AMOUNT = {p.Amount} WHERE KMEDICINE = {p.Id};", fb, fbt);
                updateSql.ExecuteNonQuery();
                updateSql.Dispose();
            }
        }

        public void WarehouseData_Loaded(object sender, RoutedEventArgs e)
        {
            var selectSql = new FbCommand(
                "SELECT MD.NAME, WH.AMOUNT, Il.NAME, TR.NAME, MD.ID FROM ILLNESSTHERAPY ILTH LEFT JOIN ILLNESS IL ON ILTH.KILLNESS = IL.ID LEFT JOIN THERAPY TH ON ILTH.KTHERAPY = TH.ID LEFT JOIN SUBSTANCE SB ON TH.ID = SB.ID LEFT JOIN TREATMENT TR on SB.ID = TR.ID LEFT JOIN INGREDIENT IG on SB.ID = IG.KSUBSTANCE LEFT JOIN MEDICINE MD on MD.ID = IG.KMEDICINE LEFT JOIN WAREHOUSE WH on MD.ID = WH.KMEDICINE WHERE MD.NAME IS NOT NULL;",
                fb);
            selectSql.Transaction = fbt;
            var reader = selectSql.ExecuteReader();
            var selectResult = new ObservableCollection<Medicine>();
            try
            {
                while (reader.Read())
                {
                    var t = new Medicine(reader.GetString(0), reader.GetString(2), reader.GetString(3),
                        reader.GetInt32(1), reader.GetInt32(4));
                    bool b = true;
                    foreach (var p in selectResult)
                    {
                        if (p.Name == t.Name)
                        {
                            b = false;
                            if(!selectResult[selectResult.IndexOf(p)].SubstanceList.Contains(t.SubstanceList[0])) selectResult[selectResult.IndexOf(p)].SubstanceList.Add(t.SubstanceList[0]);
                            if(!selectResult[selectResult.IndexOf(p)].IllnessList.Contains(t.IllnessList[0])) selectResult[selectResult.IndexOf(p)].IllnessList.Add(t.IllnessList[0]);
                        }
                    }
                    if(b) selectResult.Add(t);
                }
            }
            finally
            {
                reader.Close();
            }

            selectSql.Dispose();
            medList = selectResult;
            filteredMedList = new ObservableCollection<Medicine>(medList);
            WarehouseData.DataContext = filteredMedList;
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UpdateDb();
            foreach (var p in medList)
                if (Equals(((Medicine) WarehouseData.SelectedCells[0].Item).Name, p.Name))
                {
                    var editMedicine = new EditMedicine(this, fb, fbt, p, parent);
                    editMedicine.Show();
                    Hide();
                }
        }
    }
}