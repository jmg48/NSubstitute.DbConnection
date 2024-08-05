namespace NSubstitute.DbConnection
{
    using System.Data;

    internal class QueryParameter
    {
        public QueryParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public QueryParameter(string name, object value, ParameterDirection direction)
        {
            Name = name;
            Value = value;
            Direction = direction;
        }

        public string Name { get; set; }

        public object Value { get; set; }

        public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    }
}
