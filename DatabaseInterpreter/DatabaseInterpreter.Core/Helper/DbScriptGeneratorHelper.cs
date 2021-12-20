using System.Collections.Generic;

using DatabaseInterpreter.Model;

namespace DatabaseInterpreter.Core
{
    public interface IDbScriptGeneratorFactory
    {
        DbScriptGenerator GetDbScriptGenerator(DbInterpreter dbInterpreter);
    }

    public class DbScriptGeneratorHelper
    {
        private readonly IDictionary<DatabaseType, IDbScriptGeneratorFactory> registeredScriptGeneratorFactories;

        public DbScriptGeneratorHelper(IDictionary<DatabaseType, IDbScriptGeneratorFactory> registeredScriptGeneratorFactories)
        {
            this.registeredScriptGeneratorFactories = registeredScriptGeneratorFactories;
        }

        public DbScriptGenerator GetDbScriptGenerator(DbInterpreter dbInterpreter)
        {
            return registeredScriptGeneratorFactories[dbInterpreter.DatabaseType].GetDbScriptGenerator(dbInterpreter);
        }
    }
}
