using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace DatabaseInterpreter.Core
{
    public class SqlServerProvider : IDbProvider
    { public string ProviderName => "System.Data.SqlClient";

        public DbProviderFactory GetDbProviderFactory()
        {
            return this.GetDbProviderFactory(this.ProviderName);
        }

        public DbProviderFactory GetDbProviderFactory(
            string dbProviderFactoryTypename,
            string assemblyName)
        {
            var staticProperty = ReflectionUtils.GetStaticProperty(dbProviderFactoryTypename, "Instance");
            if (staticProperty == null && ReflectionUtils.LoadAssembly(assemblyName) != null)
            {
                staticProperty = ReflectionUtils.GetStaticProperty(dbProviderFactoryTypename, "Instance");
            }

            if (staticProperty == null)
            {
                throw new InvalidOperationException();
            }

            return staticProperty as DbProviderFactory;
        }

        private DbProviderFactory GetDbProviderFactory(
            DataAccessProviderTypes type)
        {
            switch (type)
            {
                case DataAccessProviderTypes.SqlServer:
                    return SqlClientFactory.Instance;
                case DataAccessProviderTypes.SqLite:
                    return this.GetDbProviderFactory("System.Data.SQLite.SQLiteFactory", "System.Data.SQLite");
                case DataAccessProviderTypes.MySql:
                    return this.GetDbProviderFactory("MySql.Data.MySqlClient.MySqlClientFactory", "MySql.Data");
                case DataAccessProviderTypes.PostgreSql:
                    return this.GetDbProviderFactory("Npgsql.NpgsqlFactory", "Npgsql");
                default:
                    throw new NotSupportedException();
            }
        }

        private DbProviderFactory GetDbProviderFactory(string providerName)
        {
            var lower = providerName.ToLower();
            if (lower == "system.data.sqlclient")
            {
                return this.GetDbProviderFactory(DataAccessProviderTypes.SqlServer);
            }

            if (lower == "system.data.sqlite" || lower == "microsoft.data.sqlite")
            {
                return this.GetDbProviderFactory(DataAccessProviderTypes.SqLite);
            }

            if (lower == "mysql.data.mysqlclient" || lower == "mysql.data")
            {
                return this.GetDbProviderFactory(DataAccessProviderTypes.MySql);
            }

            if (lower == "npgsql")
            {
                return this.GetDbProviderFactory(DataAccessProviderTypes.PostgreSql);
            }

            throw new NotSupportedException();
        }
           }
}