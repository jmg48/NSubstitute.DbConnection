namespace NSubstitute.DbConnection
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Text.RegularExpressions;
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
                        var parameters = new List<DbParameter>();

                        mockParameters.Add(Arg.Any<object>())
                            .Returns(
                                ci =>
                                {
                                    parameters.Add((DbParameter)ci[0]);
                                    return parameters.Count - 1;
                                });
                        mockParameters.Count.Returns(ci => parameters.Count);
                        mockParameters.When(x => x.AddRange(Arg.Any<Array>())).Throw<NotSupportedException>();
                        mockParameters.When(x => x.Clear()).Do(ci => parameters.Clear());
                        mockParameters.Contains(Arg.Any<object>()).Throws<NotSupportedException>();
                        mockParameters.Contains(Arg.Any<string>()).Returns(ci => parameters.Any(parameter => parameter.ParameterName == (string)ci[0]));
                        mockParameters[Arg.Any<int>()].Returns(ci => parameters[(int)ci[0]]);
                        mockParameters[Arg.Any<string>()].Throws<NotSupportedException>();

                        mockCommand.Parameters.Returns(ci => mockParameters);
                        mockCommand.CreateParameter().Returns(ci => Substitute.For<DbParameter>());

                        DbDataReader ExecuteReader(CallInfo ci) => result.ExecuteReader(mockCommand);
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
        /// Returns a mock query builder which will match the specified command text
        /// </summary>
        /// <param name="mockConnection">The connection to add the query to</param>
        /// <param name="commandText">The command text to match on</param>
        /// <returns>The query builder</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has not been called on the connection</exception>
        public static IMockQueryBuilder SetupQuery(this IDbConnection mockConnection, string commandText)
        {
            var connectionWrapper = CheckConnectionSetup(mockConnection);

            return connectionWrapper.AddQuery(commandText);
        }

        /// <summary>
        /// Returns a mock query builder which will match the specified regex
        /// </summary>
        /// <param name="mockConnection">The connection to add the query to</param>
        /// <param name="commandRegex">The regex to match on</param>
        /// <returns>The query builder</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has not been called on the connection</exception>
        public static IMockQueryBuilder SetupQuery(this IDbConnection mockConnection, Regex commandRegex)
        {
            var connectionWrapper = CheckConnectionSetup(mockConnection);

            return connectionWrapper.AddQuery(commandRegex.IsMatch);
        }

        /// <summary>
        /// Returns a mock query builder which will match the specified delegate
        /// </summary>
        /// <param name="mockConnection">The connection to add the query to</param>
        /// <param name="queryMatcher">The delegate to match on</param>
        /// <returns>The query builder</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has not been called on the connection</exception>
        public static IMockQueryBuilder SetupQuery(this IDbConnection mockConnection, Func<string, bool> queryMatcher)
        {
            var connectionWrapper = CheckConnectionSetup(mockConnection);

            return connectionWrapper.AddQuery(queryMatcher);
        }

        private static DbConnectionWrapper CheckConnectionSetup(IDbConnection mockConnection)
        {
            if (!(mockConnection is DbConnectionWrapper connectionWrapper))
            {
                throw new NotSupportedException($"{nameof(SetupCommands)} on this connection before setting up queries");
            }

            return connectionWrapper;
        }

        private class DbConnectionWrapper : IDbConnection
        {
            private readonly List<MockQuery> _queries = new List<MockQuery>();

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

            public DbDataReader ExecuteReader(IDbCommand mockCommand)
            {
                foreach (var query in _queries)
                {
                    if (query.Matches(mockCommand))
                    {
                        return query.ExecuteReader();
                    }
                }

                throw new NotSupportedException("No matching query found - call SetupQuery to add mocked queries");
            }

            public IMockQueryBuilder AddQuery(string commandText)
            {
                var query = new MockQuery
                {
                    CommandText = commandText,
                    Matcher = queryString => string.Equals(queryString.Trim(), commandText.Trim(), StringComparison.InvariantCultureIgnoreCase),
                };
                _queries.Add(query);
                return query;
            }

            public IMockQueryBuilder AddQuery(Func<string, bool> matcher)
            {
                var query = new MockQuery { Matcher = matcher };
                _queries.Add(query);
                return query;
            }
        }

        private class MockQuery : IMockQueryBuilder, IMockQueryResultBuilder
        {
            public string CommandText { get; set; }

            public Dictionary<string, object> Parameters { get; set; }

            public List<(Type RowType, IReadOnlyList<object> Rows)> ResultSets { get; } = new List<(Type RowType, IReadOnlyList<object> Rows)>();

            public Func<string, bool> Matcher { get; set; }

            public IMockQueryBuilder WithNoParameters() => WithParameters(new Dictionary<string, object>());

            public IMockQueryBuilder WithParameters(IReadOnlyDictionary<string, object> parameters)
            {
                Parameters = parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return this;
            }

            public IMockQueryResultBuilder Returns<T>(IReadOnlyList<T> results)
            {
                ResultSets.Add((typeof(T), results.Cast<object>().ToList()));
                return this;
            }

            public IMockQueryResultBuilder Returns<T>(params T[] results)
            {
                ResultSets.Add((typeof(T), results.Cast<object>().ToList()));
                return this;
            }

            public IMockQueryResultBuilder ThenReturns<T>(IReadOnlyList<T> results)
            {
                ResultSets.Add((typeof(T), results.Cast<object>().ToList()));
                return this;
            }

            public IMockQueryResultBuilder ThenReturns<T>(params T[] results)
            {
                ResultSets.Add((typeof(T), results.Cast<object>().ToList()));
                return this;
            }

            public bool Matches(IDbCommand command)
            {
                if (!Matcher(command.CommandText))
                {
                    return false;
                }

                if (Parameters == null)
                {
                    return true;
                }

                if (command.Parameters.Count != Parameters.Count)
                {
                    return false;
                }

                for (var i = 0; i < command.Parameters.Count; i++)
                {
                    if (!(command.Parameters[i] is DbParameter parameter) ||
                        !Parameters.TryGetValue(parameter.ParameterName, out var parameterValue) ||
                        !Equals(parameterValue, parameter.Value))
                    {
                        return false;
                    }
                }

                return true;
            }

            public DbDataReader ExecuteReader()
            {
                var properties = ResultSets.Select(resultSet => resultSet.RowType.GetProperties()).ToList();

                var resultSetIndex = 0;
                var rowIndex = -1;
                var mockReader = Substitute.For<DbDataReader>();

                SetupNextResult(
                    mockReader,
                    ci =>
                    {
                        rowIndex = -1;
                        return ++resultSetIndex < ResultSets.Count;
                    });

                mockReader.FieldCount.Returns(ci => properties[resultSetIndex].Length);
                mockReader.GetName(Arg.Any<int>()).Returns(ci => properties[resultSetIndex][(int)ci[0]].Name);
                mockReader.GetFieldType(Arg.Any<int>()).Returns(ci => properties[resultSetIndex][(int)ci[0]].PropertyType);

                SetupRead(mockReader, ci => ++rowIndex < ResultSets[resultSetIndex].Rows.Count);

                mockReader[Arg.Any<int>()].Returns(ci => properties[resultSetIndex][(int)ci[0]].GetValue(ResultSets[resultSetIndex].Rows[rowIndex]));

                return mockReader;
            }

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