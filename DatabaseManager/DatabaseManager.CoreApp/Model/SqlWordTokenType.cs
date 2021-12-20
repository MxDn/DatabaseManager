using System;

namespace DatabaseManager.Model
{
    [Flags]
    public enum SqlWordTokenType : int
    {
        None = 0,
        Keyword = 2,
        BuiltinFunction = 4,
        Owner = 8,
        Function = 16,
        Table = 32,
        View = 64,
        TableColumn = 128,
        String = 256,
        Comment = 512
    }
}
