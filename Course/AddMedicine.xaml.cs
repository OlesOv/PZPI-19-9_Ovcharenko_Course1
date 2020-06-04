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
    public partial class AddMedicine
    {
        private readonly FbConnection fb;
        private readonly Window parent;
        private readonly Window pparent;
        private readonly FbTransaction fbt;
        private List<int> substanceId;
        private readonly List<ComboBox> substanceCBs;
        private readonly List<TextBox> concentrationBs;
        private static readonly Regex Regex = new Regex("[^0-9.]"); //regex that matches disallowed text

        public AddMedicine(Window parent, FbConnection pfb, FbTransaction pfbt, Window pparent)
        {
            this.parent = parent;
            fb = pfb;
            fbt = pfbt;
            this.pparent = pparent;
            InitializeComponent();
            substanceId = new List<int>();
            LoadSubstances();
            substanceCBs = new List<ComboBox> {SubstanceCBox};
            concentrationBs = new List<TextBox> {Concentration};
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

        private int NextId()
        {
            var selectSql = new FbCommand("SELECT MAX(ID) FROM MEDICINE;", fb) { Transaction = fbt };
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

        private void LoadSubstances()
        {
            substanceId = new List<int>();
            var selectSql =
                new FbCommand(
                    "SELECT SUBSTANCE.ID, T.NAME FROM SUBSTANCE LEFT JOIN TREATMENT T on SUBSTANCE.ID = T.ID;", fb,
                    fbt)
                { Transaction = fbt };
            var reader = selectSql.ExecuteReader();
            var selectResult = new List<string>();
            try
            {
                while (reader.Read())
                {
                    substanceId.Add(reader.GetInt32(0));
                    selectResult.Add(reader.GetString(1));
                }
            }
            finally
            {
                reader.Close();
            }

            selectSql.Dispose();
            SubstanceCBox.DataContext = selectResult;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Window warehouse = new Warehouse(pparent, fb, fbt);
            parent.Close();
            pparent.Hide();
            warehouse.Show();
        }

        private void PlusSubstanceButton_Click(object sender, RoutedEventArgs e)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = SubstanceCBox.ItemsSource,
                Width = 210,
                Height = 28,
                FontSize = 16,
                Margin = new Thickness(10, 10, 10, 10)
            };
            substanceCBs.Add(comboBox);

            var textBox = new TextBox();
            textBox.PreviewTextInput += Concentration_PreviewTextInput;
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var nxt = NextId();
            var s = "";
            var t = 0;
            while (NameBox.Text.IndexOf('\'', t) > 0)
            {
                s += NameBox.Text.Substring(t, NameBox.Text.IndexOf('\'', t) - t + 1) + '\'';
                t = NameBox.Text.IndexOf('\'', t) + 1;
            }

            s += NameBox.Text.Substring(t, NameBox.Text.Length - t);
            var insertSql = new FbCommand($"INSERT INTO MEDICINE (ID, NAME) VALUES ({nxt}, '{s}');", fb, fbt);
            insertSql.ExecuteNonQuery();
            if (SubstanceCBox.SelectedItem == null) MessageBox.Show("Оберіть хоч одну діючу речовину");
            else
            {
                foreach (var p in substanceCBs.Where(p => p.SelectedItem != null))
                {
                    insertSql = new FbCommand(
                        $"INSERT INTO INGREDIENT (KMEDICINE, KSUBSTANCE, AMOUNT) VALUES ({nxt}, {substanceId[p.SelectedIndex]}, {Convert.ToDouble(concentrationBs[substanceCBs.IndexOf(p)].Text)});",
                        fb, fbt);
                    insertSql.ExecuteNonQuery();
                    
                }
                insertSql = new FbCommand(
                    $"INSERT INTO WAREHOUSE (KMEDICINE, AMOUNT) VALUES ({nxt}, {Convert.ToDouble(AmountBox.Text)});",
                        fb, fbt);
                insertSql.ExecuteNonQuery();
                insertSql.Dispose();
                Close();
            }
        }

        private static bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text);
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
    }
}