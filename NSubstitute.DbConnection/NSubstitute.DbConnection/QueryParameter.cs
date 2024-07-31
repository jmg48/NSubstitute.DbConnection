namespace NSubstitute.DbConnection
{
    internal class QueryParameter
    {
        public QueryParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public QueryParameter(string name, object value, bool output)
        {
            Name = name;
            Value = value;
            Output = output;
        }

        public string Name { get; set; }

        public object Value { get; set; }

        public bool Output { get; set; }
    }
}
