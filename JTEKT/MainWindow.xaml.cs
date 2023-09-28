using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Chart = System.Windows.Forms.DataVisualization.Charting.Chart;

namespace JTEKT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Browse for the JSON file that stores history data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files| *.json";
            openFileDialog.ShowDialog();
            loadGraph(openFileDialog.FileName);
        }
        /// <summary>
        /// Load the JSON, find all dates in the file and add them to the combo box for selection 
        /// </summary>
        /// <param name="fileName"></param>
        private void loadGraph(string fileName)
        {
            _json = File.Open(fileName, FileMode.Open);
            StreamReader reader = new StreamReader(_json);
            string json = reader.ReadToEnd();
            dynamic Obj = System.Web.Helpers.Json.Decode(json);
            Chart positionChart = this.FindName("Position") as Chart;
            positionChart.BackColor = System.Drawing.Color.AliceBlue; 
            Series chartPoints =positionChart.Series.Add("Points");
            chartPoints.ChartType = SeriesChartType.Line;
            Chart forceChart = this.FindName("Force") as Chart;
            forceChart.BackColor = System.Drawing.Color.AliceBlue;
            Series forcePoints = forceChart.Series.Add("Points");
            forcePoints.ChartType = SeriesChartType.Line;

            ComboBox combo = this.FindName("Combo_Box") as ComboBox;
     
            foreach (dynamic obj in Obj)
             {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = obj.createdDate;
                    combo.Items.Add(item); 
             }

            chartPoints.Color = System.Drawing.Color.Blue;
            chartPoints.Enabled = true;
            chartPoints.XValueType = ChartValueType.Double;
            ChartArea posArea = positionChart.ChartAreas.Add("Time");
            posArea.AxisX.Title = "Time (s)";
            posArea.AxisY.Title = "Position (mm)"; 

            ChartArea forceArea = forceChart.ChartAreas.Add("Time");
            forceArea.AxisX.Title = "Time (s)";
            forceArea.AxisY.Title = "Force (N)";
            combo.SelectedIndex = 0; 
        }
        /// <summary>
        /// When the combo box selection changes, populate graphs with data from the selected date 
        /// </summary>
        /// <param name="sender">The combo box triggering the event</param>
        /// <param name="e">Not used here, but a defualt part of the event callback</param>
        private void Combo_Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //move the stream reader back to the beginning so that we can pars full doc
            _json.Position = 0;
            StreamReader reader = new StreamReader(_json);
            string json = reader.ReadToEnd();
            dynamic Obj = System.Web.Helpers.Json.Decode(json);
            Chart positionChart = this.FindName("Position") as Chart;
            positionChart.BackColor = System.Drawing.Color.AliceBlue;
            Series chartPoints = positionChart.Series.FindByName("Points");
            chartPoints.ChartType = SeriesChartType.Line;
            Chart forceChart = this.FindName("Force") as Chart;
            forceChart.BackColor = System.Drawing.Color.AliceBlue;
            Series forcePoints = forceChart.Series.FindByName("Points");
            forcePoints.ChartType = SeriesChartType.Line;
            //reset th current points 
            forcePoints.Points.Clear();
            chartPoints.Points.Clear(); 
            //cast the sender so we can get th item info 
            ComboBox comboBox = (ComboBox)sender; 
            ComboBoxItem item = comboBox.SelectedItem as ComboBoxItem;
            string dateTime = item.Content as string;
            //find the data of the matching date and populate chart
            foreach (dynamic obj in Obj)
            {
                if (obj.createdDate == dateTime)
                {
                    TextBlock block = this.FindName("Status") as TextBlock;
                    block.Text = obj.status;
                    block = this.FindName("MaxForce") as TextBlock;
                    block.Text = obj.maxForce.ToString();
                    block = this.FindName("MaxPosition") as TextBlock;
                    block.Text = obj.maxPosition.ToString(); 
                    if (obj.cycleTime != null)
                    {
                        var points = obj.Points;
                        int i = 0;
                        double cycleTime = ((double)obj.cycleTime) / 1000.0;
                        foreach (var point in points)
                        {
                            chartPoints.Points.AddXY(cycleTime * i, (double)point.X);
                            forcePoints.Points.AddXY(cycleTime * i, (double)point.Y);
                            i++;
                        }
                    }
                    break;
                }
            }


        }
        //private variable to stor file stream loaded from selected JSON file 
        private FileStream _json;
    }

       
    
}
