﻿using System.Data;
using System.Globalization;
using System.IO;

using CsvHelper;

namespace DatabaseManager.Helper
{
    public class DataTableHelper
    {
        public static void WriteToFile(DataTable dt, string filePath)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                CsvWriter writer = new CsvWriter(sw, CultureInfo.CurrentCulture);

                foreach (DataColumn column in dt.Columns)
                {
                    writer.WriteField(column.ColumnName);
                }

                writer.NextRecord();

                foreach (DataRow row in dt.Rows)
                {
                    for (var i = 0; i < dt.Columns.Count; i++)
                    {
                        writer.WriteField(row[i]);
                    }

                    writer.NextRecord();
                }
            }
        }
    }
}
