namespace NSubstitute.Community.DbConnection
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading;
    using NSubstitute.Core;
    using NSubstitute.DbConnection;

    internal class MockQuery : IMockQueryBuilder, IMockQueryResultBuilder
    {
        public Dictionary<string, object> Parameters { get; set; }

        public List<(Type RowType, IReadOnlyList<object> Rows)> ResultSets { get; } = new List<(Type RowType, IReadOnlyList<object> Rows)>();

        public Func<string, bool> CommandTextMatcher { get; set; }

        public IMockQueryBuilder WithNoParameters() => WithParameters(new Dictionary<string, object>());

        public IMockQueryBuilder WithParameter(string name, object value)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, object>();
            }

            Parameters[name] = value;
            return this;
        }

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

        public IMockQueryResultBuilder ThenReturns<T>(IReadOnlyList<T> results) => Returns(results);

        public IMockQueryResultBuilder ThenReturns<T>(params T[] results) => Returns(results);

        public bool Matches(IDbCommand command)
        {
            if (!CommandTextMatcher(command.CommandText))
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
            var propertiesByName = ResultSets
                .Select(resultSet => resultSet.RowType.GetProperties().ToDictionary(property => property.Name, property => property))
                .ToList();

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
            mockReader[Arg.Any<string>()].Returns(ci => propertiesByName[resultSetIndex][(string)ci[0]].GetValue(ResultSets[resultSetIndex].Rows[rowIndex]));

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