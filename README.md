# NSubstitute.DbConnection

A powerful and flexible extension to NSubstitute for mocking database queries

If there's anything you need to do that's not covered below then raise an [Issue](https://github.com/jmg48/NSubstitute.DbConnection/issues) and we'll see about adding it!

## Installation

The package is available from nuget.org as [NSubstitute.Community.DbConnection](https://www.nuget.org/packages/NSubstitute.Community.DbConnection)

Or, you can run `install-package NSubstitute.Community.DbConnection` from within your IDE

## Getting started

To get started, create a mock connection by calling `.SetupCommands()` on a regular NSubstitute `IDbConnection` mock:

```
    var mockConnection = Substitute.For<IDbConnection>().SetupCommands();
```
or, if you want async support, use `DbConnection` instead of `IDbConnection` - either works fine here:

```
    var mockConnection = Substitute.For<DbConnection>().SetupCommands();
```

You can then fluently add behaviour to your mock connection using the `.SetupQuery()` methods below, starting with some relative "greedy" matching by default, simply on the query text passed in, but with the ability to match parameter values if you need and also match parts of the query text.

You can define single or multiple result sets, using anonymous types or concrete types, including record types.

## Set up a simple query

Set up a query using `.SetupQuery()` and the expected query text, then specify the response using `.Returns()` and an array of anonymous types:

```
    mockConnection.SetupQuery("select * from MyTable").Returns(
        new { Foo = 1, Bar = "abc" },
        new { Foo = 2, Bar = "def" }
    );
```

That's all you need to do - executing this query against the connection will return the specified results:

```
    using var command = mockConnection.CreateCommand();
    command.CommandText = "select * from table";
    mockConnection.Open();
    using var reader = command.ExecuteReader();

    reader.Read();
    reader["Foo"].Should().Be(1);
    reader["Bar"].Should().Be("abc");

    reader.Read();
    reader["Foo"].Should().Be(2);
    reader["Bar"].Should().Be("def");
```
## Works with async

You'll also get the correct mocked behaviour using the async API

```
    await using var command = mockConnection.CreateCommand();
    command.CommandText = "select * from table";
    await mockConnection.OpenAsync();
    await using var reader = command.ExecuteReaderAsync();

    await reader.ReadAsync();
    reader["Foo"].Should().Be(1);
    reader["Bar"].Should().Be("abc");

    await reader.ReadAsync();
    reader["Foo"].Should().Be(2);
    reader["Bar"].Should().Be("def");
```

## Works with Dapper

Because Dapper uses the same calls to DbConnection under the hood, you'll get the correct mocked behaviour here too:

```
    var result = mockConnection.Query<(int Foo, string Bar)>("select * from table").ToList();

    reader[0].Foo.Should().Be(1);
    reader[0].Bar.Should().Be("abc");
    reader[1].Foo.Should().Be(2);
    reader[1].Bar.Should().Be("def");
```

## Query parameters

Use `.WithParameter()` to also match on the value of a parameter passed to the query (by default, a query will match on just the command text, ignoring any parameter values):

```
    mockConnection.SetupQuery("select * from table where Id = @id")
        .WithParameter( "id", 1)
        .Returns(new { Id = 1, Name = "The first one"});

    mockConnection.SetupQuery("select * from table where Id = @id")
        .WithParameter( "id", 2)
        .Returns(new { Id = 2, Name = "The second one"});
```

You can add multiple parameters by chaining calls to `.WithParameter()` or by using a single call to `WithParameters()`

You can also force a query to match _only_ if no parameters are passed by calling `.WithNoParameters()`

## Query text matching

In addition to supplying just the query text to match against, you can also supply a delegate or a regular expression:

```
    mockConnection.SetupQuery(queryText => queryText.Contains("from MyTable"))
        .Returns(new { Foo = 1, Bar = "abc" });

    mockConnection.SetupQuery(new Regex("select .+ from"))
        .Returns(new { Foo = 1, Bar = "abc" });
```

## Concrete result types

As well as anonymous types, you can also use concrete types (including record types) as result types:

```
public class KeyValue
{
    public int Key { get; set; }

    public string Value { get; set; }
}
```

```
public record KeyValueRecord(int Key, string Value);
```

```
    mockConnection.SetupQuery("select * from MyTable")
        .Returns(new KeyValue { Key = 1, Value = "abc" });

    mockConnection.SetupQuery("select * from MyTable")
        .Returns(new KeyValueRecord(1, "abc"));
```

## Multiple result sets

Use `.ThenReturns()` to set up second and subsequent result sets for your query:

```
    mockConnection.SetupQuery("select * from MyTable")
        .Returns(new { Never = 1, Eat = 1 })
        .ThenReturns(new { Shredded = 3, Wheat = 4 });
    );

    using var command = mockConnection.CreateCommand();
    command.CommandText = "select * from table";
    mockConnection.Open();
    using var reader = command.ExecuteReader();

    reader.Read();
    reader["Never"].Should().Be(1);
    reader["Eat"].Should().Be(2);

    reader.NextResult();

    reader.Read();
    reader["Shredded"].Should().Be(3);
    reader["Wheat"].Should().Be(4);
```

## Further examples

Check out the test fixtures in `NSubstitute.DbConnection.Tests` and `NSubstitute.DbConnection.Dapper.Tests` for working examples of the full set of supported functionality.
