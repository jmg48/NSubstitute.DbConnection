using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Tests;

public class QueryParameterMatchingTests
{
    private static readonly string NoMatchingQueryErrorMessage = "No matching query found - call SetupQuery to add mocked queries";

    [Test]
    public void ShouldMockParameterisedQuery()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where id = @id";

        var record1 = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameters(new Dictionary<string, object> { { "id", 1 } })
            .Returns(record1);

        var record2 = new KeyValueRecord(2, "def");
        mockConnection.SetupQuery(commandText)
            .WithParameters(new Dictionary<string, object> { { "id", 2 } })
            .Returns(record2);

        var reader1 = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("id", 1);
            });
        reader1.AssertSingleRecord(record1);

        var reader2 = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("id", 2);
            });
        reader2.AssertSingleRecord(record2);
    }

    [Test]
    public void ShouldAddSingleParameter()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where id = @id";

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameter( "id", 1)
            .Returns(record);
  
        var reader = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("id", 1);
            });
        reader.AssertSingleRecord(record);
    }
    
    [Test]
    public void ShouldReplaceSingleParameter()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where id = @id";

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameter( "id", 1)
            .WithParameter( "id", 2)
            .Returns(record);
  
        var reader = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("id", 2);
            });
        reader.AssertSingleRecord(record);
    }

    [Test]
    public void ShouldReplaceSingleNullParameterValue()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where value = @id";

        var record = new KeyValueRecord(1, null);
        mockConnection.SetupQuery(commandText)
            .WithParameter("value", null)
            .Returns(record);

        var reader = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("value", DBNull.Value);
            });
        reader.AssertSingleRecord(record);
    }

    [Test]
    public void ShouldAddParametersOneByOne()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where id = @id";

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameter( "id", 1)
            .WithParameter( "x", 2)
            .Returns(record);
  
        var reader = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("id", 1);
                command.AddParameter("x", 2);
            });
        reader.AssertSingleRecord(record);
    }

    [Test]
    public void ShouldAddParameterAsTuple()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where id = @id";

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameters(("id", 1))
            .Returns(record);
  
        var reader = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("id", 1);
            });
        reader.AssertSingleRecord(record);

    }

    [Test]
    public void ShouldAddParametersAsTuples()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where id = @id";

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameters(
                ("id", 1),
                ("x", 2))
            .Returns(record);
  
        var reader = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("id", 1);
                command.AddParameter("x", 2);
            });
        reader.AssertSingleRecord(record);
    }

    [Test]
    public void ShouldFailWhenQueryParametersDoNotMatch()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where id = @id";
        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameters(new Dictionary<string, object> { { "id", 1 } })
            .Returns(record);

        var resultNoParameters = () => mockConnection.ExecuteReader(command => command.CommandText = commandText);
        resultNoParameters.Should().Throw<NotSupportedException>().WithMessage(NoMatchingQueryErrorMessage+$": '{commandText}'");

        var resultWrongParameterName = () => mockConnection.ExecuteReader(
            command =>
        {
            command.CommandText = commandText;
            command.AddParameter("x", 1);
        });
        resultWrongParameterName.Should().Throw<NotSupportedException>().WithMessage(NoMatchingQueryErrorMessage+$": '{commandText}'");

        var resultWrongParameterValue = () => mockConnection.ExecuteReader(
            command =>
        {
            command.CommandText = commandText;
            command.AddParameter("id", 2);
        });
        resultWrongParameterValue.Should().Throw<NotSupportedException>().WithMessage(NoMatchingQueryErrorMessage+$": '{commandText}'");

        var resultExtraParameter = () => mockConnection.ExecuteReader(
            command =>
        {
            command.CommandText = commandText;
            command.AddParameter("id", 1);
            command.AddParameter("x", 2);
        });
        resultExtraParameter.Should().Throw<NotSupportedException>().WithMessage(NoMatchingQueryErrorMessage+$": '{commandText}'");
    }


    [Test]
    public void ShouldIgnoreParametersWhenNotSetUp()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table";

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .Returns(record);

        var reader = mockConnection.ExecuteReader(
            command =>
            {
                command.CommandText = commandText;
                command.AddParameter("id", 1);
            });

        reader.AssertSingleRecord(record);
    }

    [Test]
    public void ShouldFailWhenSetUpWithNoParameters()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table where @id = 1";

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithNoParameters()
            .Returns(record);

        var result = () => mockConnection.ExecuteReader(
            command =>
        {
            command.CommandText = commandText;
            command.AddParameter("id", 1);
        });
        result.Should().Throw<NotSupportedException>().WithMessage(NoMatchingQueryErrorMessage+$": '{commandText}'");
    }

    [Test]
    public void ShouldIncludeOutputParametersWhenMatching()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "spMySproc";
        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameters(new Dictionary<string, object> { { "id", 1 } })
            .WithOutputParameters(("output", 7))
            .Returns(record);

        using var command = mockConnection.CreateCommand();
        command.CommandText = commandText;
        command.AddParameter("id", 1);
        command.AddOutputParameter("output", DbType.Byte);
        mockConnection.Open();

        command.ExecuteNonQuery().Should().Be(1);
        command.Parameters["output"].As<DbParameter>().Value.Should().Be(7);
    }

    [Test]
    public void ShouldIncludeOutputParametersWhenMatchingQuery()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "select * from table";

        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithOutputParameters(("output", "example"))
            .Returns(record);
        
        using var command = mockConnection.CreateCommand();
        command.CommandText = commandText;
        command.AddOutputParameter("output", DbType.String);

        var reader = command.ExecuteReader();

        reader.AssertSingleRecord(record);
        command.Parameters["output"].As<DbParameter>().Value.Should().Be("example");
    }

    [Test]
    public void ShouldFailWithNoMatchingOutputParameter()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "spMySproc";
        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameters(new Dictionary<string, object> { { "id", 1 } })
            .WithOutputParameters(("output", 7))
            .Returns(record);

        using var command = mockConnection.CreateCommand();
        command.CommandText = commandText;
        command.AddParameter("id", 1);
        mockConnection.Open();

        var act = () => command.ExecuteNonQuery();
        act.Should().Throw<NotSupportedException>().WithMessage(NoMatchingQueryErrorMessage + $": '{commandText}'");
    }

    [Test]
    public void ShouldFailWithUnexpectedOutputParameterOnCommand()
    {
        var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
        var commandText = "spMySproc";
        var record = new KeyValueRecord(1, "abc");
        mockConnection.SetupQuery(commandText)
            .WithParameters(new Dictionary<string, object> { { "id", 1 } })
            .Returns(record);

        using var command = mockConnection.CreateCommand();
        command.CommandText = commandText;
        command.AddParameter("id", 1);
        command.AddOutputParameter("output", DbType.Byte);
        mockConnection.Open();

        var act = () => command.ExecuteNonQuery();
        act.Should().Throw<NotSupportedException>().WithMessage(NoMatchingQueryErrorMessage + $": '{commandText}'");
    }
}