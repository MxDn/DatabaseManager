using System.Data.Common;

namespace  DatabaseInterpreter.Core
{
    public interface IDbProvider
    {
        string ProviderName { get; }

        DbProviderFactory GetDbProviderFactory();
    }
}
