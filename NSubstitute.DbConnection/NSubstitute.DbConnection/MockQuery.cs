﻿namespace NSubstitute.DbConnection
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using NSubstitute.Core;
    using NSubstitute.DbConnection.Extensions;
    using NSubstitute.ExceptionExtensions;

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

        public IMockQueryBuilder WithParameters(IReadOnlyDictionary<string, object> parameters) =>
            SetParameters(parameters.Select(kvp => (kvp.Key, new QueryParameter(kvp.Key, kvp.Value))));

        public IMockQueryBuilder WithParameters(params (string Key, object Value)[] parameters) =>
            SetParameters(parameters.Select(kvp => (kvp.Key, new QueryParameter(kvp.Key, kvp.Value))));

        public IMockQueryBuilder WithOutputParameters(IReadOnlyDictionary<string, object> parameters) =>
            SetParameters(parameters.Select(kvp => (kvp.Key, new QueryParameter(kvp.Key, kvp.Value, ParameterDirection.Output))));

        public IMockQueryBuilder WithOutputParameters(params (string Key, object OutputValue)[] parameters) =>
            SetParameters(parameters.Select(kvp => (kvp.Key, new QueryParameter(kvp.Key, kvp.OutputValue, ParameterDirection.Output))));

        public IMockQueryBuilder WithReturnParameter(string parameterName, object returnValue) =>
            SetParameters(new[] { (parameterName, new QueryParameter(parameterName, returnValue, ParameterDirection.ReturnValue)) });

        public IMockQueryBuilder WithInputOutputParameters(params (string Key, object InputValue, object OutputValue)[] parameters) =>
            SetParameters(parameters.Select(kvp => (kvp.Key, new InOutQueryParameter(kvp.Key, kvp.InputValue, kvp.OutputValue))));

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

            return command.Parameters.Cast<object>().All(parameter =>
                parameter is DbParameter dbParameter &&
                    Parameters.TryGetValue(dbParameter.ParameterName, out var mockParameter) &&
                    DbEquals(mockParameter.Value, dbParameter));
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

            int FieldCount() => properties[resultSetIndex].Length;
            PropertyInfo FieldByIndex(int index) => properties[resultSetIndex][index];
            PropertyInfo FieldByName(string name) => propertiesByName[resultSetIndex][name];
            object GetValue(PropertyInfo field) => field.GetValue(resultSets[resultSetIndex][rowIndex]);

            mockReader.FieldCount.Returns(ci => FieldCount());
            mockReader.GetName(Arg.Any<int>()).Returns(ci => FieldByIndex((int)ci[0]).Name);
            mockReader.GetFieldType(Arg.Any<int>()).Returns(ci => FieldByIndex((int)ci[0]).PropertyType);

            SetupRead(mockReader, ci => ++rowIndex < resultSets[resultSetIndex].Count);

            mockReader[Arg.Any<int>()].Returns(ci => GetValue(FieldByIndex((int)ci[0])));
            mockReader.GetValue(Arg.Any<int>()).Returns(ci => GetValue(FieldByIndex((int)ci[0])));
            mockReader[Arg.Any<string>()].Returns(ci => GetValue(FieldByName((string)ci[0])));
            mockReader.GetValues(Arg.Any<object[]>()).Returns(ci =>
            {
                var values = ci.ArgAt<object[]>(0);
                var valueCount = Math.Min(values.Length, FieldCount());
                for (var fieldIndex = 0; fieldIndex < valueCount; fieldIndex++)
                {
                    values[fieldIndex] = GetValue(FieldByIndex(fieldIndex));
                }

                return valueCount;
            });

            Exception ToDo(CallInfo ci) => new NotImplementedException("Not yet implemented - if you need this method please raise a request on github :)");

            mockReader.GetEnumerator().Throws(ToDo);
            mockReader.GetBoolean(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetByte(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetBytes(Arg.Any<int>(), Arg.Any<long>(), Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<int>()).Throws(ToDo);
            mockReader.GetChar(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetChars(Arg.Any<int>(), Arg.Any<long>(), Arg.Any<char[]>(), Arg.Any<int>(), Arg.Any<int>()).Throws(ToDo);
            mockReader.GetData(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetDataTypeName(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetDateTime(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetDecimal(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetDouble(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetFloat(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetGuid(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetInt16(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetInt32(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetInt64(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetOrdinal(Arg.Any<string>()).Throws(ToDo);
            mockReader.GetString(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetStream(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetTextReader(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetSchemaTable().Throws(ToDo);
            mockReader.GetProviderSpecificFieldType(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetProviderSpecificValue(Arg.Any<int>()).Throws(ToDo);
            mockReader.GetProviderSpecificValues(Arg.Any<object[]>()).Throws(ToDo);

            SetupOutputParams(mockCommand, Parameters);

            return mockReader;
        }

        public int ExecuteNonQuery(IDbCommand mockCommand)
        {
            var queryInfo = GetQueryInfo(mockCommand);
            SetupOutputParams(mockCommand, Parameters);
            return RowCountSelector?.Invoke(queryInfo) ?? ResultSelectors.SelectMany(resultSelector => resultSelector.Rows(queryInfo)).Count();
        }

        public object ExecuteScalar(IDbCommand mockCommand)
        {
            var reader = ExecuteReader(mockCommand);
            return reader.Read() ? reader[0] : null;
        }

        private static bool DbEquals(object parameterValue, DbParameter parameter)
        {
            if ((parameterValue == null && parameter.Value is DBNull) ||
                parameter.Direction == ParameterDirection.Output ||
                parameter.Direction == ParameterDirection.ReturnValue)
            {
                return true;
            }

            if (parameterValue is DataTable mockTvp && parameter.Value is DataTable dataTable)
            {
                return new DataTableComparer().Equals(mockTvp, dataTable);
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
            foreach (var parameter in mockCommand.Parameters)
            {
                if (parameter is DbParameter dbParameter)
                {
                    parameters.Add(dbParameter.ParameterName, dbParameter.Value);
                }
            }

            return new QueryInfo
            {
                QueryText = mockCommand.CommandText,
                Parameters = parameters,
            };
        }

        private static void SetupOutputParams(IDbCommand mockCommand, IReadOnlyDictionary<string, QueryParameter> mockParameters)
        {
            foreach (var cmdParam in mockCommand.Parameters)
            {
                if (cmdParam is DbParameter dbParameter)
                {
                    switch (dbParameter.Direction)
                    {
                        case ParameterDirection.InputOutput:
                            var inOut = mockParameters[dbParameter.ParameterName] as InOutQueryParameter;
                            dbParameter.Value = inOut?.ReturnValue;
                            break;
                        case ParameterDirection.Output:
                        case ParameterDirection.ReturnValue:
                            dbParameter.Value = mockParameters[dbParameter.ParameterName].Value;
                            break;
                    }
                }
            }
        }

        private IMockQueryBuilder SetParameters<T>(IEnumerable<(string Key, T Value)> parameters)
            where T : QueryParameter
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, QueryParameter>();
            }

            foreach (var kvp in parameters)
            {
                Parameters.Add(kvp.Key, kvp.Value);
            }

            return this;
        }
    }
}