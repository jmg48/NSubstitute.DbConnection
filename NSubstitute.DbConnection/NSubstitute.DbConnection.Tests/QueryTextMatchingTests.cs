using System;
using System.Data;
using System.Text.RegularExpressions;
using FluentAssertions;
using NSubstitute.Community.DbConnection;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Tests;

public class QueryTextMatchingTests
{
    private static readonly string noMatchingQueryErrorMessage = "No matching query found - call SetupQuery to add mocked queries";

    [Test]
    public void ShouldFailWhenQueryTextDoesNotMatch()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery("select * from table")
            .Returns(record);

        var resultWrongSql = () => mockConnection.ExecuteReader(command => command.CommandText = "select * from anotherTable");
        resultWrongSql.Should().Throw<NotSupportedException>().WithMessage(noMatchingQueryErrorMessage);
    }

    [Test]
    public void ShouldUseSuppliedMatcherOverDefaultQueryMatching()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(command => command.Contains("from table"))
            .Returns(record);

        var reader = mockConnection.ExecuteReader(command => command.CommandText = "select * from table t inner join otherTable ot on ot.id = t.otherId");
        reader.AssertSingleRecord(record);
    }

    [Test]
    public void ShouldUseRegexMatcherOverDefaultQueryMatching()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(new Regex("select .+ from"))
            .Returns(record);

        var reader = mockConnection.ExecuteReader(command => command.CommandText = "select id, foo,bar from table where id = @id");
        reader.AssertSingleRecord(record);
    }
}