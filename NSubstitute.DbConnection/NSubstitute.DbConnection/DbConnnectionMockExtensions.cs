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
        public static DbConnection SetupCommands(this DbConnection mockConnection)
        {
            if (mockConnection is DbConnectionWrapper)
            {
                throw new NotSupportedException($"{nameof(SetupCommands)} has already been called on this connection");
            }

            var result = new DbConnectionWrapper(mockConnection);
            mockConnection.State.Returns(ConnectionState.Open);
            mockConnection.CreateCommand().Returns(_ => CreateMockCommand(result));
            return result;
        }

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
            mockConnection.CreateCommand().Returns(_ => CreateMockCommand(result));
            return result;
        }

        /// <summary>
        /// Returns a mock query builder which will match the specified command text
        /// </summary>
        /// <param name="mockConnection">The connection to add the query to</param>
        /// <param name="commandText">The command text to match on</param>
        /// <returns>The query builder</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has not been called on the connection</exception>
        public static IMockQueryBuilder SetupQuery(this IDbConnection mockConnection, string commandText) =>
            SetupQuery(
                mockConnection,
                queryString => string.Equals(
                    queryString.Trim(),
                    commandText.Trim(),
                    StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        /// Returns a mock query builder which will match the specified regex
        /// </summary>
        /// <param name="mockConnection">The connection to add the query to</param>
        /// <param name="queryRegex">The regex to match on</param>
        /// <returns>The query builder</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has not been called on the connection</exception>
        public static IMockQueryBuilder SetupQuery(this IDbConnection mockConnection, Regex queryRegex) =>
            SetupQuery(mockConnection, queryRegex.IsMatch);

        /// <summary>
        /// Returns a mock query builder which will match the specified delegate
        /// </summary>
        /// <param name="mockConnection">The connection to add the query to</param>
        /// <param name="queryMatcher">The delegate to match on</param>
        /// <returns>The query builder</returns>
        /// <exception cref="NotSupportedException">If SetupCommands() has not been called on the connection</exception>
        public static IMockQueryBuilder SetupQuery(this IDbConnection mockConnection, Func<string, bool> queryMatcher) =>
            CheckConnectionSetup(mockConnection).AddQuery(queryMatcher);

        private static DbConnectionWrapper CheckConnectionSetup(IDbConnection mockConnection) =>
            mockConnection is DbConnectionWrapper connectionWrapper
                ? connectionWrapper
                : throw new NotSupportedException($"{nameof(SetupCommands)} on this connection before setting up queries");

        private static IDbCommand CreateMockCommand(DbConnectionWrapper result)
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

            int ExecuteNonQuery(CallInfo ci) => result.ExecuteNonQuery(mockCommand);

            mockCommand.ExecuteNonQuery().Returns(ExecuteNonQuery);
            mockCommand.ExecuteNonQueryAsync().Returns(ExecuteNonQuery);
            mockCommand.ExecuteNonQueryAsync(Arg.Any<CancellationToken>()).Returns(ExecuteNonQuery);

            return mockCommand;
        }
    }
}