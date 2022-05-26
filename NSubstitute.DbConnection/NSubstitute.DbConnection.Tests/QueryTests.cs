using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Tests;

using System.Data.Common;

public class QueryTests
{
    [Test]
    public void ShouldMockQuery()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new KeyValueRecord(1, "abc"));

        using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        mockConnection.Open();
        using var reader = command.ExecuteReader();

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
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new KeyValueRecord(1, "abc"));

        await using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        await mockConnection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

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
}