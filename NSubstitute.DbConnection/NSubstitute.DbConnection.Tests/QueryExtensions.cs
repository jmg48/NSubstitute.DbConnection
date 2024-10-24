using System;
using System.Data;
using FluentAssertions;

namespace NSubstitute.DbConnection.Tests;

public static class QueryExtensions
{
    public static IDataReader ExecuteReader(this IDbConnection mockConnection, Action<IDbCommand> configureCommand)
    {
        var command = mockConnection.CreateCommand();
        configureCommand(command);
        mockConnection.Open();
        return command.ExecuteReader();
    }

    public static void AssertSingleRecord(this IDataReader reader, KeyValueRecord expected)
    {
        reader.Read().Should().BeTrue();

        reader.FieldCount.Should().Be(2);
        reader.GetName(0).Should().Be("Key");
        reader.GetName(1).Should().Be("Value");
        reader.GetFieldType(0).Should().Be(typeof(int));
        reader.GetFieldType(1).Should().Be(typeof(string));

        reader[0].Should().Be(expected.Key);
        reader[1].Should().Be(expected.Value);
        reader["Key"].Should().Be(expected.Key);
        reader["Value"].Should().Be(expected.Value);

        reader.GetValue(0).Should().Be(expected.Key);
        reader.GetValue(1).Should().Be(expected.Value);

        reader.Read().Should().BeFalse();
        reader.NextResult().Should().BeFalse();
    }

    public static void AddParameter<T>(this IDbCommand command, string name, T value) =>
        AddParameter(command, name, value, ParameterDirection.Input);

    public static void AddOutputParameter(this IDbCommand command, string name) =>
        AddParameter(command, name, string.Empty, ParameterDirection.Output);

    public static void AddReturnParameter(this IDbCommand command, string name) =>
        AddParameter(command, name, string.Empty, ParameterDirection.ReturnValue);

    public static void AddInputOutputParameter<T>(this IDbCommand command, string name, T input) =>
        AddParameter(command, name, input, ParameterDirection.InputOutput);

    public static void AddParameter<T>(this IDbCommand command, string name, T value, ParameterDirection direction)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        parameter.Direction = direction;
        command.Parameters.Add(parameter);
    }
}