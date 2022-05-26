namespace NSubstitute.DbConnection
{
    using System.Collections.Generic;

    public class QueryInfo
    {
        public string QueryText { get; set; }

        public IReadOnlyDictionary<string, object> Parameters { get; set; }
    }
}