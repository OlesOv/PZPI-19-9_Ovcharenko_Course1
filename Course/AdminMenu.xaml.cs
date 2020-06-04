using FirebirdSql.Data.FirebirdClient;
using System;
using System.Windows;

namespace Course
{
    public partial class AdminMenu : Window
    {
        private readonly FbConnection fb;
        private readonly Window parent;
        private readonly FbTransaction fbt;
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            parent.Show();
        }
        public AdminMenu(Window parent, FbConnection pfb, FbTransaction pfbt)
        {
            this.parent = parent;
            fb = pfb;
            fbt = pfbt;
            InitializeComponent();
            DoctorsButton.Click += DoctorsButton_Click;
            WarehouseButton.Click += WarehouseButton_Click;
            ExitButton.Click += ExitButton_Click;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            var warehouse = new Warehouse(this, fb, fbt);
            Hide();
            warehouse.Show();
        }

        private void DoctorsButton_Click(object sender, RoutedEventArgs e)
        {
            var doctors = new Doctors(this, fb, fbt);
            Hide();
            doctors.Show();
        }
    }
}
