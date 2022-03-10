using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Dapper.Tests
{
    [TestFixture]
    public class DapperQueryAsyncTests
    {
        [Test]
        public async Task ShouldMockQueryAsyncUsingConcreteType()
        {
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            mockConnection.SetupQuery("select * from table").Returns(new KeyValue { Key = 1, Value = "abc" });

            var result = (await mockConnection.QueryAsync<KeyValue>("select * from table")).ToList();

            result.Count.Should().Be(1);
            result[0].Key.Should().Be(1);
            result[0].Value.Should().Be("abc");
        }

        [Test]
        public async Task ShouldMockQueryAsyncUsingAnonymousType()
        {
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            mockConnection.SetupQuery("select * from table").Returns(new { Key = 1, Value = "abc" });

            var result = (await mockConnection.QueryAsync<(int Key, string Value)>("select * from table")).ToList();

            result.Count.Should().Be(1);
            result[0].Key.Should().Be(1);
            result[0].Value.Should().Be("abc");
        }

        [Test]
        public async Task ShouldMockQueryAsyncUsingRecordType()
        {
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            mockConnection.SetupQuery("select * from table").Returns(new KeyValueRecord(1, "abc"));

            var result = (await mockConnection.QueryAsync<KeyValueRecord>("select * from table")).ToList();

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