﻿using Newtonsoft.Json.Linq;
using Playnite.Controls;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PluginCommon
{
    public class Tools
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        // https://stackoverflow.com/a/48735199
        public static JArray RemoveValue(JArray oldArray, dynamic obj)
        {
            List<string> newArray = oldArray.ToObject<List<string>>();
            newArray.Remove(obj);
            return JArray.FromObject(newArray);
        }

        /// <summary>
        /// Get number week.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int WeekOfYearISO8601(DateTime date)
        {
            var day = (int)CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.AddDays(4 - (day == 0 ? 7 : day)), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Gel all control in depObj.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="depObj"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static void DesactivePlayniteWindowControl(DependencyObject depObj)
        {
            foreach (Button sp in FindVisualChildren<Button>(depObj))
            {
                if (sp.Name == "PART_ButtonMinimize")
                {
                    sp.Visibility = Visibility.Hidden;
                }
                if (sp.Name == "PART_ButtonMaximize")
                {
                    sp.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}
