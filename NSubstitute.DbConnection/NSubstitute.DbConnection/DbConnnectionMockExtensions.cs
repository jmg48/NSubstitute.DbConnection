namespace NSubstitute.DbConnection
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Threading;
    using NSubstitute.Core;
    using NSubstitute.ExceptionExtensions;

    public static class DbConnnectionMockExtensions
    {
        /// <summary>
        /// Initialises mock behaviour for IDbConnection.CreateCommand()
        /// </summary>
        /// <param name="mockConnection">An NSubstitute mock connection object</param>
        /// <returns>A mock connection object that can be used to set up mock queries</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has already been called on this connection</exception>
        public static IDbConnection SetupCommands(this IDbConnection mockConnection)
        {
            if (mockConnection is DbConnectionWrapper)
            {
                throw new NotSupportedException($"{nameof(SetupCommands)} has already been called on this connection");
            }

            var result = new DbConnectionWrapper(mockConnection);
            mockConnection.State.Returns(ConnectionState.Open);
            mockConnection.CreateCommand()
                .Returns(
                    _ =>
                    {
                        var mockCommand = Substitute.For<DbCommand>();
                        var mockParameters = Substitute.For<DbParameterCollection>();
                        var parameters = new List<object>();

                        mockParameters.Add(Arg.Any<object>()).Returns(
                            ci =>
                            {
                                parameters.Add(ci[0]);
                                return parameters.Count - 1;
                            });
                        mockParameters.Count.Returns(parameters.Count);
                        mockParameters.When(x => x.AddRange(Arg.Any<Array>())).Throw<NotSupportedException>();
                        mockParameters.When(x => x.Clear()).Do(ci => parameters.Clear());
                        mockParameters.Contains(Arg.Any<object>()).Throws<NotSupportedException>();
                        mockParameters.Contains(Arg.Any<string>()).Throws<NotSupportedException>();
                        mockParameters[Arg.Any<int>()].Throws<NotSupportedException>();
                        mockParameters[Arg.Any<string>()].Throws<NotSupportedException>();

                        mockCommand.Parameters.Returns(ci => mockParameters);
                        mockCommand.CreateParameter().Returns(ci => Substitute.For<DbParameter>());

                        DbDataReader ExecuteReader(CallInfo ci) => result.Configure(mockCommand);
                        mockCommand.ExecuteReader().Returns(ExecuteReader);
                        mockCommand.ExecuteReader(Arg.Any<CommandBehavior>()).Returns(ExecuteReader);
                        mockCommand.ExecuteReaderAsync().Returns(ExecuteReader);
                        mockCommand.ExecuteReaderAsync(Arg.Any<CancellationToken>()).Returns(ExecuteReader);
                        mockCommand.ExecuteReaderAsync(Arg.Any<CommandBehavior>()).Returns(ExecuteReader);
                        mockCommand.ExecuteReaderAsync(Arg.Any<CommandBehavior>(), Arg.Any<CancellationToken>()).Returns(ExecuteReader);
                        return mockCommand;
                    });

            return result;
        }

        /// <summary>
        /// Sets up a mock query which will return the given results for the specified command text
        /// </summary>
        /// <typeparam name="T">The record type</typeparam>
        /// <param name="mockConnection">The connection to add the query to</param>
        /// <param name="commandText">The command text to match on</param>
        /// <param name="results">The result set to return</param>
        /// <returns>The connection</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has not been called on the connection</exception>
        public static IDbConnection SetupQuery<T>(this IDbConnection mockConnection, string commandText, IReadOnlyList<T> results)
        {
            if (!(mockConnection is DbConnectionWrapper connectionWrapper))
            {
                throw new NotSupportedException($"{nameof(SetupCommands)} on this connection before setting up queries");
            }

            connectionWrapper.AddQuery(commandText, results);

            return mockConnection;
        }

        /// <summary>
        /// Sets up a mock query which will return the given results for the specified command text
        /// </summary>
        /// <typeparam name="T1">The record type of the first result set</typeparam>
        /// <typeparam name="T2">The record type of the second result set</typeparam>
        /// <param name="mockConnection">The connection to add the query to</param>
        /// <param name="commandText">The command text to match on</param>
        /// <param name="results1">The first result set to return</param>
        /// <param name="results2">The second result set to return</param>
        /// <returns>The connection</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has not been called on the connection</exception>
        public static IDbConnection SetupQueryMultiple<T1, T2>(
            this IDbConnection mockConnection,
            string commandText,
            IReadOnlyList<T1> results1,
            IReadOnlyList<T2> results2)
        {
            if (!(mockConnection is DbConnectionWrapper connectionWrapper))
            {
                throw new NotSupportedException($"{nameof(SetupCommands)} on this connection before setting up queries");
            }

            connectionWrapper.AddQueryMultiple(commandText, results1, results2);

            return mockConnection;
        }

        private class DbConnectionWrapper : IDbConnection
        {
            private readonly Dictionary<string, Action<DbDataReader>> _queries = new Dictionary<string, Action<DbDataReader>>();

            public DbConnectionWrapper(IDbConnection inner) => Inner = inner;

            public string ConnectionString
            {
                get => Inner.ConnectionString;
                set => Inner.ConnectionString = value;
            }

            public IDbConnection Inner { get; }

            public int ConnectionTimeout => Inner.ConnectionTimeout;

            public string Database => Inner.Database;

            public ConnectionState State => Inner.State;

            public void Dispose() => Inner.Dispose();

            public IDbTransaction BeginTransaction() => Inner.BeginTransaction();

            public IDbTransaction BeginTransaction(IsolationLevel il) => Inner.BeginTransaction();

            public void Close() => Inner.Close();

            public void ChangeDatabase(string databaseName) => Inner.ChangeDatabase(databaseName);

            public IDbCommand CreateCommand() => Inner.CreateCommand();

            public void Open() => Inner.Open();

            public DbDataReader Configure(IDbCommand mockCommand)
            {
                var mockReader = Substitute.For<DbDataReader>();

                if (_queries.TryGetValue(mockCommand.CommandText, out var query))
                {
                    query(mockReader);
                }

                return mockReader;
            }

            public void AddQuery<T>(string commandText, IReadOnlyList<T> results) =>
                _queries.Add(
                    commandText,
                    mockReader =>
                    {
                        var properties = typeof(T).GetProperties();

                        bool NextResult(CallInfo ci) => false;
                        mockReader.NextResult().Returns(NextResult);

                        mockReader.FieldCount.Returns(properties.Length);
                        mockReader.GetName(Arg.Any<int>()).Returns(ci => properties[(int)ci[0]].Name);
                        mockReader.GetFieldType(Arg.Any<int>()).Returns(ci => properties[(int)ci[0]].PropertyType);

                        var rowIndex = -1;
                        SetupRead(mockReader, ci => ++rowIndex < results.Count);

                        mockReader[Arg.Any<int>()].Returns(ci => properties[(int)ci[0]].GetValue(results[rowIndex]));
                    });

            public void AddQueryMultiple<T1, T2>(string commandText, IReadOnlyList<T1> results1, IReadOnlyList<T2> results2) =>
                _queries.Add(
                    commandText,
                    mockReader =>
                    {
                        var properties = new[] { typeof(T1).GetProperties(), typeof(T2).GetProperties() };
                        var rowCounts = new[] { results1.Count, results2.Count };
                        var results = new Func<int, object>[] { i => results1[i], i => results2[i] };

                        var resultSetIndex = 0;
                        var rowIndex = -1;

                        SetupNextResult(
                            mockReader,
                            ci =>
                            {
                                rowIndex = -1;
                                return ++resultSetIndex < 2;
                            });

                        mockReader.FieldCount.Returns(ci => properties[resultSetIndex].Length);
                        mockReader.GetName(Arg.Any<int>()).Returns(ci => properties[resultSetIndex][(int)ci[0]].Name);
                        mockReader.GetFieldType(Arg.Any<int>()).Returns(ci => properties[resultSetIndex][(int)ci[0]].PropertyType);

                        SetupRead(mockReader, ci => ++rowIndex < rowCounts[resultSetIndex]);

                        mockReader[Arg.Any<int>()].Returns(ci => properties[resultSetIndex][(int)ci[0]].GetValue(results[resultSetIndex](rowIndex)));
                    });

            private static void SetupNextResult(DbDataReader reader, Func<CallInfo, bool> nextResult)
            {
                reader.NextResult().Returns(nextResult);
                reader.NextResultAsync().Returns(nextResult);
                reader.NextResultAsync(Arg.Any<CancellationToken>()).Returns(nextResult);
            }

            private static void SetupRead(DbDataReader reader, Func<CallInfo, bool> read)
            {
                reader.Read().Returns(read);
                reader.ReadAsync().Returns(read);
                reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(read);
            }
        }
    }
}