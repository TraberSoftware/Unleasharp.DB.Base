# 📝 Changelog

## v1.8.4 (2025-10-02)

- ✨ Add `Query.WhereExists()`
- ✨ Add `Query.WhereNotExists()`

## v1.8.3 (2025-09-22)

- ✨ `DuckDB` Add full support for importing from JSON and Parquet files
- 🔄 Improve the logic for determining query type execution: Now, when the `QueryType` is `RAW`, the builder uses `RawQueryType` rather than always `QueryType`

## v1.8.2 (2025-09-21)
- ✨ Add debug logging to help tracing library bugs and query errors
- 🔧 `DuckDB` Fix duplicate `RETURNING {column}` rendering on `INSERT`

## v1.8.1 (2025-09-20)
- ✨ `DuckDB` Add `Query.OnConflict()` support

## v1.8.0 (2025-09-19)
- ✨ Add `ReflectionCache`
- ✨ Add `ResultCache`
- ✨ Add `TinyText`, `MediumText` and `LongText` column data types
- ✨ Add `Query.WhereGreater<T>()`
- ✨ Add `Query.WhereGreaterEquals<T>()`
- ✨ Add `Query.WhereLower<T>()`
- ✨ Add `Query.WhereLowerEquals<T>()`
- ✨ Add `Query.Is<T>()`
- ✨ Add `Query.IsNot<T>()`
- ✨ Add `Query.WhereNotIn<T>()`
- ✨ `DuckDB` Add `Query.__GetColumnDefinition()` data type check for column length in to avoid setting length to colum data types that do not allow it
- 🔄 Revamp `Connector` database connection management logic
- 🔄 Remove unused `IConnectorManager`
- 🔄 Update `QueryBuilder` enumerators logic
- 🔄 Update `QueryBuilder.Execute<T>()` to adjust to latest query execution flow changes
- 🔄 Connector generic database logic is now handled by base class and not by each implementation
- 🔄 `PostgreSQL` Update `QueryBuilder._PrepareDbCommand()` to add casted `NpgsqlParameter` to `NpgsqlCommand` instead of adding anonymous object
- 🔄 `DuckDB` Update `Query.__GetMemberInfoDuckDBParameter()` to convert whole numbers to their specific types
- 🔄 `DuckDB` Update `Query.__GetColumnDefinition()` and `Query.__GetMemberInfoDuckDBParameter()` to handle `Enums` as strings
- 🔄 `DuckDB` Update `QueryBuilder._PrepareDbCommand()` to improve data type management
- 🐛 Revert `Query._RenderSelectSentence()` removal of `SELECT_UNION` check on `SELECT(*)`
- 🐛 `DuckDB` Fix `QueryBuilder._HandleQueryResult()` to handle `UnmanagedMemoryStream` and store them as `MemoryStream`

## v1.7.1 (2025-09-14)
- ✨ `DuckDB` Added support for `DuckDB`
- ✨ Add `QueryBuilder.IterateByOffset<T>()` to iterate tables where there is not auto-increment key column to iterate by
- 🔄 Update `Query.From()` to add table name escaping
- 🔄 Update `ExtensionMethods/DataRow.__HandleRowMemberInfo<T>()` to handle a few edge cases on data serialization

## v1.7.0 (2025-09-11)
- ✨ Add `QueryBuilder` transaction methods: `QueryBuilder.Begin()`, `QueryBuilder.Rollback()`, `QueryBuilder.Commit()`
- ✨ Add `Query.OnConflict()` support for upsert operations
- 🔄 `PostgreSQL` Update `Query.Value<T>()`
- 🔄 `MSSQL` Update `Query.Value<T>()`

## v1.6.0 (2025-09-03)
- ✨ Add support for `UNION`, `UNION ALL`, `INTERSECT` and `EXCEPT` queries

## v1.5.5 (2025-09-01)
- ✨ Add `ExpressionHelper.ExtractPropertyName<T>()`
- ✨ Add `byte[]` array rendering to `Query._RenderPrepared()`
- 🔄 `PostgreSQL` Update `Query.Value<T>()` to avoid rendering system columns on insert
- 🔄 `MSSQSL` Update `Query.Value<T>()` to avoid rendering system columns on insert
- 🐛 Fix `Query.Set<T>()`
- 🐛 Fix `Query.Select<T>()`

## v1.5.4 (2025-09-01)
- ✨ Add XML documentation to code

## v1.5.3 (2025-08-31)
- ✨ Add `Query.Distinct()`

## v1.5.2 (2025-08-30)
- ✨ Add `Query._Engine`
- 🔄 Improve `ConnectorManager` thread safety
- 🐛 Fix `Query.Select<T>()` not taking `SystemColumn.Name` property as the system column name to be retrieved

## v1.5.1 (2025-08-30)
- ✨ Add `SchemaDefinition.SystemColumn`
- ✨ Add `SchemaDefinition.DatabaseEngine`
- 🔄 Update `QueryBuilder.Iterate<T>()`

