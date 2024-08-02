using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Tests;

using System.Data.Common;

[TestFixture]
public class ScalarTests
{
    [Test]
    public void ShouldReturnSingleValue()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new { Id = 1 });

        using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        mockConnection.Open();

        command.ExecuteScalar().Should().Be(1);
    }

    [Test]
    public async Task ShouldReturnSingleValueAsync()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new { Id = true });

        await using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        await mockConnection.OpenAsync();

        (await command.ExecuteScalarAsync()).Should().Be(true);
    }

    [Test]
    public async Task ShouldReturnSingleValueAsyncWithCancellation()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new { Id = "foo" });

        await using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        await mockConnection.OpenAsync(CancellationToken.None);

        (await command.ExecuteScalarAsync(CancellationToken.None)).Should().Be("foo");
    }

    [Test]
    public void ShouldReturnNullForEmptyResultSet()
    {
        var mockConnection = Substitute.For<DbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns<KeyValueRecord>();

        using var command = mockConnection.CreateCommand();
        command.CommandText = "select * from table";
        mockConnection.Open();

        command.ExecuteScalar().Should().Be(null);
    }
}