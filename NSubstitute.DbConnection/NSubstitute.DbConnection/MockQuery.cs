namespace NSubstitute.Community.DbConnection
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading;
    using NSubstitute.Core;

    internal class MockQuery : IMockQueryBuilder, IMockQueryResultBuilder
    {
        public Dictionary<string, object> Parameters { get; set; }

        public List<(Type RowType, Func<QueryInfo, IReadOnlyList<object>> Rows)> ResultSelectors { get; } = new List<(Type RowType, Func<QueryInfo, IReadOnlyList<object>> Rows)>();

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

        public IMockQueryBuilder WithParameters(params (string Key, object Value)[] parameters)
        {
            Parameters = parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return this;
        }

        public IMockQueryResultBuilder Returns<T>(IEnumerable<T> results)
        {
            ResultSelectors.Add((typeof(T), _ => results.Cast<object>().ToList()));
            return this;
        }

        public IMockQueryResultBuilder Returns<T>(params T[] results)
        {
            ResultSelectors.Add((typeof(T), _ => results.Cast<object>().ToList()));
            return this;
        }

        public IMockQueryResultBuilder Returns<T>(Func<QueryInfo, IEnumerable<T>> resultSelector)
        {
            ResultSelectors.Add((typeof(T), qi => resultSelector(qi).Cast<object>().ToList()));
            return this;
        }

        public IMockQueryResultBuilder ThenReturns<T>(IEnumerable<T> results) => Returns(results);

        public IMockQueryResultBuilder ThenReturns<T>(params T[] results) => Returns(results);

        public IMockQueryResultBuilder ThenReturns<T>(Func<QueryInfo, IEnumerable<T>> resultSelector) => Returns(resultSelector);

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

        public DbDataReader ExecuteReader(IDbCommand mockCommand)
        {
            var parameters = new Dictionary<string, object>();
            for (var i = 0; i < mockCommand.Parameters.Count; i++)
            {
                var parameter = mockCommand.Parameters[i];
                if (parameter is DbParameter dbParameter)
                {
                    parameters.Add(dbParameter.ParameterName, dbParameter.Value);
                }
            }

            var queryInfo = new QueryInfo
            {
                QueryText = mockCommand.CommandText,
                Parameters = parameters,
            };

            var properties = ResultSelectors.Select(resultSet => resultSet.RowType.GetProperties()).ToList();
            var propertiesByName = ResultSelectors
                .Select(resultSet => resultSet.RowType.GetProperties().ToDictionary(property => property.Name, property => property))
                .ToList();

            var resultSetIndex = 0;
            var rowIndex = -1;
            var mockReader = Substitute.For<DbDataReader>();

            var resultSets = new List<IReadOnlyList<object>> { ResultSelectors[0].Rows(queryInfo) };
            SetupNextResult(
                mockReader,
                ci =>
                {
                    rowIndex = -1;
                    resultSetIndex++;
                    if (resultSetIndex >= ResultSelectors.Count)
                    {
                        return false;
                    }

                    resultSets.Add(ResultSelectors[resultSetIndex].Rows(queryInfo));
                    return true;
                });

            mockReader.FieldCount.Returns(ci => properties[resultSetIndex].Length);
            mockReader.GetName(Arg.Any<int>()).Returns(ci => properties[resultSetIndex][(int)ci[0]].Name);
            mockReader.GetFieldType(Arg.Any<int>()).Returns(ci => properties[resultSetIndex][(int)ci[0]].PropertyType);

            SetupRead(mockReader, ci => ++rowIndex < resultSets[resultSetIndex].Count);

            mockReader[Arg.Any<int>()].Returns(ci => properties[resultSetIndex][(int)ci[0]].GetValue(resultSets[resultSetIndex][rowIndex]));
            mockReader[Arg.Any<string>()].Returns(ci => propertiesByName[resultSetIndex][(string)ci[0]].GetValue(resultSets[resultSetIndex][rowIndex]));

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