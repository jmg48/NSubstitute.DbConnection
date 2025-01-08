using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Tests;

using System.Data;
using System.Data.Common;

public class QueryTests
{
    private const string SimpleSelect = "select * from table";

    private static DbCommand GetSimpleCommandSingleRow(Action<DbConnection> open)
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery(SimpleSelect)
            .Returns(new KeyValueRecord(1, "abc"));

        var command = mockConnection.CreateCommand();
        command.CommandText = SimpleSelect;
        open(mockConnection);

        return command;
    }

    [Test]
    public void ShouldMockQuery()
    {
        using var reader = GetSimpleCommandSingleRow(c => c.Open()).ExecuteReader();

        reader.Read().Should().BeTrue();

        reader.FieldCount.Should().Be(2);
        reader.GetName(0).Should().Be("Key");
        reader.GetName(1).Should().Be("Value");
        reader.GetFieldType(0).Should().Be(typeof(int));
        reader.GetFieldType(1).Should().Be(typeof(string));

        reader[0].Should().Be(1);
        reader[1].Should().Be("abc");
        reader["Key"].Should().Be(1);
        reader["Value"].Should().Be("abc");

        reader.Read().Should().BeFalse();
        reader.NextResult().Should().BeFalse();
    }

    [Test]
    public async Task ShouldMockQueryAsync()
    {
        await using var reader = await GetSimpleCommandSingleRow(c => c.OpenAsync()).ExecuteReaderAsync();

        (await reader.ReadAsync()).Should().BeTrue();

        reader.FieldCount.Should().Be(2);
        reader.GetName(0).Should().Be("Key");
        reader.GetName(1).Should().Be("Value");
        reader.GetFieldType(0).Should().Be(typeof(int));
        reader.GetFieldType(1).Should().Be(typeof(string));

        reader[0].Should().Be(1);
        reader[1].Should().Be("abc");
        reader["Key"].Should().Be(1);
        reader["Value"].Should().Be("abc");

        (await reader.ReadAsync()).Should().BeFalse();
        (await reader.NextResultAsync()).Should().BeFalse();
    }

    [Test]
    public async Task ShouldMockQueryAsyncWithCancellation()
    {
        await using var reader = await GetSimpleCommandSingleRow(c => c.OpenAsync(CancellationToken.None)).ExecuteReaderAsync(CancellationToken.None);

        (await reader.ReadAsync(CancellationToken.None)).Should().BeTrue();

        reader.FieldCount.Should().Be(2);
        reader.GetName(0).Should().Be("Key");
        reader.GetName(1).Should().Be("Value");
        reader.GetFieldType(0).Should().Be(typeof(int));
        reader.GetFieldType(1).Should().Be(typeof(string));

        reader[0].Should().Be(1);
        reader[1].Should().Be("abc");
        reader["Key"].Should().Be(1);
        reader["Value"].Should().Be("abc");

        (await reader.ReadAsync(CancellationToken.None)).Should().BeFalse();
        (await reader.NextResultAsync(CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    public void ShouldReturnMultipleRows()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new { Key = 1, Value = "abc" }, new { Key = 2, Value = "def" });

        using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        mockConnection.Open();
        using var reader = command.ExecuteReader();

        reader.Read().Should().BeTrue();
        reader["Key"].Should().Be(1);
        reader["Value"].Should().Be("abc");

        reader.Read().Should().BeTrue();
        reader["Key"].Should().Be(2);
        reader["Value"].Should().Be("def");

        reader.Read().Should().BeFalse();
        reader.NextResult().Should().BeFalse();
    }

    [TestCase(1, 0)]
    [TestCase(1, 2)]
    [TestCase(2, 1)]
    [TestCase(1, 8)]
    [TestCase(2, 4)]
    [TestCase(4, 2)]
    public void ResultsShouldDependOnParameters(int resultSetCount, int rowCount)
    {
        var uid = Guid.NewGuid().ToString("D");

        var callCount = 0;

        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        var mockQuery = mockConnection.SetupQuery("select * from table")
            .WithParameter("id", uid)
            .Returns(
                qi =>
                {
                    callCount++;
                    var id = (string)qi.Parameters["id"];
                    return Enumerable.Range(0, rowCount).Select(i => new { ResultSet = 0, Row = i, Id = id });
                });

        for (var resultSet = 1; resultSet < resultSetCount; resultSet++)
        {
            var resultSetCaptured = resultSet;
            mockQuery.ThenReturns(
                qi =>
                {
                    callCount++;
                    var id = (string)qi.Parameters["id"];
                    return Enumerable.Range(0, rowCount).Select(i => new { ResultSet = resultSetCaptured, Row = i, Id = id });
                });
        }

        using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        command.AddParameter("id", uid);
        mockConnection.Open();
        using var reader = command.ExecuteReader();

        for (var resultSetIndex = 0; resultSetIndex < resultSetCount; resultSetIndex++)
        {
            if (resultSetIndex > 0)
            {
                reader.NextResult().Should().BeTrue();
            }

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                reader.Read().Should().BeTrue();
                reader["ResultSet"].Should().Be(resultSetIndex);
                reader["Row"].Should().Be(rowIndex);
                reader["Id"].Should().Be(uid);
            }

            reader.Read().Should().BeFalse();
        }

        reader.Read().Should().BeFalse();
        reader.NextResult().Should().BeFalse();

        callCount.Should().Be(resultSetCount);
    }

    [Test]
    public void ShouldThrow()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        var expectedException = new Exception();
        mockConnection.SetupQuery("delete from table")
            .Throws(expectedException);

        using var command = mockConnection.CreateCommand();
        command.CommandText = "delete from table";
        mockConnection.Open();

        var act = () => command.ExecuteReader();
        act.Should().Throw<Exception>().And.Should().Be(expectedException);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public void ShouldThrowDependOnParameters(int id)
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        var expectedException = new Exception($"failed to delete record with {id}");
        mockConnection.SetupQuery("delete from table where id=@id")
            .WithParameter("id", id)
            .Throws(expectedException);

        using var command = mockConnection.CreateCommand();
        command.CommandText = "delete from table where id=@id";
        command.AddParameter("id", id);
        mockConnection.Open();

        var act = () => command.ExecuteReader();
        act.Should().Throw<Exception>().And.Should().Be(expectedException);
    }

    [Test]
    public void ShouldReaderReturnValuesArray()
    {
        using var reader = GetSimpleCommandSingleRow(c => c.Open()).ExecuteReader();

        reader.Read().Should().BeTrue();

        var values = new object[2];
        reader.GetValues(values).Should().Be(2);
        values[0].Should().Be(1);
        values[1].Should().Be("abc");
    }

    [Test]
    public void ShouldCommandPopulateConnection()
    {
        var command = GetSimpleCommandSingleRow(c => c.Open());
        command.Connection.Should().NotBeNull();
    }
}