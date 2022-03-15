using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute.Community.DbConnection;
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
}