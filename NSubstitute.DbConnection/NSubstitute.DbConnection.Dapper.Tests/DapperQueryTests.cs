using System;
using System.Data;
using System.Linq;
using Dapper;
using FluentAssertions;
using NSubstitute.DbConnection;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Dapper.Tests;

[TestFixture]
public class DapperQueryTests
{
    [Test]
    public void ShouldMockQueryUsingConcreteType()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table").Returns(new KeyValue { Key = 1, Value = "abc" });

        var result = mockConnection.Query<KeyValue>("select * from table").ToList();

        result.Count.Should().Be(1);
        result[0].Key.Should().Be(1);
        result[0].Value.Should().Be("abc");
    }

    [Test]
    public void ShouldMockQueryUsingAnonymousType()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table").Returns(new { Key = 1, Value = "abc" });

        var result = mockConnection.Query<(int Key, string Value)>("select * from table").ToList();

        result.Count.Should().Be(1);
        result[0].Key.Should().Be(1);
        result[0].Value.Should().Be("abc");
    }

    [Test]
    public void ShouldMockQueryUsingRecordType()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        mockConnection.SetupQuery("select * from table")
            .Returns(new KeyValueRecord(1, "abc"));

        var result = mockConnection.Query<KeyValueRecord>("select * from table").ToList();

        result.Count.Should().Be(1);
        result[0].Key.Should().Be(1);
        result[0].Value.Should().Be("abc");
    }

    [Test]
    public void ShouldMockQueryThrow()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var expectedException = new Exception();
        mockConnection.SetupQuery("delete from table")
            .Throws(expectedException);

        using var command = mockConnection.CreateCommand();
        command.CommandText = "delete from table";
        mockConnection.Open();

        var act = () => mockConnection.Query("delete from table");
        act.Should().Throw<Exception>().And.Should().Be(expectedException);
    }
}