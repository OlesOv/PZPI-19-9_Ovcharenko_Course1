using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Course
{
    public partial class Recipe
    {
        private readonly Window parent;

        public Recipe(Window parent, IReadOnlyCollection<int> ids, FbConnection pfb, FbTransaction pfbt, Account curDoc)
        {
            this.parent = parent;
            var curDoc1 = curDoc;
            InitializeComponent();
            RecipeBox.Text += "Процедури: \n";
            foreach (var p in ids)
            {
                var selectSql = new FbCommand(
                    $"SELECT TR.NAME, TH.AMOUNT, TH.INSTRUCTION FROM ILLNESSTHERAPY ILTH LEFT JOIN ILLNESS IL ON ILTH.KILLNESS = IL.ID LEFT JOIN THERAPY TH ON ILTH.KTHERAPY = TH.ID LEFT JOIN MANIPULATION MN ON TH.ID = MN.ID LEFT JOIN TREATMENT TR on MN.ID = TR.ID WHERE(ILTH.KILLNESS = {p}) AND(MN.ID IS NOT NULL);", pfb, pfbt);
                var
                    reader = selectSql
                        .ExecuteReader();
                var selectResult = "";
                try
                {
                    while (reader.Read())
                    {
                        selectResult += reader.GetString(0) + " " + reader.GetString(1) + " рази " +
                                         reader.GetString(2) + "\n і ";
                    }
                }
                finally
                {
                    reader.Close();
                }

                selectSql.Dispose(); //в документации написано, что ОЧЕНЬ рекомендуется убивать объекты этого типа, если они больше не нужны

                RecipeBox.Text += selectResult + "\n";
            }

            RecipeBox.Text = RecipeBox.Text.Substring(0, RecipeBox.Text.Length - 3);
            RecipeBox.Text += "\n\n\n Рецепт: \n";
            foreach (var p in ids)
            {
                var selectSql = new FbCommand(
                    $"SELECT MD.NAME, (TH.AMOUNT / IG.AMOUNT) DOSAGE, TH.INSTRUCTION, WH.AMOUNT FROM ILLNESSTHERAPY ILTH LEFT JOIN ILLNESS IL ON ILTH.KILLNESS = IL.ID LEFT JOIN THERAPY TH ON ILTH.KTHERAPY = TH.ID LEFT JOIN SUBSTANCE SB ON TH.ID = SB.ID LEFT JOIN TREATMENT TR on SB.ID = TR.ID LEFT JOIN INGREDIENT IG on SB.ID = IG.KSUBSTANCE LEFT JOIN MEDICINE MD on MD.ID = IG.KMEDICINE LEFT JOIN WAREHOUSE WH on MD.ID = WH.KMEDICINE WHERE(ILTH.KILLNESS = {p}) AND(SB.ID IS NOT NULL) AND(WH.AMOUNT > 0);", pfb, pfbt); //задаем запрос на выборку
                var
                    reader = selectSql
                        .ExecuteReader(); //для запросов, которые возвращают результат в виде набора данных надо использоваться метод ExecuteReader()
                var selectResult = ""; //в эту переменную будем складывать результат запроса Select
                try
                {
                    while (reader.Read())
                    {
                        selectResult += reader.GetString(0) + " " + reader.GetString(1) + " д. " +
                                         reader.GetString(2) + "\n АБО ";
                    }
                }
                finally
                {
                    reader.Close();
                }

                selectSql.Dispose(); //в документации написано, что ОЧЕНЬ рекомендуется убивать объекты этого типа, если они больше не нужны

                RecipeBox.Text += selectResult + "\n";
            }

            if (RecipeBox.Text[^5] == 'А')
                RecipeBox.Text = RecipeBox.Text.Substring(0, RecipeBox.Text.Length - 5);

            var getSpeciality =
                new FbCommand(
                    $"SELECT * FROM DOCTOR DC LEFT JOIN SPECIALTY SP ON SP.ID = DC.KSPECIALTY WHERE DC.ID = {curDoc1.Id};", pfb, pfbt);
            var sPreader = getSpeciality.ExecuteReader();
            var spstring = "\n\nРецепт виписав ";
            try
            {
                while (sPreader.Read())
                {
                    spstring += sPreader.GetString(3) + " " + curDoc1.Login + "\n";
                }
            }
            finally
            {
                sPreader.Close();
            }

            getSpeciality.Dispose();

            RecipeBox.Text += spstring;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(RecipeBox, "Друк Рецепта");
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            parent.Show();
        }

    }
}