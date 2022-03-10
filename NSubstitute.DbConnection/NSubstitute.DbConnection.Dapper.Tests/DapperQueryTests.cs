using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Dapper.Tests
{
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
                .Returns(
                    new[]
                    {
                        new KeyValueRecord(1, "abc"),
                    });

            var result = mockConnection.Query<KeyValueRecord>("select * from table").ToList();

            result.Count.Should().Be(1);
            result[0].Key.Should().Be(1);
            result[0].Value.Should().Be("abc");
        }

        [Test]
        public void ShouldMockParameterisedQuery()
        {
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            mockConnection.SetupQuery("select * from table where id = @id")
                .WithParameters(
                    new Dictionary<string, object>
                    {
                        { "id", 1 },
                    })
                .Returns(new KeyValueRecord(1, "abc"));

            mockConnection.SetupQuery("select * from table where id = @id")
                .WithParameters(
                    new Dictionary<string, object>
                    {
                        { "id", 2 },
                    })
                .Returns(new KeyValueRecord(2, "def"));

            var result1 = mockConnection.Query<KeyValueRecord>(
                    "select * from table where id = @id",
                    new { id = 1 },
                    commandType: CommandType.StoredProcedure)
                .ToList();

            result1.Count.Should().Be(1);
            result1[0].Key.Should().Be(1);
            result1[0].Value.Should().Be("abc");

            var result2 = mockConnection.Query<KeyValueRecord>(
                    "select * from table where id = @id",
                    new { id = 2 },
                    commandType: CommandType.StoredProcedure)
                .ToList();

            result2.Count.Should().Be(1);
            result2[0].Key.Should().Be(2);
            result2[0].Value.Should().Be("def");
        }

        [Test]
        public void ShouldFailWhenNoMatchingQuery()
        {
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            mockConnection.SetupQuery("select * from table where id = @id")
                .WithParameters(
                    new Dictionary<string, object>
                    {
                        { "id", 1 },
                    })
                .Returns(new KeyValueRecord(1, "abc"));

            var errorMessage = "No matching query found - call SetupQuery to add mocked queries";

            var resultWrongSql = () => mockConnection.Query<KeyValueRecord>("select * from table");
            resultWrongSql.Should().Throw<NotSupportedException>().WithMessage(errorMessage);

            var resultNoParameters = () => mockConnection.Query<KeyValueRecord>("select * from table where id = @id");
            resultNoParameters.Should().Throw<NotSupportedException>().WithMessage(errorMessage);

            var resultWrongParameterName = () => mockConnection.Query<KeyValueRecord>(
                "select * from table where id = @id",
                new Dictionary<string, object> { { "x", 1 } });
            resultWrongParameterName.Should().Throw<NotSupportedException>().WithMessage(errorMessage);

            var resultWrongParameterValue = () => mockConnection.Query<KeyValueRecord>(
                "select * from table where id = @id",
                new Dictionary<string, object> { { "id", 2 } });
            resultWrongParameterValue.Should().Throw<NotSupportedException>().WithMessage(errorMessage);

            var resultExtraParameter = () => mockConnection.Query<KeyValueRecord>(
                "select * from table where id = @id",
                new Dictionary<string, object>
                {
                    { "id", 1 },
                    { "x", 2 },
                });
            resultExtraParameter.Should().Throw<NotSupportedException>().WithMessage(errorMessage);
        }

        private class KeyValue
        {
            public int Key { get; set; }

            public string? Value { get; set; }
        }

        private record KeyValueRecord(int Key, string Value);
    }
}