using System;
using System.Collections.Generic;
using System.Windows.Input; 
using System.Drawing;
using System.IO;
using System.Media;
using System.Web.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
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
            _maxPosition = 0;
            _maxForce = 0;
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
            if(openFileDialog.FileName.ToLower().Contains(".json"))
                loadGraph(openFileDialog.FileName);
        }
        /// <summary>
        /// Load the JSON, find all dates in the file and add them to the combo box for selection 
        /// </summary>
        /// <param name="fileName"></param>
        private void loadGraph(string fileName)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait; 

            _json = File.Open(fileName, FileMode.Open);
            StreamReader reader = new StreamReader(_json);
            string json = reader.ReadToEnd();
            reader.Close(); 

            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            _obj = null;  
            serializer.MaxJsonLength = Int32.MaxValue;
            var deserialized = serializer.DeserializeObject(json);

            if (deserialized != null)
            {
                var dictValues = deserialized as IDictionary<string, object>;
                if (dictValues != null)
                    _obj = new DynamicJsonObject(dictValues);
                var arrayValues = deserialized as object[];
                if (arrayValues != null)
                {
                    _obj = new DynamicJsonArray(arrayValues);
                }
            }
            Chart positionChart = this.FindName("Position") as Chart;
            positionChart.ChartAreas.Clear(); 
            positionChart.Series.Clear();
            positionChart.BackColor = System.Drawing.Color.AliceBlue; 
            Series chartPoints =positionChart.Series.Add("Points");
            chartPoints.ChartType = SeriesChartType.Line;

            Chart forceChart = this.FindName("Force") as Chart;
            forceChart.ChartAreas.Clear();
            forceChart.Series.Clear();
            forceChart.BackColor = System.Drawing.Color.AliceBlue;
            Series forcePoints = forceChart.Series.Add("Points");
            forcePoints.ChartType = SeriesChartType.Line;

            ComboBox combo = this.FindName("Combo_Box") as ComboBox;
            combo.SelectionChanged -= Combo_Box_SelectionChanged;
            combo.Items.Clear();
            combo.SelectionChanged += Combo_Box_SelectionChanged;
          
     
            foreach (dynamic obj in _obj)
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
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
        }

        /// <summary>
        /// When the combo box selection changes, populate graphs with data from the selected date 
        /// </summary>
        /// <param name="sender">The combo box triggering the event</param>
        /// <param name="e">Not used here, but a defualt part of the event callback</param>
        private void Combo_Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //move the stream reader back to the beginning so that we can pars full doc
            CheckBox overlay = this.FindName("Overlay_Multiple") as CheckBox;
            bool t_overlay = overlay.IsChecked == true;
            if (!t_overlay)
            {
                _overLays = 0; 
            }
            if(_overLays == 0)
            {
                _maxForce = 0;
                _maxPosition = 0; 
            }
  
            Chart positionChart = this.FindName("Position") as Chart;
            if(!t_overlay)
            {
                positionChart.Series.Clear(); 
            }
            positionChart.BackColor = System.Drawing.Color.AliceBlue;
            Series chartPoints = positionChart.Series.Add(_overLays.ToString());
            chartPoints.ChartType = SeriesChartType.Line;
            
            Chart forceChart = this.FindName("Force") as Chart;
            if(!t_overlay)
            {
                forceChart.Series.Clear();
            }
            forceChart.BackColor = System.Drawing.Color.AliceBlue;
            Series forcePoints = forceChart.Series.Add(_overLays.ToString());
            forcePoints.ChartType = SeriesChartType.Line;
            //reset th current points 
            forcePoints.Points.Clear();
            chartPoints.Points.Clear(); 
            
            forcePoints.Color = this._lineColor[_overLays % 5];
            chartPoints.Color = this._lineColor[_overLays % 5];
            //cast the sender so we can get th item info 
            ComboBox comboBox = (ComboBox)sender; 
            ComboBoxItem item = comboBox.SelectedItem as ComboBoxItem;
            if(item == null)
            {
                comboBox.SelectionChanged -= Combo_Box_SelectionChanged; 
                comboBox.SelectedIndex = 0;
                comboBox.SelectionChanged += Combo_Box_SelectionChanged;
                item = comboBox.SelectedItem as ComboBoxItem;
            }
            string dateTime = item.Content as string;
            //find the data of the matching date and populate chart
            foreach (dynamic obj in _obj)
            {
                if (obj.createdDate == dateTime)
                {
                    TextBlock block = this.FindName("Status") as TextBlock;
                    block.Text = obj.status;
                    block = this.FindName("MaxForce") as TextBlock;
                    if(obj.maxForce > _maxForce)
                    {
                        _maxForce = obj.maxForce;
                        block.Text = obj.maxForce.ToString();
                        block.Foreground = _textColor[_overLays % 5];
                    }
                  
                    block = this.FindName("MaxPosition") as TextBlock;
                    if(obj.maxPosition > _maxPosition)
                    {
                        _maxPosition = obj.maxPosition;
                        block.Text = obj.maxPosition.ToString();
                        block.Foreground = _textColor[_overLays % 5];
                    }
                  
                    if (obj.cycleTime != null)
                    {
                        var points = obj.Points;
                        int i = 0;
                        double cycleTime = ((double)obj.cycleTime) / points.Length;
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
           
            StackPanel DisplayedValues = this.FindName("Graph_Panel") as StackPanel;
            if(!t_overlay)
            {
                while(DisplayedValues.Children.Count > 5)
                {
                    DisplayedValues.Children.RemoveAt(0); 
                }
            }
            TextBlock text = new TextBlock(); 
            text.Text = dateTime;
            text.FontSize = 18;
            text.Foreground = _textColor[_overLays % _textColor.Count];
            DisplayedValues.Children.Insert(_overLays, text);
            _overLays++; 



        }
        //private variable to stor file stream loaded from selected JSON file 
        private FileStream _json;
        private int _overLays;
        private Dictionary<int, System.Drawing.Color> _lineColor = new Dictionary<int, System.Drawing.Color> { { 0, System.Drawing.Color.Black }, { 1, System.Drawing.Color.Blue }, { 2, System.Drawing.Color.Brown },
                                                                                                                { 3, System.Drawing.Color.DarkBlue }, {4, System.Drawing.Color.DarkGreen } };
        private Dictionary<int, System.Windows.Media.Brush> _textColor = new Dictionary<int, System.Windows.Media.Brush> { { 0, System.Windows.Media.Brushes.Black }, { 1,System.Windows.Media.Brushes.Blue }, 
                                                                                                                 { 2, System.Windows.Media.Brushes.Brown }, { 3,System.Windows.Media.Brushes.DarkBlue }, {4, System.Windows.Media.Brushes.DarkGreen } };
        private decimal _maxForce;
        private decimal _maxPosition;
        private dynamic _obj; 
    }

       
    
}
