﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using DatabaseInterpreter.Core;
using DatabaseInterpreter.Model;
using DatabaseInterpreter.Utility;

using DatabaseManager.Core;
using DatabaseManager.Helper;
using DatabaseManager.Model;

namespace DatabaseManager.Controls
{
    public delegate void GeneateChangeScriptsHandler();

    public partial class UC_TableDesigner : UserControl, IDbObjContentDisplayer, IObserver<FeedbackInfo>
    {
        private readonly string selfTableName = "<self>";

        private DatabaseObjectDisplayInfo displayInfo;

        public FeedbackHandler OnFeedback;

        public DbInterpreterHelper DbInterpreterHelper = new DbInterpreterHelper(new Dictionary<DatabaseType, IDbInterpreterFactory>() { { DatabaseType.SqlServer, new SqlServerDbInterpreterFactory() } });

        public UC_TableDesigner()
        {
            InitializeComponent();

            this.ucIndexes.OnColumnSelect += this.ShowColumnSelector;
            this.ucForeignKeys.OnColumnMappingSelect += this.ShowColumnMappingSelector;
        }

        private void UC_TableDesigner_Load(object sender, EventArgs e)
        {
            this.ucColumns.OnGenerateChangeScripts += this.GeneateChangeScripts;
            this.ucIndexes.OnGenerateChangeScripts += this.GeneateChangeScripts;
            this.ucForeignKeys.OnGenerateChangeScripts += this.GeneateChangeScripts;
            this.ucConstraints.OnGenerateChangeScripts += this.GeneateChangeScripts;
        }

        private async void InitControls()
        {
            if (this.displayInfo.DatabaseType == DatabaseType.MySql)
            {
                this.tabControl1.TabPages.Remove(this.tabConstraints);
            }

            DbInterpreter dbInterpreter = this.GetDbInterpreter();

            List<UserDefinedType> userDefinedTypes = await dbInterpreter.GetUserDefinedTypesAsync();

            this.ucColumns.UserDefinedTypes = userDefinedTypes;
            this.ucColumns.InitControls();

            if (this.displayInfo.IsNew)
            {
                this.LoadDatabaseOwners();
            }
            else
            {
                this.cboOwner.Enabled = false;

                SchemaInfoFilter filter = new SchemaInfoFilter() { Strict = true, TableNames = new string[] { this.displayInfo.Name } };
                filter.DatabaseObjectType = DatabaseObjectType.Table | DatabaseObjectType.TableColumn | DatabaseObjectType.TablePrimaryKey;

                SchemaInfo schemaInfo = await dbInterpreter.GetSchemaInfoAsync(filter);

                Table table = schemaInfo.Tables.FirstOrDefault();

                if (table != null)
                {
                    this.txtTableName.Text = table.Name;
                    this.cboOwner.Text = table.Owner;
                    this.txtTableComment.Text = table.Comment;

                    #region Load Columns

                    List<TableColumnDesingerInfo> columnDesingerInfos = ColumnManager.GetTableColumnDesingerInfos(dbInterpreter, table, schemaInfo.TableColumns, schemaInfo.TablePrimaryKeys);

                    this.ucColumns.LoadColumns(table, columnDesingerInfos);

                    #endregion Load Columns
                }
                else
                {
                    MessageBox.Show("Table is not existed");
                }
            }
        }

        private async void LoadDatabaseOwners()
        {
            DbInterpreter dbInterpreter = this.GetDbInterpreter();

            List<string> items = new List<string>();
            string defaultItem = null;

            List<DatabaseOwner> owners = await dbInterpreter.GetDatabaseOwnersAsync();

            items.AddRange(owners.Select(item => item.Name));

            if (this.displayInfo.DatabaseType == DatabaseType.SqlServer)
            {
                defaultItem = "dbo";
            }
            else if (this.displayInfo.DatabaseType == DatabaseType.Oracle)
            {
                this.cboOwner.Enabled = false;
                defaultItem = (this.GetDbInterpreter() as OracleInterpreter).GetDbOwner();
            }
            else if (this.displayInfo.DatabaseType == DatabaseType.MySql)
            {
                this.cboOwner.Enabled = false;
                defaultItem = dbInterpreter.ConnectionInfo.Database;
            }

            cboOwner.Items.AddRange(items.ToArray());

            if (cboOwner.Items.Count == 1)
            {
                cboOwner.SelectedIndex = 0;
            }
            else
            {
                if (defaultItem != null)
                {
                    cboOwner.Text = defaultItem;
                }
            }
        }

