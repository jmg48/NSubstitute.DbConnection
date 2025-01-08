namespace NSubstitute.DbConnection
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal class DbConnectionWrapper : DbConnection
    {
        private readonly List<MockQuery> _queries = new List<MockQuery>();
        private readonly IDbConnection _inner;

        public DbConnectionWrapper(IDbConnection inner) => _inner = inner;

        public override string ConnectionString
        {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;
        }

        public override int ConnectionTimeout => _inner.ConnectionTimeout;

        public override string Database => _inner.Database;

        public override string DataSource => ((DbConnection)_inner).DataSource;

        public override string ServerVersion => ((DbConnection)_inner).ServerVersion;

        public override ConnectionState State => _inner.State;

        public override void Close() => _inner.Close();

        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

        public override void Open() => _inner.Open();

        public override async Task OpenAsync(CancellationToken cancellationToken) => _inner.Open();

        public DbDataReader ExecuteReader(IDbCommand mockCommand) => GetMatchingQuery(mockCommand).ExecuteReader(mockCommand);

        public int ExecuteNonQuery(IDbCommand mockCommand) => GetMatchingQuery(mockCommand).ExecuteNonQuery(mockCommand);

        public object ExecuteScalar(IDbCommand mockCommand) => GetMatchingQuery(mockCommand).ExecuteScalar(mockCommand);

        public IMockQueryBuilder AddQuery(Func<string, bool> matcher)
        {
            var query = new MockQuery { CommandTextMatcher = matcher };
            _queries.Add(query);
            return query;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => (DbTransaction)_inner.BeginTransaction();

        protected override DbCommand CreateDbCommand()
        {
            var cmd = (DbCommand)_inner.CreateCommand();
            cmd.Connection = this;
            return cmd;
        }

        private MockQuery GetMatchingQuery(IDbCommand mockCommand) =>
            _queries.FirstOrDefault(query => query.Matches(mockCommand)) ??
            throw new NotSupportedException($"No matching query found - call SetupQuery to add mocked queries: '{mockCommand.CommandText}'");
    }
}