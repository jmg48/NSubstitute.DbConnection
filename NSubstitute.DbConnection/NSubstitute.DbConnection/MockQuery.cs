namespace NSubstitute.DbConnection
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
        public Dictionary<string, QueryParameter> Parameters { get; set; }

        public List<(Type RowType, Func<QueryInfo, IReadOnlyList<object>> Rows)> ResultSelectors { get; } = new List<(Type RowType, Func<QueryInfo, IReadOnlyList<object>> Rows)>();

        public Func<string, bool> CommandTextMatcher { get; set; }

        public Func<QueryInfo, int> RowCountSelector { get; set; }

        public IMockQueryBuilder WithNoParameters() => WithParameters(new Dictionary<string, object>());

        public IMockQueryBuilder WithParameter(string name, object value)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, QueryParameter>();
            }

            Parameters[name] = new QueryParameter(name, value);
            return this;
        }

        public IMockQueryBuilder WithParameters(IReadOnlyDictionary<string, object> parameters)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, QueryParameter>();
            }

            foreach (var kvp in parameters)
            {
                Parameters.Add(kvp.Key, new QueryParameter(kvp.Key, kvp.Value));
            }

            return this;
        }

        public IMockQueryBuilder WithParameters(params (string Key, object Value)[] parameters)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, QueryParameter>();
            }

            foreach (var kvp in parameters)
            {
                Parameters.Add(kvp.Key, new QueryParameter(kvp.Key, kvp.Value));
            }

            return this;
        }

        public IMockQueryBuilder WithOutputParameters(IReadOnlyDictionary<string, object> parameters)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, QueryParameter>();
            }

            foreach (var kvp in parameters)
            {
                Parameters.Add(kvp.Key, new QueryParameter(kvp.Key, kvp.Value, true));
            }

            return this;
        }

        public IMockQueryBuilder WithOutputParameters(params (string Key, object Value)[] parameters)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, QueryParameter>();
            }

            foreach (var kvp in parameters)
            {
                Parameters.Add(kvp.Key, new QueryParameter(kvp.Key, kvp.Value, true));
            }

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

        public void Affects(int rowCount) => RowCountSelector = _ => rowCount;

        public void Affects(Func<QueryInfo, int> rowCountSelector) => RowCountSelector = rowCountSelector;

        public void Throws<T>(T exception)
            where T : Exception => ResultSelectors.Add((typeof(T), _ => throw exception));

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
                    !DbEquals(parameterValue.Value, parameter))
                {
                    return false;
                }
            }

            return true;
        }

        public DbDataReader ExecuteReader(IDbCommand mockCommand)
        {
            var queryInfo = GetQueryInfo(mockCommand);

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
            SetupOutputParams(mockCommand, Parameters);

            return mockReader;
        }

        public int ExecuteNonQuery(IDbCommand mockCommand)
        {
            var queryInfo = GetQueryInfo(mockCommand);
            SetupOutputParams(mockCommand, Parameters);
            return RowCountSelector?.Invoke(queryInfo) ?? ResultSelectors.SelectMany(resultSelector => resultSelector.Rows(queryInfo)).Count();
        }

        private static bool DbEquals(object parameterValue, DbParameter parameter)
        {
            if ((parameterValue == null && parameter.Value is DBNull) || parameter.Direction == ParameterDirection.Output)
            {
                return true;
            }

            return Equals(parameterValue, parameter.Value);
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

        private static QueryInfo GetQueryInfo(IDbCommand mockCommand)
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
            return queryInfo;
        }

        private static void SetupOutputParams(IDbCommand mockCommand, Dictionary<string, QueryParameter> parameters)
        {
            for (var i = 0; i < mockCommand.Parameters.Count; i++)
            {
                var cmdParam = mockCommand.Parameters[i];
                if (cmdParam is DbParameter dbParameter && dbParameter.Direction == ParameterDirection.Output)
                {
                    if (!parameters.TryGetValue(dbParameter.ParameterName, out var value))
                    {
                        throw new NotSupportedException($"Unmatched output parameter: '{mockCommand.CommandText}'");
                    }

                    dbParameter.Value = value.Value;
                }
            }
        }
    }
}