namespace DatabaseInterpreter.Model
{
    public enum DatabaseType
    {
        Unknown = 0,
        SqlServer = 1,
        MySql = 2,
        Oracle = 3
    }
    public class DataBaseTypeConfiguration
    {
        public DataBaseTypeConfiguration(string name)
        {
            this.Name = name ;
        }

        public string Name { get; }
    }
    
}