        public void Show(DatabaseObjectDisplayInfo displayInfo)
        {
            this.displayInfo = displayInfo;
            this.ucColumns.DatabaseType = displayInfo.DatabaseType;
            this.ucIndexes.DatabaseType = displayInfo.DatabaseType;
            this.ucForeignKeys.DatabaseType = displayInfo.DatabaseType;
            this.ucConstraints.DatabaseType = displayInfo.DatabaseType;

            this.InitControls();
        }

        private DbInterpreter GetDbInterpreter()
        {
            DbInterpreter dbInterpreter = DbInterpreterHelper.GetDbInterpreter(this.displayInfo.DatabaseType, this.displayInfo.ConnectionInfo, new DbInterpreterOption());

            return dbInterpreter;
        }

        private SchemaDesignerInfo GetSchemaDesingerInfo()
        {
            SchemaDesignerInfo schemaDesingerInfo = new SchemaDesignerInfo();

            TableDesignerInfo tableDesignerInfo = new TableDesignerInfo()
            {
                Name = this.txtTableName.Text.Trim(),
                Owner = this.cboOwner.Text.Trim(),
                Comment = this.txtTableComment.Text.Trim(),
                OldName = this.displayInfo.DatabaseObject?.Name
            };

            schemaDesingerInfo.TableDesignerInfo = tableDesignerInfo;

            List<TableColumnDesingerInfo> columns = this.ucColumns.GetColumns();

            columns.ForEach(item => { item.Owner = tableDesignerInfo.Owner; item.TableName = tableDesignerInfo.Name; });

            schemaDesingerInfo.TableColumnDesingerInfos.AddRange(columns);

            if (this.tabIndexes.Tag == null)
            {
                schemaDesingerInfo.IgnoreTableIndex = true;
            }
            else
            {
                List<TableIndexDesignerInfo> indexes = this.ucIndexes.GetIndexes();

                indexes.ForEach(item => { item.Owner = tableDesignerInfo.Owner; item.TableName = tableDesignerInfo.Name; });

                schemaDesingerInfo.TableIndexDesingerInfos.AddRange(indexes);
            }

            if (this.tabForeignKeys.Tag == null)
            {
                schemaDesingerInfo.IgnoreTableForeignKey = true;
            }
            else
            {
                List<TableForeignKeyDesignerInfo> foreignKeys = this.ucForeignKeys.GetForeignKeys();

                foreignKeys.ForEach(item =>
                {
                    item.Owner = tableDesignerInfo.Owner; item.TableName = tableDesignerInfo.Name;

                    if (item.ReferencedTableName == this.selfTableName)
                    {
                        item.ReferencedTableName = tableDesignerInfo.Name;
                        item.Name = item.Name.Replace(this.selfTableName, tableDesignerInfo.Name);
                    }
                });

                schemaDesingerInfo.TableForeignKeyDesignerInfos.AddRange(foreignKeys);
            }

            if (this.tabConstraints.Tag == null)
            {
                schemaDesingerInfo.IgnoreTableConstraint = true;
            }
            else
            {
                List<TableConstraintDesignerInfo> constraints = this.ucConstraints.GetConstraints();

                constraints.ForEach(item => { item.Owner = tableDesignerInfo.Owner; item.TableName = tableDesignerInfo.Name; });

                schemaDesingerInfo.TableConstraintDesignerInfos.AddRange(constraints);
            }

            return schemaDesingerInfo;
        }

        public ContentSaveResult Save(ContentSaveInfo info)
        {
            this.EndControlsEdit();

            ContentSaveResult result = Task.Run(() => this.SaveTable()).Result;

            if (!result.IsOK)
            {
                MessageBox.Show(result.Message);
            }
            else
            {
                this.Feedback("Table saved.");

                Table table = result.ResultData as Table;

                this.displayInfo.DatabaseObject = table;
                this.ucColumns.OnSaved();
                this.ucIndexes.OnSaved();
                this.ucForeignKeys.OnSaved();
                this.ucConstraints.OnSaved();

                if (this.displayInfo.IsNew || table.Name != this.displayInfo.Name)
                {
                    if (FormEventCenter.OnRefreshNavigatorFolder != null)
                    {
                        FormEventCenter.OnRefreshNavigatorFolder();
                    }
                }
            }

            return result;
        }

