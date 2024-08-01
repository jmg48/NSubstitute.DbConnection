namespace NSubstitute.DbConnection
{
    internal class QueryParameter
    {
        public QueryParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public QueryParameter(string name, object value, bool isOutput)
        {
            Name = name;
            Value = value;
            IsOutput = isOutput;
        }

        public string Name { get; set; }

        public object Value { get; set; }

        public bool IsOutput { get; set; }
    }
}
