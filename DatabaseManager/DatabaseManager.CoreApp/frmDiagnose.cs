using System;
using System.Collections.Generic;
using System.Windows.Forms;

using DatabaseInterpreter.Core;
using DatabaseInterpreter.Model;
using DatabaseInterpreter.Utility;

using DatabaseManager.Core;
using DatabaseManager.Model;

namespace DatabaseManager
{
    public partial class frmDiagnose : Form
    {
        public DatabaseType DatabaseType { get; set; }
        public ConnectionInfo ConnectionInfo { get; set; }

        private DbManager dbManager = null;

        public frmDiagnose()
        {
            InitializeComponent();
        }

        private void frmDiagnose_Load(object sender, EventArgs e)
        {
            this.InitControls();
        }

        private void InitControls()
        {
            if (this.DatabaseType == DatabaseType.Oracle)
            {
                this.rbNotNullWithEmpty.Enabled = false;
                this.rbSelfReferenceSame.Checked = true;
            }
        }

        public void Init(IObserver<FeedbackInfo> observer)
        {
            this.dbManager = new DbManager(new Dictionary<DatabaseType, IDbInterpreterFactory>() { { DatabaseType.SqlServer, new SqlServerDbInterpreterFactory() } });

            dbManager.Subscribe(observer);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            this.Diagnose();
        }

        private async void Diagnose()
        {
            DiagnoseType diagnoseType = this.rbNotNullWithEmpty.Checked ? DiagnoseType.NotNullWithEmpty : DiagnoseType.SelfReferenceSame;

            try
            {
                this.btnStart.Enabled = false;

                DiagnoseResult result = await dbManager.Diagnose(this.DatabaseType, this.ConnectionInfo, diagnoseType);

                if (result.Details.Count > 0)
                {
                    frmDiagnoseResult frmResult = new frmDiagnoseResult()
                    {
                        DatabaseType = this.DatabaseType,
                        ConnectionInfo = this.ConnectionInfo
                    };

                    frmResult.LoadResult(result);
                    frmResult.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Diagnosis finished, no invalid data found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ExceptionHelper.GetExceptionDetails(ex), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.btnStart.Enabled = true;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
