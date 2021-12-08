using DatabaseInterpreter.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseInterpreter.Core
{
    public class MySqlScriptGenerator : DbScriptGenerator
    {
        public MySqlScriptGenerator(DbInterpreter dbInterpreter) : base(dbInterpreter) { }

        #region Generate Schema Scripts 

        public override ScriptBuilder GenerateSchemaScripts(SchemaInfo schemaInfo)
        {
            ScriptBuilder sb = new ScriptBuilder();

            #region Function           
            sb.AppendRange(this.GenerateScriptDbObjectScripts<Function>(schemaInfo.Functions));
            #endregion

            #region Table
            foreach (Table table in schemaInfo.Tables)
            {
                this.FeedbackInfo(OperationState.Begin, table);

                IEnumerable<TableColumn> columns = schemaInfo.TableColumns.Where(item => item.TableName == table.Name).OrderBy(item => item.Order);

                TablePrimaryKey primaryKey = schemaInfo.TablePrimaryKeys.FirstOrDefault(item => item.TableName == table.Name);
                IEnumerable<TableForeignKey> foreignKeys = schemaInfo.TableForeignKeys.Where(item => item.TableName == table.Name);
                IEnumerable<TableIndex> indexes = schemaInfo.TableIndexes.Where(item => item.TableName == table.Name).OrderBy(item => item.Order);

                ScriptBuilder sbTable = this.AddTable(table, columns, primaryKey, foreignKeys, indexes, null);

                sb.AppendRange(sbTable.Scripts);

                this.FeedbackInfo(OperationState.End, table);
            }
            #endregion            

            #region View           
            sb.AppendRange(this.GenerateScriptDbObjectScripts<View>(schemaInfo.Views));

            #endregion

            #region Trigger           
            sb.AppendRange(this.GenerateScriptDbObjectScripts<TableTrigger>(schemaInfo.TableTriggers));
            #endregion

            #region Procedure           
            sb.AppendRange(this.GenerateScriptDbObjectScripts<Procedure>(schemaInfo.Procedures));
            #endregion

            if (this.option.ScriptOutputMode.HasFlag(GenerateScriptOutputMode.WriteToFile))
            {
                this.AppendScriptsToFile(sb.ToString(), GenerateScriptMode.Schema, true);
            }

            return sb;
        }

        private void RestrictColumnLength<T>(IEnumerable<TableColumn> columns, IEnumerable<T> children) where T : SimpleColumn
        {
            if (children == null)
            {
                return;
            }

            var childColumns = columns.Where(item => children.Any(t => item.Name == t.ColumnName)).ToList();

            childColumns.ForEach(item =>
            {
                if (DataTypeHelper.IsCharType(item.DataType) && item.MaxLength > MySqlInterpreter.KeyIndexColumnMaxLength)
                {
                    item.MaxLength = MySqlInterpreter.KeyIndexColumnMaxLength;
                }
            });
        }

        private string GetRestrictedLengthName(string name)
        {
            if (name.Length > MySqlInterpreter.NameMaxLength)
            {
                return name.Substring(0, MySqlInterpreter.NameMaxLength);
            }

            return name;
        }

        #endregion

        #region Data Script    

        public override Task<string> GenerateDataScriptsAsync(SchemaInfo schemaInfo)
        {
            return base.GenerateDataScriptsAsync(schemaInfo);
        }

        protected override string GetBytesConvertHexString(object value, string dataType)
        {
            string hex = string.Concat(((byte[])value).Select(item => item.ToString("X2")));
            return $"UNHEX('{hex}')";
        }
        protected override object ParseValue(TableColumn column, object value, bool bytesAsString = false)
        {
            if (value != null)
            {
                Type type = value.GetType();
                bool needQuotated = false;
                string strValue = "";

                if (type == typeof(DBNull))
                {
                    return "NULL";
                }
                else if (type == typeof(Byte[]))
                {
                    if (((Byte[])value).Length == 16) //GUID
                    {
                        string str = ValueHelper.ConvertGuidBytesToString((Byte[])value, this.databaseType, column.DataType, column.MaxLength, bytesAsString);

                        if (!string.IsNullOrEmpty(str))
                        {
                            needQuotated = true;
                            strValue = str;
                        }
                        else
                        {
                            return value;
                        }
                    }
                    else
                    {
                        return value;
                    }
                }

                bool oracleSemicolon = false;

                switch (type.Name)
                {
                    case nameof(Guid):

                        needQuotated = true;
                        if (this.databaseType == DatabaseType.Oracle && column.DataType.ToLower() == "raw" && column.MaxLength == 16)
                        {
                            strValue = StringHelper.GuidToRaw(value.ToString());
                        }
                        else
                        {
                            strValue = value.ToString();
                        }
                        break;

                    case nameof(String):

                        needQuotated = true;
                        strValue = value.ToString();
                        if (this.databaseType == DatabaseType.Oracle)
                        {
                            if (strValue.Contains(";"))
                            {
                                oracleSemicolon = true;
                            }
                        }
                        break;

                    case nameof(DateTime):
                    case nameof(DateTimeOffset):
                    case nameof(MySql.Data.Types.MySqlDateTime):

                        if (this.databaseType == DatabaseType.Oracle)
                        {
                            if (type.Name == nameof(MySql.Data.Types.MySqlDateTime))
                            {
                                DateTime dateTime = ((MySql.Data.Types.MySqlDateTime)value).GetDateTime();

                                strValue = this.GetOracleDatetimeConvertString(dateTime);
                            }
                            else if (type.Name == nameof(DateTime))
                            {
                                DateTime dateTime = Convert.ToDateTime(value);

                                strValue = this.GetOracleDatetimeConvertString(dateTime);
                            }
                            else if (type.Name == nameof(DateTimeOffset))
                            {
                                DateTimeOffset dtOffset = DateTimeOffset.Parse(value.ToString());
                                int millisecondLength = dtOffset.Millisecond.ToString().Length;
                                string strMillisecond = millisecondLength == 0 ? "" : $".{"f".PadLeft(millisecondLength, 'f')}";
                                string format = $"yyyy-MM-dd HH:mm:ss{strMillisecond}";

                                string strDtOffset = dtOffset.ToString(format) + $"{dtOffset.Offset.Hours}:{dtOffset.Offset.Minutes}";

                                strValue = $@"TO_TIMESTAMP_TZ('{strDtOffset}','yyyy-MM-dd HH24:MI:ssxff TZH:TZM')";
                            }
                        }
                        else if (this.databaseType == DatabaseType.MySql)
                        {
                            if (type.Name == nameof(DateTimeOffset))
                            {
                                DateTimeOffset dtOffset = DateTimeOffset.Parse(value.ToString());

                                strValue = $"'{dtOffset.DateTime.Add(dtOffset.Offset).ToString("yyyy-MM-dd HH:mm:ss.ffffff")}'";
                            }
                        }

                        if (string.IsNullOrEmpty(strValue))
                        {
                            needQuotated = true;
                            strValue = value.ToString();
                        }
                        break;

                    case nameof(Boolean):

                        strValue = value.ToString() == "True" ? "1" : "0";
                        break;

                    case nameof(TimeSpan):

                        if (this.databaseType == DatabaseType.Oracle)
                        {
                            return value;
                        }
                        else
                        {
                            needQuotated = true;

                            if (column.DataType.IndexOf("datetime", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                DateTime dateTime = this.dbInterpreter.MinDateTime.AddSeconds(TimeSpan.Parse(value.ToString()).TotalSeconds);

                                strValue = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            else
                            {
                                strValue = value.ToString();
                            }
                        }
                        break;

                    case "SqlHierarchyId":
                    case "SqlGeography":
                    case "SqlGeometry":

                        needQuotated = true;
                        strValue = value.ToString();
                        break;

                    default:

                        if (string.IsNullOrEmpty(strValue))
                        {
                            strValue = value.ToString();
                        }
                        break;
                }

                if (needQuotated)
                {
                    strValue = $"{this.dbInterpreter.UnicodeInsertChar}'{ValueHelper.TransferSingleQuotation(strValue)}'";
                     

                    return strValue;
                }
                else
                {
                    return strValue;
                }
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Alter Table
        public override Script RenameTable(Table table, string newName)
        {
            return new AlterDbObjectScript<Table>($"ALTER TABLE {this.GetQuotedString(table.Name)} RENAME {this.GetQuotedString(newName)};");
        }

        public override Script SetTableComment(Table table, bool isNew = true)
        {
            return new AlterDbObjectScript<Table>($"ALTER TABLE {this.GetQuotedString(table.Name)} COMMENT = '{this.dbInterpreter.ReplaceSplitChar(ValueHelper.TransferSingleQuotation(table.Comment))}';");
        }

        public override Script AddTableColumn(Table table, TableColumn column)
        {
            return new CreateDbObjectScript<TableColumn>($"ALTER TABLE {this.GetQuotedString(table.Name)} ADD { this.dbInterpreter.ParseColumn(table, column)};");
        }

        public override Script RenameTableColumn(Table table, TableColumn column, string newName)
        {
            return new AlterDbObjectScript<TableColumn>($"ALTER TABLE {this.GetQuotedString(table.Name)} CHANGE {this.GetQuotedString(column.Name)} {newName} {this.dbInterpreter.ParseDataType(column)};");
        }

        public override Script AlterTableColumn(Table table, TableColumn column)
        {
            return new AlterDbObjectScript<TableColumn>($"ALTER TABLE {this.GetQuotedString(table.Name)} MODIFY COLUMN {this.dbInterpreter.ParseColumn(table, column)}");
        }

        public override Script SetTableColumnComment(Table table, TableColumn column, bool isNew = true)
        {
            return new AlterDbObjectScript<TableColumn>($"ALTER TABLE {this.GetQuotedString(column.TableName)} MODIFY COLUMN {this.dbInterpreter.ParseColumn(table, column)}");
        }

        public override Script DropTableColumn(TableColumn column)
        {
            return new DropDbObjectScript<TableColumn>($"ALTER TABLE {this.GetQuotedString(column.TableName)} DROP COLUMN {this.GetQuotedString(column.Name)};");
        }

        public override Script AddPrimaryKey(TablePrimaryKey primaryKey)
        {
            string columnNames = string.Join(",", primaryKey.Columns.Select(item => this.GetQuotedString(item.ColumnName)));

            string sql = $"ALTER TABLE {this.GetQuotedString(primaryKey.TableName)} ADD CONSTRAINT { this.GetQuotedString(this.GetRestrictedLengthName(primaryKey.Name))} PRIMARY KEY ({columnNames})";

            if (!string.IsNullOrEmpty(primaryKey.Comment))
            {
                sql += $" COMMENT '{this.TransferSingleQuotationString(primaryKey.Comment)}'";
            }

            return new CreateDbObjectScript<TablePrimaryKey>(sql + this.scriptsDelimiter);
        }

        public override Script DropPrimaryKey(TablePrimaryKey primaryKey)
        {
            return new DropDbObjectScript<TablePrimaryKey>($"ALTER TABLE {this.GetQuotedString(primaryKey.TableName)} DROP PRIMARY KEY");
        }

        public override Script AddForeignKey(TableForeignKey foreignKey)
        {
            string columnNames = string.Join(",", foreignKey.Columns.Select(item => this.GetQuotedString(item.ColumnName)));
            string referenceColumnName = string.Join(",", foreignKey.Columns.Select(item => $"{ this.GetQuotedString(item.ReferencedColumnName)}"));

            string sql = $"ALTER TABLE {this.GetQuotedString(foreignKey.TableName)} ADD CONSTRAINT { this.GetQuotedString(this.GetRestrictedLengthName(foreignKey.Name))} FOREIGN KEY ({columnNames}) REFERENCES { this.GetQuotedString(foreignKey.ReferencedTableName)}({referenceColumnName})";

            if (foreignKey.UpdateCascade)
            {
                sql += " ON UPDATE CASCADE";
            }
            else
            {
                sql += " ON UPDATE NO ACTION";
            }

            if (foreignKey.DeleteCascade)
            {
                sql += " ON DELETE CASCADE";
            }
            else
            {
                sql += " ON DELETE NO ACTION";
            }

            return new CreateDbObjectScript<TableForeignKey>(sql + this.scriptsDelimiter);
        }

        public override Script DropForeignKey(TableForeignKey foreignKey)
        {
            return new DropDbObjectScript<TableForeignKey>($"ALTER TABLE {this.GetQuotedString(foreignKey.TableName)} DROP FOREIGN KEY {this.GetQuotedString(foreignKey.Name)}");
        }
        public override Script AddIndex(TableIndex index)
        {
            string columnNames = string.Join(",", index.Columns.Select(item => $"{this.GetQuotedString(item.ColumnName)}"));

            string type = "";

            if (index.Type == IndexType.Unique.ToString())
            {
                type = "UNIQUE";
            }
            else if (index.Type == IndexType.FullText.ToString())
            {
                type = "FULLTEXT";
            }

            string sql = $"ALTER TABLE {this.GetQuotedString(index.TableName)} ADD {type} INDEX {this.GetQuotedString(this.GetRestrictedLengthName(index.Name))} ({columnNames})";

            if (!string.IsNullOrEmpty(index.Comment))
            {
                sql += $" COMMENT '{this.TransferSingleQuotationString(index.Comment)}'";
            }

            return new CreateDbObjectScript<TableIndex>(sql + this.scriptsDelimiter);
        }

        public override Script DropIndex(TableIndex index)
        {
            return new DropDbObjectScript<TableIndex>($"ALTER TABLE {this.GetQuotedString(index.TableName)} DROP INDEX {this.GetQuotedString(index.Name)}");
        }

        public override Script AddCheckConstraint(TableConstraint constraint)
        {
            return new Script("");
        }

        public override Script DropCheckConstraint(TableConstraint constraint)
        {
            return new Script("");
        }

        public override Script SetIdentityEnabled(TableColumn column, bool enabled)
        {
            Table table = new Table() { Owner = column.Owner, Name = column.TableName };

            return new AlterDbObjectScript<TableColumn>($"ALTER TABLE { this.GetQuotedString(column.TableName)} MODIFY COLUMN {this.dbInterpreter.ParseColumn(table, column)} {(enabled ? "AUTO_INCREMENT" : "")}");
        }

        #endregion

        #region Database Operation

        public override Script AddUserDefinedType(UserDefinedType userDefinedType) { return new Script(""); }

        public override ScriptBuilder AddTable(Table table, IEnumerable<TableColumn> columns,
            TablePrimaryKey primaryKey,
            IEnumerable<TableForeignKey> foreignKeys,
            IEnumerable<TableIndex> indexes,
            IEnumerable<TableConstraint> constraints)
        {
            ScriptBuilder sb = new ScriptBuilder();

            MySqlInterpreter mySqlInterpreter = this.dbInterpreter as MySqlInterpreter;
            string dbCharSet = mySqlInterpreter.DbCharset;
            string notCreateIfExistsClause = mySqlInterpreter.NotCreateIfExistsClause;

            string tableName = table.Name;
            string quotedTableName = this.GetQuotedObjectName(table);

            this.RestrictColumnLength(columns, primaryKey?.Columns);
            this.RestrictColumnLength(columns, foreignKeys.SelectMany(item => item.Columns));
            this.RestrictColumnLength(columns, indexes.SelectMany(item => item.Columns));

            string primaryKeyColumns = "";

            if (this.option.TableScriptsGenerateOption.GeneratePrimaryKey && primaryKey!=null)
            {
                primaryKeyColumns =
$@"
,PRIMARY KEY
(
{string.Join(Environment.NewLine, primaryKey.Columns.Select(item => $"{ this.GetQuotedString(item.ColumnName)},")).TrimEnd(',')}
)";
            }

            #region Table

            string tableScript =
$@"
CREATE TABLE {notCreateIfExistsClause} {quotedTableName}(
{string.Join("," + Environment.NewLine, columns.Select(item => this.dbInterpreter.ParseColumn(table, item)))}{primaryKeyColumns}
){(!string.IsNullOrEmpty(table.Comment) ? ($"comment='{this.dbInterpreter.ReplaceSplitChar(ValueHelper.TransferSingleQuotation(table.Comment))}'") : "")}
DEFAULT CHARSET={dbCharSet}" + this.scriptsDelimiter;

            sb.AppendLine(new CreateDbObjectScript<Table>(tableScript));

            #endregion

            //#region Primary Key
            //if (this.option.TableScriptsGenerateOption.GeneratePrimaryKey && primaryKeys.Count() > 0)
            //{
            //    TablePrimaryKey primaryKey = primaryKeys.FirstOrDefault();

            //    if (primaryKey != null)
            //    {
            //        sb.AppendLine(this.AddPrimaryKey(primaryKey));
            //    }
            //}
            //#endregion

            List<string> foreignKeysLines = new List<string>();

            #region Foreign Key
            if (this.option.TableScriptsGenerateOption.GenerateForeignKey)
            {
                foreach (TableForeignKey foreignKey in foreignKeys)
                {
                    sb.AppendLine(this.AddForeignKey(foreignKey));
                }
            }

            #endregion

            #region Index
            if (this.option.TableScriptsGenerateOption.GenerateIndex)
            {
                foreach (TableIndex index in indexes)
                {
                    sb.AppendLine(this.AddIndex(index));
                }
            }
            #endregion

            sb.AppendLine();

            return sb;
        }

        public override Script DropUserDefinedType(UserDefinedType userDefinedType)
        {
            return new Script("");
        }

        public override Script DropTable(Table table)
        {
            return new DropDbObjectScript<Table>(this.GetDropSql(nameof(Table), table));
        }

        public override Script DropView(View view)
        {
            return new DropDbObjectScript<View>(this.GetDropSql(nameof(View), view));
        }

        public override Script DropTrigger(TableTrigger trigger)
        {
            return new DropDbObjectScript<View>(this.GetDropSql("trigger", trigger));
        }

        public override Script DropFunction(Function function)
        {
            return new DropDbObjectScript<Function>(this.GetDropSql(nameof(Function), function));
        }

        public override Script DropProcedure(Procedure procedure)
        {
            return new DropDbObjectScript<Procedure>(this.GetDropSql(nameof(Procedure), procedure));
        }

        private string GetDropSql(string typeName, DatabaseObject dbObject)
        {
            return $"DROP {typeName.ToUpper()} IF EXISTS {this.GetQuotedObjectName(dbObject)};";
        }

        public override IEnumerable<Script> SetConstrainsEnabled(bool enabled)
        {
            yield return new ExecuteProcedureScript($"SET FOREIGN_KEY_CHECKS = { (enabled ? 1 : 0)};");
        }

        #endregion
    }
}
