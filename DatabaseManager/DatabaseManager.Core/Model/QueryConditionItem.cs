﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using DatabaseInterpreter.Core;

namespace DatabaseManager.Model
{
    public class QueryConditionItem
    {
        public string ColumnName { get; set; }
        public Type DataType { get; set; }
        public QueryConditionMode Mode { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public List<string> Values { get; set; } = new List<string>();
        public bool NeedQuoted => ValueHelper.NeedQuotedForSql(this.DataType);

        private string GetValue(string value)
        {
            return this.NeedQuoted ? $"'{ this.GetSafeValue(value)}'" : value;
        }

        private string GetSafeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            value = Regex.Replace(value, @";", string.Empty);
            value = Regex.Replace(value, @"'", string.Empty);
            value = Regex.Replace(value, @"&", string.Empty);
            value = Regex.Replace(value, @"%20", string.Empty);
            value = Regex.Replace(value, @"--", string.Empty);
            value = Regex.Replace(value, @"==", string.Empty);
            value = Regex.Replace(value, @"<", string.Empty);
            value = Regex.Replace(value, @">", string.Empty);
            value = Regex.Replace(value, @"%", string.Empty);

            return value;
        }

        public override string ToString()
        {
            string conditon = "";

            if (this.Mode == QueryConditionMode.Single)
            {
                string value = this.Operator.Contains("LIKE") ? $"'%{this.Value}%'" : this.GetValue(this.Value);

                conditon = $"{this.Operator} {value}";
            }
            else if (this.Mode == QueryConditionMode.Range)
            {
                conditon = $"BETWEEN {this.GetValue(this.From)} AND {this.GetValue(this.To)}";
            }
            else if (this.Mode == QueryConditionMode.Series)
            {
                conditon = $"IN({string.Join(",", this.Values.Select(item => this.GetValue(item)))})";
            }

            return conditon;
        }
    }

    public enum QueryConditionMode
    {
        Single = 0,
        Range = 1,
        Series = 2
    }
}
