using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

namespace  DatabaseInterpreter.Core
{
    public class OracleProvider:IDbProvider
    {
        public string ProviderName => "Oracle.ManagedDataAccess.Client";
        public DbProviderFactory GetDbProviderFactory()
        {
            return OracleClientFactory.Instance;
        }
    }
}
