namespace NSubstitute.DbConnection
{
    using System.Data;

    internal class InOutQueryParameter : QueryParameter
    {
        public InOutQueryParameter(string name, object value, object returnValue)
            : base(name, value, ParameterDirection.InputOutput)
        {
            ReturnValue = returnValue;
        }

        public object ReturnValue { get; set; }
    }
}