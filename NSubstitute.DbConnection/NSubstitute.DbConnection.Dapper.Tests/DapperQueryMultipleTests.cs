using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Dapper.Tests
{
    [TestFixture]
    public class DapperQueryMultipleTests
    {

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        public void ShouldReturnMultipleResultSets(int resultSets)
        {
            var expectedResults = Enumerable.Range(0, resultSets).Select(i => new KeyValueRecord(i, Guid.NewGuid().ToString())).ToList();
            
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            var query = mockConnection.SetupQuery("select * from table").Returns(expectedResults[0]);
            for (var i = 1; i < resultSets; i++)
            {
                query = query.ThenReturns(expectedResults[i]);
            }

            var reader = mockConnection.QueryMultiple("select * from table");

            for (var i = 0; i < resultSets; i++)
            {
                var result = reader.Read<KeyValueRecord>().ToList();

                result.Count.Should().Be(1);
                result[0].Key.Should().Be(i);
                result[0].Value.Should().Be(expectedResults[i].Value);
            }
        }

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        public async Task ShouldReturnMutipleResultSetsAsync(int resultSets)
        {
            var expectedResults = Enumerable.Range(0, resultSets).Select(i => new KeyValueRecord(i, Guid.NewGuid().ToString())).ToList();
            
            var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
            var query = mockConnection.SetupQuery("select * from table").Returns(expectedResults[0]);
            for (var i = 1; i < resultSets; i++)
            {
                query = query.ThenReturns(expectedResults[i]);
            }

            var reader = await mockConnection.QueryMultipleAsync("select * from table");

            for (var i = 0; i < resultSets; i++)
            {
                var result = (await reader.ReadAsync<KeyValueRecord>()).ToList();

                result.Count.Should().Be(1);
                result[0].Key.Should().Be(i);
                result[0].Value.Should().Be(expectedResults[i].Value);
            }
        }

        private record KeyValueRecord(int Key, string Value);
    }
}