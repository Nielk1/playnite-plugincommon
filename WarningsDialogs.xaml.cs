﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PluginCommon
{
    /// <summary>
    /// Logique d'interaction pour WarningsDialogs.xaml
    /// </summary>
    public partial class WarningsDialogs : Window
    {
        public WarningsDialogs(string Caption, List<JArray> Messages)
        {
            InitializeComponent();

            tbCaption.Text = Caption;

            List<WarningData> MessagesData = new List<WarningData>();
            for (int i = 0; i < Messages.Count; i++)
            {
                MessagesData.Add(new WarningData
                {
                    At = (string)Messages[i][0]["At"],
                    FpsData = new Data { Name = (string)Messages[i][1]["Name"], Value = (int)Messages[i][1]["Value"], isWarm = (bool)Messages[i][1]["isWarm"] },
                    CpuTempData = new Data { Name = (string)Messages[i][2]["Name"], Value = (int)Messages[i][2]["Value"], isWarm = (bool)Messages[i][2]["isWarm"] },
                    GpuTempData = new Data { Name = (string)Messages[i][3]["Name"], Value = (int)Messages[i][3]["Value"], isWarm = (bool)Messages[i][3]["isWarm"] },
                    CpuUsageData = new Data { Name = (string)Messages[i][4]["Name"], Value = (int)Messages[i][4]["Value"], isWarm = (bool)Messages[i][4]["isWarm"] },
                    GpuUsageData = new Data { Name = (string)Messages[i][5]["Name"], Value = (int)Messages[i][5]["Value"], isWarm = (bool)Messages[i][5]["isWarm"] },
                    RamUsageData = new Data { Name = (string)Messages[i][6]["Name"], Value = (int)Messages[i][6]["Value"], isWarm = (bool)Messages[i][6]["isWarm"] }
                });
            }
            icData.ItemsSource = MessagesData;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class SetTextColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return Brushes.Orange;
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class WarningData
    {
        public string At { get; set; }
        public Data FpsData { get; set; }
        public Data CpuTempData { get; set; }
        public Data GpuTempData { get; set; }
        public Data CpuUsageData { get; set; }
        public Data GpuUsageData { get; set; }
        public Data RamUsageData { get; set; }
    }
    public class Data
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public bool isWarm { get; set; }
    }
}
