using LiveCharts;
using LiveCharts.Wpf;
using Sharp7;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace StatTools
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Thread
        private static BackgroundWorker bwWork = new BackgroundWorker();

        // Création du client
        public static S7Client cpu = new S7Client();

        // Variables
        private static bool statusCon;
        private static double[] items = new double[24];

        public MainWindow()
        {
            InitializeComponent();

            // Initialisation de la tâche de fond (travail)
            bwWork.WorkerReportsProgress = true;
            bwWork.WorkerSupportsCancellation = true;
            bwWork.DoWork += new DoWorkEventHandler(bwWork_DoWork);
            bwWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwWork_RunWorkerCompleted);

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
                }
            };

            Labels = new[] { "0h", "1h", "2h", "3h", "4h", "5h", "6h", "7h", "8h", "9h", "10h", "11h", "12h",
            "13h", "14h", "15h", "16h", "17h", "18h", "19h", "20h", "21h", "22h", "23h"};
            Formatter = value => value.ToString("N");

            DataContext = this;
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }

        private void bwWork_DoWork(object sender, DoWorkEventArgs e)
        {
            //BackgroundWorker worker = sender as BackgroundWorker;

            // Status de la connexion
            var res = -1;
            byte[] Buffer = new byte[1];
            res = cpu.DBRead(94, 0, 1, Buffer);

            if (res == 0)
                statusCon = true;
            else
                statusCon = false;

            Thread.Sleep(1000);
        }

        private void bwWork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Status de la connexion
            if (statusCon)
                label1.Content = "Connecté";
            else
                label1.Content = "Déconnecté";

            bwWork.RunWorkerAsync();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cpu.ConnectTo("127.0.0.1", 0, 2);
            bwWork.RunWorkerAsync();
        }

        private static double[] ReadCountersProduction(int nbDays)
        {
            //-------------- Read DB98
            var res = -1; // Result of the function
            byte[] DB98Buffer = new byte[100]; // Buffer 100 bytes
            double[] temp = new double[24]; // Array one day
            int Start, Size; // Parameters of the function
            int hour = DateTime.Now.Hour;
            //hour = hour - 1;

            if (nbDays == 0)
            {
                Start = 18; // Start at DB98.DBD18
                Size = hour * 4; // Read from current hour to the begin of the day
            }
            else
            {
                if (nbDays == 1)
                    Start = 18 + (hour * 4);
                else
                    Start = 18 + (hour * 4) + ((nbDays - 1) * 24 * 4);

                Size = 24 * 4; // Read one day
            }

            if (statusCon)
                res = cpu.DBRead(98, Start, Size, DB98Buffer); // Read DB98

            if (!(res == 0))
                return temp; // Break the function

            if ((nbDays == 0) & (res == 0))
            {
                int j = hour - 1;
                for (int i = 0; i < hour; i++)
                {
                    temp[j] = S7.GetDIntAt(DB98Buffer, i * 4); // Fill the array
                    j = j - 1;
                }
            }
            if ((nbDays > 0) & (res == 0))
            {
                int j = 23;
                for (int i = 0; i < 23; i++)
                {
                    temp[j] = S7.GetDIntAt(DB98Buffer, i * 4); // Fill the array
                    j = j - 1;
                }
            }

            return temp; // Return values
        }

        private void Label1_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            cpu.ConnectTo("127.0.0.1", 0, 2);
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            items = ReadCountersProduction(selectDay.SelectedIndex);

            for (int i = 0; i < items.Length; i++)
            {
                SeriesCollection[0].Values[i] = items[i];
            }
        }
    }
}