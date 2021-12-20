namespace DatabaseManager.Model
{
    public class SqlWordToken
    {
        public SqlWordTokenType Type { get; set; }
        public int StartIndex { get; set; }
        public int StopIndex { get; set; }
        public string Text { get; set; }
    }
}
