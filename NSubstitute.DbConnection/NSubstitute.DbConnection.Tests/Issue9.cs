using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace NSubstitute.DbConnection.Tests;

using InterpolatedSql.Dapper.SqlBuilders;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Dapper;
using DapperQueryBuilder;

public interface IDbConnectionFactory
{
    public IDbConnection GetConnection(string connectionString);
}
public class DbOptions
{
    public string ConnectionString { get; set; }
}
public interface IHostService
{
    public Task<bool> GetBool(int id);

    public Task<List<string>> GetListOfString();
}

public class HostService : IHostService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IOptionsMonitor<DbOptions> _DbOptions;

    public HostService(IDbConnectionFactory dbConnectionFactory, IOptionsMonitor<DbOptions> optionsMonitor)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _DbOptions = optionsMonitor;
    }

    public async Task<bool> GetBool(int id)
    {
        using (IDbConnection conn = _dbConnectionFactory.GetConnection(_DbOptions.CurrentValue.ConnectionString))
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            QueryBuilder branchesQuery = conn.QueryBuilder(
                $@"
                                    SELECT COUNT(DISTINCT 1) AS Id 
                                    FROM Orders
                                    AND OrderId = {id}
                    ");

            bool result = await branchesQuery.ExecuteScalarAsync<bool>();

            result = await conn.ExecuteScalarAsync<bool>(
                $@"SELECT COUNT(DISTINCT 1) AS Id
                                                    FROM Orders
                                                    AND OrderId = {id}");

            return result;
        }
    }

    public async Task<List<string>> GetListOfString()
    {
        using (IDbConnection conn = _dbConnectionFactory.GetConnection(_DbOptions.CurrentValue.ConnectionString))
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            QueryBuilder qb = conn.QueryBuilder(
                $@"Select Distinct FileName
                            FROM John_Lewis_Transaction_Staging
                                                          ");

            return qb.Query<string>().ToList();
        }
    }
}


[TestFixture]
public class Issue9
{
    [Test]
    public async Task ShouldReturnSingleValue()
    {
        System.Data.Common.DbConnection dbconn = Substitute.For<System.Data.Common.DbConnection>().SetupCommands();
        dbconn.SetupQuery(q => q.Contains("Select Distinct FileName"))
            .Returns(new { FileName = "File1" }, new { FileName = "File2" });
        IDbConnectionFactory connFactory = Substitute.For<IDbConnectionFactory>();
        connFactory.GetConnection(default).ReturnsForAnyArgs(dbconn);

        IOptionsMonitor<DbOptions> opt = Substitute.For<IOptionsMonitor<DbOptions>>();
        DbOptions o = new DbOptions() { ConnectionString = "Test" };
        opt.CurrentValue.ReturnsForAnyArgs(o);

        HostService sut = new HostService(connFactory, opt);

        List<string> ret = await sut.GetListOfString();

        Assert.IsNotEmpty(ret);
    }
}