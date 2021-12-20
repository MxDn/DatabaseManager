using System.Collections.Generic;

using DatabaseInterpreter.Model;

namespace DatabaseInterpreter.Core
{
    public interface IDbInterpreterFactory
    {
        DbInterpreter GetDbInterpreter(ConnectionInfo connectionInfo, DbInterpreterOption option);
    }

    public class DbInterpreterHelper
    {
        private readonly Dictionary<DatabaseType, IDbInterpreterFactory> registeredDbInterpreter;

        public DbInterpreterHelper(Dictionary<DatabaseType, IDbInterpreterFactory> dbInterpreterFactorys)
        {
            registeredDbInterpreter = dbInterpreterFactorys;
        }

        public DbInterpreterHelper()
        {
        }

        public DbInterpreter GetDbInterpreter(DatabaseType dbType, ConnectionInfo connectionInfo, DbInterpreterOption option)
        {
            return registeredDbInterpreter[dbType].GetDbInterpreter(connectionInfo, option);
        }
    }
}
