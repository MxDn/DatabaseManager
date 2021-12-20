using System;

using DatabaseInterpreter.Model;

namespace DatabaseConverter.Core
{
    public class ViewConvertException : ConvertException
    {
        public override string ObjectType => nameof(View);

        public ViewConvertException(Exception ex) : base(ex)
        {
        }
    }
}
