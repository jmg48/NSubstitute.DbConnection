using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Tests;

using System.Data.Common;

[TestFixture]
public class NonQueryTests
{
    [Test]
    public void ShouldReturnRowCount()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new KeyValueRecord(1, "abc"));

        using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        mockConnection.Open();

        command.ExecuteNonQuery().Should().Be(1);
    }

    [Test]
    public async Task ShouldReturnRowCountAsync()
    {
        var mockConnection = Substitute.For<System.Data.Common.DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new KeyValueRecord(1, "abc"));

        await using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        await mockConnection.OpenAsync();

        (await command.ExecuteNonQueryAsync()).Should().Be(1);
    }

    [Test]
    public void ShouldReturnRowsAffected()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Affects(3);

        using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        mockConnection.Open();

        command.ExecuteNonQuery().Should().Be(3);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public void RowsAffectedShouldDependOnParameters(int rowCount)
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .WithParameter("rowCount", rowCount)
            .Affects(qi => (int)qi.Parameters["rowCount"]);

        using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        command.AddParameter("rowCount", rowCount);
        mockConnection.Open();

        command.ExecuteNonQuery().Should().Be(rowCount);
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

        var act = () => command.ExecuteNonQuery();
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

        var act = () => command.ExecuteNonQuery();
        act.Should().Throw<Exception>().And.Should().Be(expectedException);
    }
}