## v1.5.0 (2025-08-28)
- ✨ Add `Type.GetColumnName()`
- ✨ Add `QueryBuilder.Execute<T>()` to provide return of Last Insert Id, AffectedRows, and Query Success
- ✨ `MySQL` Add little patch to support `Length=-1` on `Char` and `Varchar` column types
- ✨ `PostgreSQL` Add `RETURNING` support for `INSERT` queries
- ✨ `MSSQL` Add `OUTPUT Inserted.` support for `INSERT` queries
- 🔄 Improve Key definition

## v1.4.1 (2025-08-28)
- ✨ `MSSQL` Added support for `Microsoft SQL Server`
- ✨ Add `Query._GetKeyColumnName()`
- 🐛 Fix table key rendering on table create

## v1.4.0 (2025-08-28)
- ✨ Add `SchemaDefinition`/`ForeignKey` constructor to support direct reference for table classes
- ✨ Add `SchemaDefinition`/`Column` constructor to support joined columns
- ✨ Add joined table support for `SELECT` and the result iterators
- ✨ Add Query Expression support to `Query.Join()`
- ✨ Add `Query.Select<T>()`
- ✨ Add `QueryBuilder` select `Tuple` support
- 🔄 Update `QueryBuilder` to improve query result handling when mixing columns from different tables
- 🔄 Update `QueryBuilder.Iterate()` to change ulong for long to widen database engine support
- 🔄 Update `SchemaDefinition`/`NamedStructure` to set Name as `protected`
- 🔄 Improve `DataRow` parsing to custom class

## v1.3.3 (2025-08-26)
- ✨ Add support for .Net 6
 
## v1.3.2 (2025-08-25)
- ✨ Add Query.GetColumnDataType()

## v1.3.1 (2025-08-25)
- ✨ Add `ConnectorManager.WithBeforeQueryExecutionAction()`
- ✨ Add `ConnectorManager.WithAfterQueryExecutionAction()`
- 🔄 Update `ExtensionMethods.DataRow` to avoid serializing values not retrieved by a query result
- 🔄 Update `QueryBuilder.OnQueryExceptionAction` to now take the query and the exception as parameters
- 🐛 Fix `QueryBuilder.OnQueryExceptionAction()` invocation missing the query argument

## v1.3.0 (2025-08-25)
- ✨ Add `Query Expression` support for query building: `Select`, `Set`, `Value`, `Where`, `WhereIn`, `WhereLike`, `GroupBy`, `OrderBy`
- ✨ Add `QueryBuilder.ExecuteScalar()` to add a direct scalar execution method
- ✨ Add `QueryBuilder.ScalarValue` to store the `QueryBuilder.ExecuteScalar()` value and the first column from the first row in a regular select
- ✨ Add `QueryBuilder.WithBeforeQueryExecutionAction()` and `QueryBuilder.WithAfterQueryExecutionAction()` to setup callbacks before and after a query execution happens (logging purposes mostly)

## v1.2.1 (2025-08-23)
- 🔄 `PostgreSQL` Improve query rendering for printing purposes only

## v1.2.0 (2025-08-22)
- ✨ `PostgreSQL` Added support for `PostgreSQL`
- ✨ Add `SchemaDefinition/Column.Check`
- ✨ Add `ConnectorManager.WithConnectionSetup()`
- 🔄 Update `ConnectorManager` to ease overriding connection instantiation and setup
- 🔄 Update `Query` to improve query preparation overriding
- 🔄 Removed the ability to insert a null value on a `Primary Key` column, it will now either insert a not-null value or not insert it
- 🔄 Extension methods refactor

## v1.1.1 (2025-08-21)
- ✨ Add `ConnectionManager.WithOnQueryExceptionAction()` to setup a callback when an exception occurs
- ✨ Add `ColumnDataType.Enum`
- ✨ Add `Query.CreateTable()` aliases
- 🔄 Improve `Query` change detection for rendering
- 🔄 Improve `DataRow` serialization
- 🔄 Improve `Table` key definition
- 🔄 Improve `Column` and `Key` rendering on table creation
- 🔄 Moved `ConnectorManager.QueryBuilder()` and `ConnectorManager.DetachedQueryBuilder()` methods to Base `ConnectorManager` instead of each implementation

## v1.1.0 (2025-08-20)
- ✨ Add support for ColumnDataType
- ✨ Add SchemaDefinition/Column class to support generic database column data typing

## v1.0.2 (2025-08-20)
- ✨ Add `Query.WhereLike()` operator
- 🔄 Update `Query.__RenderWhereValue()` to handle `Enum` values with `Description` attribute
- 🔄 Update `SchemaDefinition/ForeignKey` definition

## v1.0.1 (2025-08-20)
- 🔄 General refactor

## v1.0.0 (2025-08-19)
- ✨ `MySQL` Added support for `MySQL`
- ✨ `SQLite` Added support for `SQLite`