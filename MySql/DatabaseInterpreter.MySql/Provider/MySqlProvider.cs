using System.Data.Common;

using MySql.Data.MySqlClient;

namespace DatabaseInterpreter.Core
{
    public class MySqlProvider : IDbProvider
    {
        public string ProviderName => "MySql.Data.MySqlClient";

        public DbProviderFactory GetDbProviderFactory()
        {
            return MySqlClientFactory.Instance;
        }
    }
}
