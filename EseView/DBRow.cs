﻿using System;
using System.Collections.Generic;

namespace EseView
{
    public class DBRow
    {
        public DBRow(Dictionary<string, int> columnIndexByName, List<object> data)
        {
            m_columnIndexByName = columnIndexByName;
            m_data = data;
        }

        public object GetValue(string columnName)
        {
            int index = m_columnIndexByName[columnName];
            return m_data[index];
        }

        public object this[int index]
        {
            get
            {
                return m_data[index];
            }
        }

        private Dictionary<string, int> m_columnIndexByName;
        private List<object> m_data;
    }

    public class DBRowValueConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var columnName = parameter as string;
            var row = value as DBRow;

            return row.GetValue(columnName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
