﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EseView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel m_viewModel;
        private string m_filename;
        private string m_selectedIndex;
        private string m_selectedTable;

        public MainWindow()
        {
            m_selectedTable = null;
            m_selectedIndex = null;

            m_viewModel = new MainViewModel();
            InitializeComponent();

            TableList.DataContext = m_viewModel;
            TableList.SelectionChanged += TableList_SelectionChanged;

            StatusText.Text = "No database loaded.";
        }

        void UpdateIndexList()
        {
            IndexSelector.Items.Clear();
            NoIndex.FontWeight = FontWeights.Bold;
            IndexSelector.Items.Add(NoIndex);
            if (m_selectedTable != null)
            {
                foreach (string indexName in m_viewModel.GetIndexes(m_selectedTable))
                {
                    var item = new ComboBoxItem();
                    item.Content = indexName;
                    IndexSelector.Items.Add(item);
                }
            }
            IndexSelector.SelectedItem = NoIndex;
        }

        void TableList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IndexInfoToggle.IsChecked = false;

            string tableName = TableList.SelectedItem as string;
            m_selectedTable = tableName;
            m_selectedIndex = null;

            UpdateIndexList();

            UpdateColumnDefinitions(m_viewModel.GetColumnNamesAndTypes(tableName));
            RowData.DataContext = m_viewModel.VirtualRows(tableName, m_selectedIndex);
            UpdateStatusText(tableName);
        }

        void UpdateColumnDefinitions(IEnumerable<KeyValuePair<string, Type>> columnNamesAndTypes)
        {
            if (m_selectedTable == null)
            {
                RowGrid.Columns.Clear();
                RowData.DataContext = null;
                return;
            }

            IEnumerable<DBRow> indexInfo;
            if (m_selectedIndex != null)
            {
                indexInfo = m_viewModel.GetIndexInfo(m_selectedTable, m_selectedIndex);
            }
            else
            {
                indexInfo = new List<DBRow>(); // empty
            }

            RowGrid.Columns.Clear();

            foreach (KeyValuePair<string, Type> colspec in columnNamesAndTypes)
            {
                var cellBinding = new Binding();
                cellBinding.Converter = new DBRowValueConverter();
                cellBinding.ConverterParameter = colspec.Key;
                cellBinding.Mode = BindingMode.OneTime;

                FrameworkElementFactory cellFactory;
                if (colspec.Value == typeof(bool?))
                {
                    cellFactory = new FrameworkElementFactory(typeof(CheckBox));
                    cellFactory.SetBinding(CheckBox.IsCheckedProperty, cellBinding);

                    // HACK: Don't allow changes, but don't gray it out either like IsEnabled does.
                    cellFactory.SetValue(CheckBox.IsHitTestVisibleProperty, false);
                    cellFactory.SetValue(CheckBox.FocusableProperty, false);
                }
                else
                {
                    cellFactory = new FrameworkElementFactory(typeof(ContentControl));
                    cellFactory.SetBinding(ContentControl.ContentProperty, cellBinding);
                }

                var template = new DataTemplate();
                template.VisualTree = cellFactory;

                var gridColumn = new GridViewColumn();
                gridColumn.Header = colspec.Key;
                gridColumn.CellTemplate = template;

                // Bold the column header if it's part of the current index.
                if (indexInfo.Any(o => o.GetValue("ColumnName").Equals(colspec.Key)))
                {
                    var style = new Style(typeof(GridViewColumnHeader));
                    style.Setters.Add(new Setter(GridViewColumnHeader.FontWeightProperty, FontWeights.Bold));
                    gridColumn.HeaderContainerStyle = style;

                    // Set the column cells to bold as well.
                    cellFactory.SetValue(ContentControl.FontWeightProperty, FontWeights.Bold);
                }

                RowGrid.Columns.Add(gridColumn);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                m_filename = dialog.FileName;
                m_viewModel.OpenDatabase(dialog.FileName);

                TableList.DataContext = m_viewModel.Tables;
                TableList.SelectedIndex = -1;
            }

            Title = "EseView: " + m_filename;
            UpdateStatusText(null);

            m_selectedTable = null;
            m_selectedIndex = null;
            IndexInfoToggle.IsChecked = false;

            UpdateIndexList();
        }

        private void UpdateStatusText(string tableName)
        {
            string text = string.Format("{0} tables.", m_viewModel.Tables.Count);
            if (!string.IsNullOrEmpty(tableName))
            {
                text += string.Format(" {0} rows in current table.", m_viewModel.GetRowCount(tableName));
            }
            StatusText.Text = text;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                  "EseView by Bill Fraser <wfraser@microsoft.com>\n"
                + "\n"
                + "Note: this is not a Microsoft product;\n"
                + "         this is supported in my own free time.\n"
                + "\n"
                + "http://github.com/wfraser/EseView",
                "About EseView", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetIndex_Click(object sender, RoutedEventArgs e)
        {
            if (m_selectedTable == null)
                return;

            foreach (var item in IndexSelector.Items.OfType<ComboBoxItem>())
            {
                item.FontWeight = FontWeights.Regular;
            }

            var selected = IndexSelector.SelectedItem as ComboBoxItem;
            selected.FontWeight = FontWeights.Bold;

            if (selected == NoIndex)
            {
                m_selectedIndex = null;
            }
            else
            {
                m_selectedIndex = selected.Content as string;
            }

            if (!IndexInfoToggle.IsChecked.GetValueOrDefault(false))
            {
                UpdateColumnDefinitions(m_viewModel.GetColumnNamesAndTypes(m_selectedTable));
                RowData.DataContext = m_viewModel.VirtualRows(m_selectedTable, m_selectedIndex);
            }
            else
            {
                ShowIndexInfo();
            }
        }

        private void IndexInfo_Click(object sender, RoutedEventArgs e)
        {
            if (m_selectedTable == null)
            {
                IndexInfoToggle.IsChecked = false;
                return;
            }

            if (IndexInfoToggle.IsChecked.GetValueOrDefault(false))
            {
                ShowIndexInfo();
            }
            else
            {
                UpdateColumnDefinitions(m_viewModel.GetColumnNamesAndTypes(m_selectedTable));
                RowData.DataContext = m_viewModel.VirtualRows(m_selectedTable, m_selectedIndex);
            }
        }

        void ShowIndexInfo()
        {
            UpdateColumnDefinitions(m_viewModel.GetIndexColumnNamesAndTypes(m_selectedTable, m_selectedIndex));
            RowData.DataContext = m_viewModel.GetIndexInfo(m_selectedTable, m_selectedIndex);
        }
    }
}
