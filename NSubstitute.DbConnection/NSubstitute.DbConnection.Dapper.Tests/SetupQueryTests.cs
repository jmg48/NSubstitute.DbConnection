using System.Data;
using System.Linq;
using Dapper;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Dapper.Tests
{
    [TestFixture]
    public class SetupQueryTests
    {
        [Test]
        public void ShouldMockQueryUsingConcreteType()
        {
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            mockConnection.SetupQuery(
                "select * from table",
                new[]
                {
                    new KeyValue { Key = 1, Value = "abc" },
                });

            var result = mockConnection.Query<KeyValue>("select * from table").ToList();

            result.Count.Should().Be(1);
            result[0].Key.Should().Be(1);
            result[0].Value.Should().Be("abc");
        }

        [Test]
        public void ShouldMockQueryUsingAnonymousType()
        {
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            mockConnection.SetupQuery(
                "select * from table",
                new[]
                {
                    new { Key = 1, Value = "abc" },
                });

            var result = mockConnection.Query<(int Key, string Value)>("select * from table").ToList();

            result.Count.Should().Be(1);
            result[0].Key.Should().Be(1);
            result[0].Value.Should().Be("abc");
        }

        [Test]
        public void ShouldMockQueryUsingRecordType()
        {
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            mockConnection.SetupQuery(
                "select * from table",
                new[]
                {
                    new KeyValueRecord(1, "abc"),
                });

            var result = mockConnection.Query<KeyValueRecord>("select * from table").ToList();

            result.Count.Should().Be(1);
            result[0].Key.Should().Be(1);
            result[0].Value.Should().Be("abc");
        }

        private class KeyValue
        {
            public int Key { get; set; }

            public string? Value { get; set; }
        }

        private record KeyValueRecord(int Key, string Value);
    }
}