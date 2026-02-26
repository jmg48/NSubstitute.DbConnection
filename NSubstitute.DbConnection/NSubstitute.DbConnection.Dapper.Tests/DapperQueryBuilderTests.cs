using System.Data;
using Dapper;
using DapperQueryBuilder;
using InterpolatedSql.Dapper.SqlBuilders;

namespace NSubstitute.DbConnection.Dapper.Tests;

[TestFixture]
public class DapperQueryBuilderTests
{
    [Test]
    public void ShouldReturnSingleValue()
    {
        System.Data.Common.DbConnection conn = Substitute.For<System.Data.Common.DbConnection>().SetupCommands();
        conn.SetupQuery("SELECT DISTINCT FileName FROM Table")
            .Returns(
            new { FileName = "File1" },
            new { FileName = "File2" });


        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        QueryBuilder qb = conn.QueryBuilder($"SELECT DISTINCT FileName FROM Table");

        var ret = qb.Query<string>().ToList();
        ret.Count.Should().Be(2);
    }
}