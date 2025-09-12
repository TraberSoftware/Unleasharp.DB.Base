# Introduction

## What is Unleasharp.DB?

The Unleasharp.DB QueryBuilder library provides a fluent API for building and executing SQL queries with strong typing and expression-based syntax. This documentation provides a comprehensive guide to using the Unleasharp.DB implementations for database operations. The library's fluent API and expression-based syntax make it easy to build complex queries while maintaining type safety and preventing SQL injection vulnerabilities.

### Key Features

- **Fluent API Design**: Intuitive method chaining for query construction
- **Strong Typing**: Compile-time type safety with minimal runtime overhead
- **Expression-Based Syntax**: Leverages C# expressions for query building
- **SQL Injection Prevention**: Automatic parameterization of user inputs
- **Multi-Database Support**: Compatible with major database engines (PostgreSQL, MySQL, SQL Server, SQLite)

### Quick Example

```csharp
var db  = new ConnectorManager("Data Source=unleasharp.db;Version=3;");
var row = db.QueryBuilder().Build(query => query
    .From<ExampleTable>()
    .OrderBy<ExampleTable>(row => row.Id, OrderDirection.DESC)
    .Limit(1)
    .Select()
).FirstOrDefault<ExampleTable>();
```

## What Unleasharp.DB Is Not

Unleasharp.DB QueryBuilder does not intend to replace most common ORMs used in the .NET ecosystem, like Entity Framework or Dapper. It doesn't aim to provide most of the features they offer:

### Features Not Included

- **Entity Tracking**: No change tracking or lazy loading capabilities
- **Migrations**: No database migration system
- **Entity Relationships**: No support for complex relationship mapping
- **Caching**: No built-in query result caching
- **Advanced ORM Features**: No LINQ to Entities, no proxy generation

### When to Use Unleasharp.DB

Use Unleasharp.DB when you need:

✅ **Simple Query Building** - For straightforward CRUD operations  
✅ **Performance-Critical Applications** - When you need maximum query performance  
✅ **Database Agnostic Queries** - For applications targeting multiple database platforms  
✅ **Expression-Based Querying** - When you prefer C# expression syntax over raw SQL  

### When to Consider Alternatives

Avoid Unleasharp.DB when you need:

❌ **Complex Entity Relationships** - For applications with intricate data models  
❌ **Advanced ORM Features** - When you require change tracking or lazy loading  
❌ **Database Migrations** - For applications requiring migration management  
❌ **Entity Framework Integration** - When working within existing EF ecosystems  

## Core Philosophy

Unleasharp.DB is designed to be a lightweight, focused library that excels at one thing: building safe, efficient SQL queries. It's not trying to be a full-featured ORM but rather a powerful query builder that can work alongside other tools in your stack.

> **Note**: While Unleasharp.DB provides excellent query building capabilities, it's recommended to combine it with appropriate data access patterns and consider the overall architecture of your application when choosing between different data access technologies [^1].

[^1]: Microsoft's .NET documentation on data access patterns and ORM considerations: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/

## Supported Databases

The library provides native support for the following database engines:

- **MySQL** (via MySqlConnector) - [Unleasharp.DB.MySQL](https://github.com/TraberSoftware/Unleasharp.DB.MySQL)
- **SQLite** (via System.Data.SQLite.Core) - [Unleasharp.DB.SQLite](https://github.com/TraberSoftware/Unleasharp.DB.SQLite)
- **PostgreSQL** (via Npgsql) - [Unleasharp.DB.PostgreSQL](https://github.com/TraberSoftware/Unleasharp.DB.PostgreSQL)
- **SQL Server** (via Microsoft.Data.SqlClient) - [Unleasharp.DB.MSSQL](https://github.com/TraberSoftware/Unleasharp.DB.MSSQL)

Each provider is optimized for its respective database engine while maintaining consistent API behavior across platforms.