        private TableManager GetTableManager()
        {
            DbInterpreter dbInterpreter = this.GetDbInterpreter();

            TableManager tableManager = new TableManager(dbInterpreter);

            return tableManager;
        }

        private async Task<ContentSaveResult> SaveTable()
        {
            SchemaDesignerInfo schemaDesignerInfo = this.GetSchemaDesingerInfo();

            TableManager tableManager = this.GetTableManager();

            this.Feedback("Saving table...");

            return await tableManager.Save(schemaDesignerInfo, this.displayInfo.IsNew);
        }

        private void EndControlsEdit()
        {
            this.ucColumns.EndEdit();
            this.ucIndexes.EndEdit();
            this.ucForeignKeys.EndEdit();
            this.ucConstraints.EndEdit();
        }

        private async void GeneateChangeScripts()
        {
            this.EndControlsEdit();

            SchemaDesignerInfo schemaDesignerInfo = this.GetSchemaDesingerInfo();

            TableManager tableManager = this.GetTableManager();

            this.Feedback("Generating changed scripts...");

            ContentSaveResult result = await tableManager.GenerateChangeScripts(schemaDesignerInfo, this.displayInfo.IsNew);

            this.Feedback("End generate changed scripts.");

            if (!result.IsOK)
            {
                MessageBox.Show(result.Message);
            }
            else
            {
                TableDesignerGenerateScriptsData scriptsData = result.ResultData as TableDesignerGenerateScriptsData;

                string scripts = string.Join(Environment.NewLine, scriptsData.Scripts.Select(item => item.Content));

                frmScriptsViewer scriptsViewer = new frmScriptsViewer() { DatabaseType = this.displayInfo.DatabaseType };
                scriptsViewer.LoadScripts(StringHelper.ToSingleEmptyLine(scripts).Trim());

                scriptsViewer.ShowDialog();
            }
        }

        private async void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabPage tabPage = this.tabControl1.SelectedTab;

            if (tabPage == null)
            {
                return;
            }

            Table table = new Table() { Owner = this.cboOwner.Text, Name = this.txtTableName.Text.Trim() };

            DbInterpreter dbInterpreter = this.GetDbInterpreter();

            if (tabPage.Name == this.tabIndexes.Name)
            {
                tabPage.Tag = 1;

                this.ucIndexes.Table = table;

                if (!this.ucIndexes.Inited)
                {
                    this.ucIndexes.InitControls(dbInterpreter);
                }

                if (!this.displayInfo.IsNew)
                {
                    if (!this.ucIndexes.LoadedData)
                    {
                        SchemaInfoFilter filter = new SchemaInfoFilter();
                        filter.TableNames = new string[] { this.displayInfo.Name };

                        List<TableIndex> tableIndexes = await dbInterpreter.GetTableIndexesAsync(filter, true);

                        this.ucIndexes.LoadIndexes(IndexManager.GetIndexDesignerInfos(this.displayInfo.DatabaseType, tableIndexes));
                    }
                }

                IEnumerable<TableColumnDesingerInfo> columns = this.ucColumns.GetColumns().Where(item => !string.IsNullOrEmpty(item.Name) && item.IsPrimary);

                this.ucIndexes.LoadPrimaryKeys(columns);
            }
            else if (tabPage.Name == this.tabForeignKeys.Name)
            {
                tabPage.Tag = 1;

                this.ucForeignKeys.Table = table;

                if (!this.ucForeignKeys.Inited)
                {
                    dbInterpreter.Option.ObjectFetchMode = DatabaseObjectFetchMode.Simple;

                    List<Table> tables = await dbInterpreter.GetTablesAsync();

                    if (this.displayInfo.IsNew)
                    {
                        tables.Add(new Table() { Name = "<self>" });
                    }

                    this.ucForeignKeys.InitControls(tables);
                }

                if (!this.displayInfo.IsNew)
                {
                    if (!this.ucForeignKeys.LoadedData)
                    {
                        SchemaInfoFilter filter = new SchemaInfoFilter();
                        filter.TableNames = new string[] { this.displayInfo.Name };

                        dbInterpreter.Option.ObjectFetchMode = DatabaseObjectFetchMode.Details;

                        List<TableForeignKey> foreignKeys = await dbInterpreter.GetTableForeignKeysAsync(filter);

                        this.ucForeignKeys.LoadForeignKeys(IndexManager.GetForeignKeyDesignerInfos(foreignKeys));
                    }
                }
            }
            else if (tabPage.Name == this.tabConstraints.Name)
            {
                tabPage.Tag = 1;

                if (!this.ucConstraints.Inited)
                {
                    this.ucConstraints.InitControls();
                }

                if (!this.displayInfo.IsNew)
                {
                    if (!this.ucConstraints.LoadedData)
                    {
                        SchemaInfoFilter filter = new SchemaInfoFilter();
                        filter.TableNames = new string[] { this.displayInfo.Name };

                        List<TableConstraint> constraints = await dbInterpreter.GetTableConstraintsAsync(filter);

                        this.ucConstraints.LoadConstraints(IndexManager.GetConstraintDesignerInfos(constraints));
                    }
                }
            }
        }

