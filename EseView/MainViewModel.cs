﻿using System;
using System.Collections.Generic;

namespace EseView
{
    class MainViewModel
    {
        public MainViewModel()
        {
            m_db = null;
            m_tables = new Lazy<List<string>>();
        }

        public MainViewModel(string filename)
        {
            OpenDatabase(filename);
        }

        public void OpenDatabase(string filename)
        {
            if (m_db != null)
            {
                m_db.Close();
            }

            m_db = new DBReader(filename);
            m_tables = new Lazy<List<string>>(() => new List<string>(m_db.Tables));
        }

        public List<string> Tables
        {
            get { return m_tables.Value; }
        }

        public IEnumerable<DBRow> Rows(string tableName)
        {
            if (m_db == null)
            {
                yield break;
            }
            else
            {
                var columnIndexByName = new Dictionary<string, int>();

                int i = 0;
                foreach (var col in m_db.GetColumnNamesAndTypes(tableName))
                {
                    columnIndexByName.Add(col.Key, i);
                }

                foreach (List<object> row in m_db.GetRows(tableName))
                {
                    yield return new DBRow(columnIndexByName, row);
                }
            }
        }

        public VirtualizedReadOnlyList<DBRow> VirtualRows(string tableName)
        {
            return new VirtualizedReadOnlyList<DBRow>(new DatabaseVirtualizedProvider(m_db, tableName));
        }

        public int GetRowCount(string tableName)
        {
            return m_db.GetRowCount(tableName);
        }

        public IEnumerable<KeyValuePair<string, Type>> GetColumnNamesAndTypes(string tableName)
        {
            if (m_db == null)
            {
                return null;
            }
            else
            {
                return m_db.GetColumnNamesAndTypes(tableName);
            }
        }

        private DBReader m_db;
        private Lazy<List<string>> m_tables;
    }
}
