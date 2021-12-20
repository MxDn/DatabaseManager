using System;

using DatabaseInterpreter.Model;

namespace DatabaseConverter.Core
{
    public class DataTransferException : ConvertException
    {
        public override string ObjectType => nameof(Table);

        public DataTransferException(Exception ex) : base(ex)
        {
        }
    }
}