        private void ShowColumnSelector(DatabaseObjectType databaseObjectType, IEnumerable<IndexColumn> values, bool columnIsReadonly)
        {
            frmColumSelect columnSelect = new frmColumSelect() { ColumnIsReadOnly = columnIsReadonly };

            IEnumerable<TableColumnDesingerInfo> columns = this.ucColumns.GetColumns().Where(item => !string.IsNullOrEmpty(item.Name));

            List<IndexColumn> columnInfos = new List<IndexColumn>();

            foreach (TableColumnDesingerInfo column in columns)
            {
                if (databaseObjectType == DatabaseObjectType.TableIndex)
                {
                    if (!string.IsNullOrEmpty(column.DataType) && string.IsNullOrEmpty(column.ExtraPropertyInfo?.Expression))
                    {
                        DataTypeSpecification dataTypeSpec = DataTypeManager.GetDataTypeSpecification(this.displayInfo.DatabaseType, column.DataType);

                        if (dataTypeSpec != null && !dataTypeSpec.IndexForbidden)
                        {
                            columnInfos.Add(new IndexColumn() { ColumnName = column.Name });
                        }
                    }
                }
            }

            columnSelect.InitControls(columnInfos, this.displayInfo.DatabaseType == DatabaseType.SqlServer);
            columnSelect.LoadColumns(values);

            if (columnSelect.ShowDialog() == DialogResult.OK)
            {
                if (databaseObjectType == DatabaseObjectType.TableIndex)
                {
                    this.ucIndexes.SetRowColumns(columnSelect.SelectedColumns);
                }
            }
        }

        private async void ShowColumnMappingSelector(string referenceTableName, List<ForeignKeyColumn> mappings)
        {
            frmColumnMapping form = new frmColumnMapping() { ReferenceTableName = referenceTableName, TableName = this.txtTableName.Text.Trim(), Mappings = mappings };

            IEnumerable<TableColumnDesingerInfo> columns = this.ucColumns.GetColumns().Where(item => !string.IsNullOrEmpty(item.Name));

            form.TableColumns = columns.OrderBy(item => item.Name).Select(item => item.Name).ToList();

            DbInterpreter dbInterpreter = this.GetDbInterpreter();
            dbInterpreter.Option.ObjectFetchMode = DatabaseObjectFetchMode.Simple;

            SchemaInfoFilter filter = new SchemaInfoFilter() { TableNames = new string[] { referenceTableName } };
            List<TableColumn> referenceTableColumns = await dbInterpreter.GetTableColumnsAsync(filter);

            if (referenceTableName == this.selfTableName)
            {
                form.ReferenceTableColumns = this.ucColumns.GetColumns().Select(item => item.Name).ToList();
            }
            else
            {
                form.ReferenceTableColumns = referenceTableColumns.Select(item => item.Name).ToList();
            }

            if (form.ShowDialog() == DialogResult.OK)
            {
                this.ucForeignKeys.SetRowColumns(form.Mappings);
            }
        }

        #region IObserver<FeedbackInfo>

        public void OnNext(FeedbackInfo value)
        {
            this.Feedback(value);
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }

        #endregion IObserver<FeedbackInfo>

        private void Feedback(string message)
        {
            this.Feedback(new FeedbackInfo() { InfoType = FeedbackInfoType.Info, Message = message, Owner = this });
        }

        private void FeedbackError(string message)
        {
            this.Feedback(new FeedbackInfo() { InfoType = FeedbackInfoType.Error, Message = message, Owner = this });
        }

        private void Feedback(FeedbackInfo info)
        {
            if (this.OnFeedback != null)
            {
                this.OnFeedback(info);
            }
        }
    }
}
