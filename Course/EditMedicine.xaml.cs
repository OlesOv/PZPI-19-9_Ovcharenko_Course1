using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Course
{
    public partial class EditMedicine
    {
        private readonly FbConnection fb;
        private readonly Window parent;
        private readonly Window pparent;
        private readonly FbTransaction fbt;
        private List<int> substanceId;
        private readonly List<ComboBox> substanceCBs;
        private readonly List<TextBox> concentrationBs;
        private readonly Medicine curMed;
        private static readonly Regex Regex = new Regex("[^0-9.]"); //regex that matches disallowed text
        private List<string> substancesList = new List<string>();

        public EditMedicine(Window parent, FbConnection pfb, FbTransaction pfbt, Medicine curMed, Window pparent)
        {
            this.parent = parent;
            fb = pfb;
            fbt = pfbt;
            this.curMed = curMed;
            this.pparent = pparent;
            InitializeComponent();
            substanceId = new List<int>();
            LoadSubstances();
            substanceCBs = new List<ComboBox>();
            substanceCBs.Add(SubstanceCBox);
            concentrationBs = new List<TextBox>();
            concentrationBs.Add(Concentration);
            LoadMedicine();
        }

        public void LoadSubstances()
        {
            substanceId = new List<int>();
            var selectSql =
                new FbCommand(
                    "SELECT SUBSTANCE.ID, T.NAME FROM SUBSTANCE LEFT JOIN TREATMENT T on SUBSTANCE.ID = T.ID;", fb,
                    fbt);
            selectSql.Transaction = fbt;
            var reader = selectSql.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    substanceId.Add(reader.GetInt32(0));
                    substancesList.Add(reader.GetString(1));
                }
            }
            finally
            {
                reader.Close();
            }

            selectSql.Dispose();
            SubstanceCBox.DataContext = substancesList;
        }

        private static bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            FbCommand deleteSql = new FbCommand(string.Format("DELETE FROM MEDICINE WHERE ID =  {0};", curMed.Id), fb,
                fbt);
            deleteSql.ExecuteNonQuery();
            Close();
        }
        private void Concentration_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextAllowed(e.Text);
        }

        private void MinusSubstanceButton_Click(object sender, RoutedEventArgs e)
        {
            SubstanceWp.Children.Remove(SubstanceWp.Children[^1]);
            substanceCBs.Remove(substanceCBs[^1]);
            SubstanceWp.Children.Remove(SubstanceWp.Children[^1]);
            concentrationBs.Remove(concentrationBs[^1]);
            if (SubstanceWp.Children.Count == 4) MinusSubstanceButton.IsEnabled = false;
        }

        private void PlusSubstanceButton_Click(object sender, RoutedEventArgs e)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = substancesList,
                Width = 210,
                Height = 28,
                FontSize = 16,
                Margin = new Thickness(10, 10, 10, 10)
            };
            substanceCBs.Add(comboBox);

            var textBox = new TextBox();
            textBox.PreviewTextInput += Concentration_PreviewTextInput;
            textBox.ToolTip = Concentration.ToolTip;
            textBox.Width = 40;
            textBox.Height = 28;
            textBox.FontSize = 16;
            textBox.Margin = new Thickness(10, 10, 10, 10);

            concentrationBs.Add(textBox);
            SubstanceWp.Children.Add(comboBox);
            SubstanceWp.Children.Add(textBox);

            if (SubstanceWp.Children.Count > 4) MinusSubstanceButton.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AmountBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Window warehouse = new Warehouse(pparent, fb, fbt);
            parent.Close();
            pparent.Hide();
            warehouse.Show();
        }

        private void LoadMedicine()
        {
            var selectSql =
                new FbCommand(
                    string.Format("SELECT AMOUNT FROM INGREDIENT WHERE KMEDICINE = {0};", curMed.Id), fb,
                    fbt)
                { Transaction = fbt };
            var reader = selectSql.ExecuteReader();
            var concentrations = new List<double>();
            try
            {
                while (reader.Read())
                {
                    concentrations.Add(reader.GetDouble(0));
                }
            }
            finally
            {
                reader.Close();
            }

            selectSql.Dispose();

            NameBox.Text = curMed.Name;
            AmountBox.Text = Convert.ToString(curMed.Amount);
            for (var i = 0; i < curMed.SubstanceList.Count; i++)
            {
                if (i >= substanceCBs.Count) PlusSubstanceButton_Click(this, null);
                substanceCBs[i].SelectedIndex = substancesList.IndexOf(curMed.SubstanceList[i]);
                concentrationBs[i].Text = Convert.ToString(concentrations[i]);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var s = "";
            var t = 0;
            while (NameBox.Text.IndexOf('\'', t) > 0)
            {
                s += NameBox.Text.Substring(t, NameBox.Text.IndexOf('\'', t) - t + 1) + '\'';
                t = NameBox.Text.IndexOf('\'', t) + 1;
            }

            s += NameBox.Text.Substring(t, NameBox.Text.Length - t);
            var updateSql =
                new FbCommand(
                    $"UPDATE MEDICINE MD SET MD.NAME = '{s}' WHERE MD.ID = {curMed.Id}",
                    fb, fbt);
            updateSql.ExecuteNonQuery();
            updateSql =
                new FbCommand($"DELETE FROM INGREDIENT WHERE KMEDICINE = {curMed.Id}",
                    fb, fbt);
            updateSql.ExecuteNonQuery();
            if (SubstanceCBox.SelectedItem == null) MessageBox.Show("Оберіть хоч одну діючу речовину");
            else
            {
                foreach (var p in substanceCBs.Where(p => p.SelectedItem != null))
                {
                    updateSql = new FbCommand(
                        $"INSERT INTO INGREDIENT (KMEDICINE, KSUBSTANCE, AMOUNT) VALUES ({curMed.Id}, {substanceId[p.SelectedIndex]}, {concentrationBs[substanceCBs.IndexOf(p)].Text});",
                        fb, fbt);
                    updateSql.ExecuteNonQuery();
                }

                updateSql = new FbCommand(
                    $"UPDATE WAREHOUSE WH SET WH.AMOUNT = {AmountBox.Text} WHERE WH.KMEDICINE = {curMed.Id};",
                    fb, fbt);
                updateSql.ExecuteNonQuery();
                updateSql.Dispose();
                curMed.Amount = Convert.ToInt32(AmountBox.Text);
                Close();
            }
        }

    }
}