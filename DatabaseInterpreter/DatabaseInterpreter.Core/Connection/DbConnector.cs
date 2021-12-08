using DatabaseInterpreter.Model;

using System.Collections.Generic;
using System.Data.Common;

namespace DatabaseInterpreter.Core
{
    public class DbConnector
    {
        private readonly IDbProvider _dbProvider;
        private readonly string _connectionString;
        private readonly IDictionary<string, DbProviderFactory> registeredDbProviderFactory;
        public DbConnector(IDbProvider dbProvider, string connectionString)
        {
            this._dbProvider = dbProvider;
            this._connectionString = connectionString;
        }

        public DbConnector(IDbProvider dbProvider, IConnectionBuilder connectionBuilder, ConnectionInfo connectionInfo)
        {
            this._dbProvider = dbProvider;
            this._connectionString = connectionBuilder.BuildConntionString(connectionInfo);
        }

        public DbConnection CreateConnection()
        {
            DbProviderFactory factory = null;

            string lowerProviderName = this._dbProvider.ProviderName.ToLower(); 
             factory = this._dbProvider.GetDbProviderFactory();
            DbConnection connection = factory.CreateConnection();
            if (connection != null)
            {
                connection.ConnectionString = this._connectionString;
                return connection;
            }
            else
            {
                return null;
            }
        }
    }
}
