using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Unleasharp.DB.Base.ExtensionMethods;
using Unleasharp.DB.Base.QueryBuilding;
using Unleasharp.DB.Base.SchemaDefinition;
using Unleasharp.ExtensionMethods;

namespace Unleasharp.DB.Base;

/// <summary>
/// Provides a generic query builder for database operations, supporting execution, iteration, and result mapping.
/// </summary>
/// <typeparam name="QueryBuilderType">The type of the query builder.</typeparam>
/// <typeparam name="DBConnectorType">The type of the database connector.</typeparam>
/// <typeparam name="DBQueryType">The type of the database query.</typeparam>
/// <typeparam name="DBConnectionType">The type of the database connection.</typeparam>
/// <typeparam name="DBConnectorSettingsType">The type of the database connector settings.</typeparam>
public class QueryBuilder<QueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType>
    where DBConnectionType        : DbConnection
    where DBConnectorSettingsType : DbConnectionStringBuilder
    where DBConnectorType         : Connector<DBConnectorType, DBConnectionType, DBConnectorSettingsType>
    where DBQueryType             : Query<DBQueryType>
    where QueryBuilderType        : QueryBuilder<QueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType>
{
    /// <summary>
    /// Provides logging functionality.
    /// </summary>
    private readonly ILogger _logger = Logging.CreateLogger<QueryBuilderType>();

    /// <summary>
    /// Gets the database connector associated with this query builder.
    /// </summary>
    public DBConnectorType Connector    { get; private set;   }
    /// <summary>
    /// Gets the database query instance used by this query builder.
    /// </summary>
    public DBQueryType     DBQuery      { get; private set;   }
    /// <summary>
    /// Gets the last executed database query instance.
    /// </summary>
    public DBQueryType     LastQuery    { get; private set;   }

    #region Query result data
    /// <summary>
    /// Gets the result of the query as a <see cref="DataTable"/>.
    /// </summary>
    public DataTable       Result         { get; protected set; } = null;
    /// <summary>
    /// Gets the number of rows affected by the query.
    /// </summary>
    public int             AffectedRows   { get; protected set; } = 0;
    /// <summary>
    /// Gets the total count of rows returned or affected by the query.
    /// </summary>
    public int             TotalCount     { get; protected set; } = 0;
    /// <summary>
    /// Gets the scalar value returned by the query, if any.
    /// </summary>
    public object          ScalarValue    { get; protected set; } = null;
    /// <summary>
    /// Gets the last inserted ID from an insert query.
    /// </summary>
    public object          LastInsertedId { get; protected set; } = 0;
    #endregion

    /// <summary>
    /// Gets or sets the action to execute before query execution.
    /// </summary>
    public Action<DBQueryType>            BeforeQueryExecutionAction { get; protected set; }
    /// <summary>
    /// Gets or sets the action to execute after query execution.
    /// </summary>
    public Action<DBQueryType>            AfterQueryExecutionAction  { get; protected set; }
    /// <summary>
    /// Gets or sets the action to execute when a query exception occurs.
    /// </summary>
    public Action<DBQueryType, Exception> OnQueryExceptionAction     { get; private   set; }

    /// <summary>
    /// Gets or sets the SQL query template used to begin a database transaction.
    /// </summary>
    protected virtual string _TransactionBeginQuery    { get; set; } = "BEGIN TRANSACTION {0};";

    /// <summary>
    /// Gets or sets the SQL query template used to commit a database transaction.
    /// </summary>
    protected virtual string _TransactionCommitQuery   { get; set; } = "COMMIT TRANSACTION {0}";

    /// <summary>
    /// Gets or sets the SQL query template used to roll back a database transaction.
    /// </summary>
    protected virtual string _TransactionRollbackQuery { get; set; } = "ROLLBACK TRANSACTION {0};";

    /// <summary>
    /// Gets or sets a value indicating whether the current operation is holding a transaction.
    /// </summary>
    public bool InTransaction {
        get {
            return Transactions.Count > 0;
        }
    }

    /// <summary>
    /// Gets the collection of transaction identifiers associated with the current instance.
    /// </summary>
    public List<string> Transactions { get; protected set; } = new List<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBuilder{QueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType}"/> class with the specified connector.
    /// </summary>
    /// <param name="connector">The database connector.</param>
    public QueryBuilder(DBConnectorType connector) {
        this.Connector = connector;

        this.DBQuery = Activator.CreateInstance<DBQueryType>();

        _logger.LogDebug("QueryBuilder instantiated for connector {ConnectorType}", connector?.GetType().FullName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBuilder{QueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType}"/> class with the specified connector and query.
    /// </summary>
    /// <param name="connector">The database connector.</param>
    /// <param name="query">The database query.</param>
    public QueryBuilder(DBConnectorType connector, DBQueryType query) {
        this.Connector = connector;

            this.DBQuery = query;

        _logger.LogDebug("QueryBuilder instantiated for connector {ConnectorType} with provided query of type {QueryType}", connector?.GetType().FullName, query?.GetType().FullName);
    }

    /// <summary>
    /// Sets the action to execute when a query exception occurs.
    /// </summary>
    /// <param name="onQueryExceptionAction">The exception action.</param>
    /// <returns>The current query builder instance.</returns>
    public QueryBuilderType WithOnQueryExceptionAction(Action<DBQueryType, Exception> onQueryExceptionAction) {
        this.OnQueryExceptionAction = onQueryExceptionAction;

        return (QueryBuilderType)this;
    }

    /// <summary>
    /// Sets the action to execute before query execution.
    /// </summary>
    /// <param name="beforeQueryExecutionAction">The action to execute before query execution.</param>
    /// <returns>The current query builder instance.</returns>
    public QueryBuilderType WithBeforeQueryExecutionAction(Action<DBQueryType> beforeQueryExecutionAction) {
        this.BeforeQueryExecutionAction = beforeQueryExecutionAction;

        return (QueryBuilderType)this;
    }

    /// <summary>
    /// Sets the action to execute after query execution.
    /// </summary>
    /// <param name="afterQueryExecutionAction">The action to execute after query execution.</param>
    /// <returns>The current query builder instance.</returns>
    public QueryBuilderType WithAfterQueryExecutionAction(Action<DBQueryType> afterQueryExecutionAction) {
        this.AfterQueryExecutionAction = afterQueryExecutionAction;

        return (QueryBuilderType)this;
    }

    /// <summary>
    /// Invokes the query exception action if set.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    protected void _OnQueryException(Exception ex) {
        _logger.LogError(ex, "Query exception while executing query: {Query}", this.DBQuery?.Render());

        if (this.OnQueryExceptionAction != null) {
            this.OnQueryExceptionAction.Invoke(this.DBQuery, ex);
        }
    }

    /// <summary>
    /// Invokes the before query execution action and resets the result.
    /// </summary>
    protected void _BeforeQueryExecution() {
        this._ResetResult();

        _logger.LogDebug("Preparing to execute query. Type: {QueryType}, Query: {QueryString}", this.DBQuery?.QueryType, this.DBQuery?.Render());

        if (this.BeforeQueryExecutionAction != null) {
            this.BeforeQueryExecutionAction.Invoke(this.DBQuery);
        }
    }

    /// <summary>
    /// Invokes the after query execution action and sets the scalar value if available.
    /// </summary>
    protected void _AfterQueryExecution() {
        if (this.Result != null && this.Result.Rows.Count > 0 && this.Result.Columns.Count > 0) {
            this.ScalarValue = this.Result.Rows[0][0];
        }
        this.LastQuery = this.DBQuery.DeepClone();
        this.DBQuery   = Activator.CreateInstance<DBQueryType>();

        int rows = this.Result?.Rows?.Count ?? 0;
        _logger.LogDebug("Query executed. Type: {QueryType}, AffectedRows: {AffectedRows}, ScalarValue: {Scalar}, LastInsertedId: {LastInsertedId}, RetrievedRows: {RetrievedRows}",
            this.LastQuery?.QueryType, this.AffectedRows, this.ScalarValue, this.LastInsertedId, rows);

        if (this.AfterQueryExecutionAction != null) {
            this.AfterQueryExecutionAction.Invoke(this.LastQuery);
        }
    }

    /// <summary>
    /// Applies the specified action to the query and returns the current query builder instance.
    /// </summary>
    /// <param name="action">The action to apply to the query.</param>
    /// <returns>The current query builder instance.</returns>
    public QueryBuilderType Build(Action<DBQueryType> action) {
        action.Invoke(this.DBQuery);

        _logger.LogDebug("Query built/modified. Type: {QueryType}", this.DBQuery?.QueryType);

        return (QueryBuilderType) this;
    }

    #region Query execution
    /// <summary>
    /// Resets the query result and related properties.
    /// </summary>
    protected void _ResetResult() {
        this.Result         = null;
        this.AffectedRows   = 0;
        this.TotalCount     = 0;
        this.ScalarValue    = null;
        this.LastInsertedId = null;
    }

    #region Query execution aliases
    /// <summary>
    /// Executes the query and returns the current query builder instance.
    /// </summary>
    /// <param name="force">If true, forces execution even if results are already available.</param>
    /// <returns>The current query builder instance.</returns>
    public virtual QueryBuilderType Execute() {
        this._BeforeQueryExecution();
        this._Execute();
        this._AfterQueryExecution();

        return (QueryBuilderType) this;
    }

    /// <summary>
    /// Executes the query and returns a result of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the result to return.</typeparam>
    /// <param name="force">If true, forces execution even if results are already available.</param>
    /// <returns>The result of the query as the specified type.</returns>
    public virtual T Execute<T>() {
        this._BeforeQueryExecution();
        bool querySuccess = this._Execute();
        this._AfterQueryExecution();

        object? result = null;
        switch (this.LastQuery.QueryType == QueryType.RAW ? this.LastQuery.RawQueryType : this.LastQuery.QueryType) {
            case QueryType.COUNT:
                result = true switch {
                    true when typeof(T) == typeof(bool)  => this.TotalCount > 0,
                    true when typeof(T) == typeof(int)   => this.TotalCount,
                    true when typeof(T) == typeof(long)  => this.TotalCount,
                    true when typeof(T) == typeof(uint)  => this.TotalCount,
                    true when typeof(T) == typeof(ulong) => this.TotalCount
                };
                break;
            case QueryType.SELECT:
                result = true switch {
                    true when typeof(T) == typeof(bool)  => (this.Result?.Rows?.Count ?? 0) > 0,
                    true when typeof(T) == typeof(int)   =>  this.Result?.Rows?.Count ?? 0,
                    true when typeof(T) == typeof(long)  =>  this.Result?.Rows?.Count ?? 0,
                    true when typeof(T) == typeof(uint)  =>  this.Result?.Rows?.Count ?? 0,
                    true when typeof(T) == typeof(ulong) =>  this.Result?.Rows?.Count ?? 0
                };
                break;
            case QueryType.INSERT:
                result = true switch {
                    true when typeof(T) == typeof(bool)   => this.AffectedRows == this.LastQuery.QueryValues.Count,
                    true when typeof(T) == typeof(int)    => this.LastQuery.QueryValues.Count == 1 ? this.LastInsertedId : this.AffectedRows,
                    true when typeof(T) == typeof(long)   => this.LastQuery.QueryValues.Count == 1 ? this.LastInsertedId : this.AffectedRows,
                    true when typeof(T) == typeof(uint)   => this.LastQuery.QueryValues.Count == 1 ? this.LastInsertedId : this.AffectedRows,
                    true when typeof(T) == typeof(ulong)  => this.LastQuery.QueryValues.Count == 1 ? this.LastInsertedId : this.AffectedRows,
                    true when typeof(T) == typeof(string) => this.LastQuery.QueryValues.Count == 1 ? this.LastInsertedId : this.AffectedRows
                };
                break;
            case QueryType.UPDATE:
                result = true switch {
                    true when typeof(T) == typeof(bool)  => this.AffectedRows > 0,
                    true when typeof(T) == typeof(int)   => this.AffectedRows,
                    true when typeof(T) == typeof(long)  => this.AffectedRows,
                    true when typeof(T) == typeof(uint)  => this.AffectedRows,
                    true when typeof(T) == typeof(ulong) => this.AffectedRows
                };
                break;
            case QueryType.DELETE:
                result = true switch {
                    true when typeof(T) == typeof(bool)  => this.AffectedRows > 0,
                    true when typeof(T) == typeof(int)   => this.AffectedRows,
                    true when typeof(T) == typeof(long)  => this.AffectedRows,
                    true when typeof(T) == typeof(uint)  => this.AffectedRows,
                    true when typeof(T) == typeof(ulong) => this.AffectedRows
                };
                break;
            case QueryType.CREATE:
            default:
                result = true switch {
                    true when typeof(T) == typeof(bool) => querySuccess
                };
                break;
        }

        if (result != null) {
            return (T)Convert.ChangeType(result, typeof(T));
        }
        return default(T);
    }

    /// <summary>
    /// Executes the query and returns a scalar value of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the scalar value to return.</typeparam>
    /// <param name="force">If true, forces execution even if results are already available.</param>
    /// <returns>The scalar value of the query as the specified type.</returns>
    public virtual T ExecuteScalar<T>() {
        this._BeforeQueryExecution();
        T scalarValue = this._ExecuteScalar<T>();
        this._AfterQueryExecution();

        this.ScalarValue = scalarValue;
        return scalarValue;
    }

    /// <summary>
    /// Asynchronously executes the query and returns the current query builder instance.
    /// </summary>
    /// <param name="force">If true, forces execution even if results are already available.</param>
    /// <returns>A task representing the asynchronous operation, with the current query builder instance as the result.</returns>
    public virtual async Task<QueryBuilderType> ExecuteAsync() {
        this._BeforeQueryExecution();
        await this._ExecuteAsync();
        this._AfterQueryExecution();

        return (QueryBuilderType)this;
    }
    #endregion

    #region Row Tracking
    /// <summary>
    /// Updates the specified row in the database if changes are detected.
    /// </summary>
    /// <remarks>This method compares the provided row with the cached version of the row to detect changes.
    /// If changes are found:
    /// <list type="bullet">
    /// <item><description>The table must have a primary key defined.</description></item>
    /// <item><description>The primary key value must be present in the cached data.</description></item>
    /// </list>
    /// If these conditions are not met, the method returns <see langword="false"/>  without performing any updates.</remarks>
    /// <param name="row">The row object to be updated. The object must represent a valid database row and include a primary or unique key value.</param>
    /// <returns><see langword="true"/> if the row was successfully updated; otherwise, <see langword="false"/>.</returns>
    public bool Update(object row) {
        _logger.LogDebug("Update called for row hash {Hash}", row?.GetHashCode());
        ResultCacheRow cached = ResultCache.Get(row.GetHashCode());

        if (cached != null) {
            Dictionary<string, object> rowData    = row.ToDynamicDictionary();
            Dictionary<string, object> rowChanges = cached.Data.CompareWith(rowData);

            if (rowChanges.Count > 0) {
                if (string.IsNullOrEmpty(cached.KeyColumnName)) {
                    // Can't update a row for a table without a Primary Key
                    _logger.LogWarning("Update aborted: table {Table} has no primary key defined.", cached.Table?.Name);
                    return false;
                }
                if (!cached.Data.ContainsKey(cached.KeyColumnName) || cached.KeyColumnName == null) {
                    // Can't update a row if we don't have the Primary Key value or it is null
                    _logger.LogWarning("Update aborted: primary or unique key value for column {KeyColumn} not present in cached data.", cached.KeyColumnName);
                    return false;
                }
                if (string.IsNullOrWhiteSpace(cached.Data[cached.KeyColumnName].ToString())) {
                    // Can't update a row if Primary Key value is empty or null
                    _logger.LogWarning("Update aborted: primary or unique key value for column {KeyColumn} is empty or null.", cached.KeyColumnName);
                    return false;
                }

                _logger.LogDebug("Detected {ChangeCount} changed fields for update on table {Table}. Proceeding to build update query.", rowChanges.Count, cached.Table?.Name);
                this._ResetResult();
                this.DBQuery = Activator.CreateInstance<DBQueryType>();

                this.DBQuery
                    .Update()
                    .From(cached.Table.Name)
                    .Set(rowChanges)
                    .Where(cached.KeyColumn, cached.Data[cached.KeyColumnName])
                ;
                return this.Execute<bool>();
            }
            else {
                _logger.LogDebug("No changes detected for row hash {Hash}. Update skipped.", row.GetHashCode());
            }
        }
        else {
            _logger.LogDebug("No cached row found for hash {Hash}. Update skipped.", row.GetHashCode());
        }

        return false;
    }
    #endregion

    /// <inheritdoc/>
    protected virtual bool _Execute() {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected virtual T _ExecuteScalar<T>() {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected virtual async Task<bool> _ExecuteAsync() {
        throw new NotImplementedException();
    }
    #endregion

    #region Data iteration
    /// <summary>
    /// Gets the number of pages based on the query limit and total count.
    /// </summary>
    /// <returns>The number of pages.</returns>
    public virtual int Pages() {
        int total = this.Count();
        if (this.DBQuery.QueryLimit != null && this.DBQuery.QueryLimit.Count > 0) {
            return (int) Math.Ceiling(Decimal.Divide(this.DBQuery.QueryLimit.Count, total));
        }

        return 0;
    }

    /// <summary>
    /// Executes a count query and returns the total count of rows.
    /// </summary>
    /// <returns>The total count of rows.</returns>
    public virtual int Count() {
        // Set the QueryType to COUNT but store the current type to restore it later
        QueryType currentQueryType = this.DBQuery.QueryType;
        this.DBQuery.SetQueryType(QueryBuilding.QueryType.COUNT);

        // Forcefully execute the query to retrieve the count
        this._BeforeQueryExecution();
        this._Execute();
        this._AfterQueryExecution();

        // Set the query type back
        this.DBQuery.SetQueryType(currentQueryType);

        _logger.LogDebug("Count executed. TotalCount: {TotalCount}", this.TotalCount);

        return this.TotalCount;
    }

    #region Data iteration - Iterate
    /// <summary>
    /// Iterates over the query results and maps each row to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="byKeyField">The key field to iterate by.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="batchSize">The batch size for iteration.</param>
    /// <returns>An enumerable of objects of type <typeparamref name="T"/>.</returns>
    public virtual IEnumerable<T> Iterate<T>(string byKeyField, long offset = 0, int batchSize = 100) where T : class {
        if (this.DBQuery.QuerySelect.Count == 0) {
            this.DBQuery.Select<T>();
        }
        if (this.DBQuery.QueryFrom.Count == 0) {
            this.DBQuery.From<T>();
        }

        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField), offset, batchSize)) {
            T TRow = row.GetObject<T>();
            ResultCache.Set(TRow);

            yield return TRow;
        }
    }

    /// <summary>
    /// Iterates over the query results and maps each row to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="batchSize">The batch size for iteration.</param>
    /// <returns>An enumerable of objects of type <typeparamref name="T"/>.</returns>
    public virtual IEnumerable<T> Iterate<T>(FieldSelector byKeyField, long offset = 0, int batchSize = 100) where T : class {
        if (this.DBQuery.QuerySelect.Count == 0) {
            this.DBQuery.Select<T>();
        }
        if (this.DBQuery.QueryFrom.Count == 0) {
            this.DBQuery.From<T>();
        }

        foreach (DataRow row in this.Iterate(byKeyField, offset, batchSize)) {
            T TRow = row.GetObject<T>();
            ResultCache.Set(TRow);

            yield return TRow;
        }
    }

    /// <summary>
    /// Iterates over the query results and maps each row to an object of type <typeparamref name="T"/> using an expression to select the key field.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="expression">The expression to select the key field.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="batchSize">The batch size for iteration.</param>
    /// <returns>An enumerable of objects of type <typeparamref name="T"/>.</returns>
    public virtual IEnumerable<T> Iterate<T>(Expression<Func<T, object>> expression, long offset = 0, int batchSize = 100) where T : class {
        string tableName  = ReflectionCache.GetTableName<T>();
        string columnName = ReflectionCache.GetColumnName<T>(expression);

        if (this.DBQuery.QuerySelect.Count == 0) {
            this.DBQuery.Select<T>();
        }
        if (this.DBQuery.QueryFrom.Count == 0) {
            this.DBQuery.From<T>();
        }

        foreach (DataRow row in this.Iterate(new FieldSelector {
            Table  = tableName,
            Field  = columnName,
            Escape = true
        }, offset, batchSize)) {
            T TRow = row.GetObject<T>();
            ResultCache.Set(TRow);

            yield return TRow;
        }
    }

    /// <summary>
    /// Iterates over the query results as <see cref="DataRow"/> objects using the specified key field.
    /// </summary>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="batchSize">The batch size for iteration.</param>
    /// <returns>An enumerable of <see cref="DataRow"/> objects.</returns>
    public virtual IEnumerable<DataRow> Iterate(FieldSelector byKeyField, long offset = 0, int batchSize = 100) {
        this.DBQuery.Select();

        long lastId   = offset;
        bool endFound = false;

        DBQueryType originalQuery = this.DBQuery.DeepClone();

        while (!endFound) {
            this._ResetResult();
            DBQueryType cachedQuery = originalQuery.DeepClone();

            cachedQuery.Where(new Where<DBQueryType> {
                Field       = byKeyField,
                Operator    = WhereOperator.AND,
                Comparer    = WhereComparer.GREATER,
                Value       = lastId,
                EscapeValue = true
            });
            cachedQuery.Limit(batchSize);
            this.DBQuery = cachedQuery;

            if (!this.Execute<bool>()) {
                endFound = true;
                break;
            }

            if (this.Result?.Rows == null || this.Result?.Rows.Count == 0) {
                endFound = true;
                break;
            }

            foreach (DataRow row in this.Result?.Rows) {
                try {
                    string byKeyColumnName = !string.IsNullOrWhiteSpace(byKeyField.Table) ? $"{byKeyField.Table}::{byKeyField.Field}" : byKeyField.Field;
                    if (!row.Table.Columns.Contains(byKeyColumnName)) {
                        byKeyColumnName = byKeyField.Field;
                    }

                    if (row[byKeyColumnName].TryConvert<long>(out long latestRowId)) { 
                        if (latestRowId > lastId) {
                            lastId = latestRowId;
                        }
                    }
                    else {
                        endFound = true;
                        break;
                    }
                }
                catch (Exception ex) {
                    endFound = true;
                    break;
                }

                yield return row;
            }
        }

        this.DBQuery = originalQuery;
    }

    /// <summary>
    /// Iterates over the query results using limit-offset statements and maps each row to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <param name="offset">The starting offset.</param>
    /// <param name="batchSize">The batch size for iteration.</param>
    /// <returns>An enumerable of objects of type <typeparamref name="T"/>.</returns>
    public virtual IEnumerable<T> IterateByOffset<T>(long offset = 0, int batchSize = 100) where T : class {
        if (this.DBQuery.QuerySelect.Count == 0) {
            this.DBQuery.Select<T>();
        }
        if (this.DBQuery.QueryFrom.Count == 0) {
            this.DBQuery.From<T>();
        }

        foreach (DataRow row in this.IterateByOffset(offset, batchSize)) {
            T TRow = row.GetObject<T>();
            ResultCache.Set(TRow);

            yield return TRow;
        }
    }

    /// <summary>
    /// Iterates over the query results as <see cref="DataRow"/> objects using limit-offset statements.
    /// </summary>
    /// <param name="offset">The starting offset.</param>
    /// <param name="batchSize">The batch size for iteration.</param>
    /// <returns>An enumerable of <see cref="DataRow"/> objects.</returns>
    public virtual IEnumerable<DataRow> IterateByOffset(long offset = 0, int batchSize = 100) {
        this.DBQuery.Select();

        bool endFound = false;

        DBQueryType originalQuery = this.DBQuery.DeepClone();

        while (!endFound) {
            this._ResetResult();
            DBQueryType cachedQuery = originalQuery.DeepClone();

            cachedQuery.Limit(batchSize, offset);
            this.DBQuery = cachedQuery;
            
            if (this.Execute<bool>()) {
                if (this.Result?.Rows == null || this.Result?.Rows.Count == 0) {
                    endFound = true;
                    break;
                }

                foreach (DataRow row in this.Result?.Rows) {
                    offset++;

                    yield return row;
                }
            }
        }

        this.DBQuery = originalQuery;
    }
    #endregion

    #region Data iteration - AsEnumerable
    /// <summary>
    /// Returns the query results as an enumerable of <see cref="DataRow"/> objects.
    /// </summary>
    /// <returns>An enumerable of <see cref="DataRow"/> objects.</returns>
    public virtual IEnumerable<DataRow> AsEnumerable() {
        if (this.Result == null || this.DBQuery.HasChanged) {
            this.Execute();
        }

        foreach (DataRow row in this.Result?.Rows) {
            yield return row;
        }
    }

    /// <summary>
    /// Returns the query results as an enumerable of objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <returns>An enumerable of objects of type <typeparamref name="T"/>.</returns>
    public virtual IEnumerable<T> AsEnumerable<T>() where T : class {
        foreach (DataRow row in this.AsEnumerable()) {
            T TRow = row.GetObject<T>();
            ResultCache.Set(TRow);

            yield return TRow;
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of <see cref="DataRow"/> objects.
    /// </summary>
    /// <returns>An async enumerable of <see cref="DataRow"/> objects.</returns>
    protected virtual async IAsyncEnumerable<DataRow> AsEnumerableAsync() {
        if (this.Result == null || this.DBQuery.HasChanged) {
            await this.ExecuteAsync();
        }

        foreach (DataRow row in this.Result?.Rows) {
            yield return row;
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <returns>An async enumerable of objects of type <typeparamref name="T"/>.</returns>
    public virtual async IAsyncEnumerable<T> AsEnumerableAsync<T>() where T : class {
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            T TRow = row.GetObject<T>();
            ResultCache.Set(TRow);

            yield return TRow;
        }
    }
    #endregion

    #region Data iteration - ToList
    /// <summary>
    /// Returns the query results as a list of objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <returns>A list of objects of type <typeparamref name="T"/>.</returns>
    public virtual List<T> ToList<T>() where T : class{
        if (this.Result == null || this.DBQuery.HasChanged) {
            this.Execute();
        }

        return this.Result?.AsEnumerable().Select(row => {
            T TRow = row.GetObject<T>();
            ResultCache.Set(TRow);

            return TRow;
        }).ToList();
    }

    /// <summary>
    /// Asynchronously returns the query results as a list of objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map each row to.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a list of objects of type <typeparamref name="T"/> as the result.</returns>
    public virtual async Task<List<T>> ToListAsync<T>() where T : class{
        if (this.Result == null || this.DBQuery.HasChanged) {
            await this.ExecuteAsync();
        }

        return this.Result?.AsEnumerable().Select(row => {
            T TRow = row.GetObject<T>();
            ResultCache.Set(TRow);

            return TRow;
        }).ToList();
    }
    #endregion

    #region Data Iteration - FirstOrDefault
    /// <summary>
    /// Returns the first row of the query result or null if no rows are available.
    /// </summary>
    /// <returns>The first <see cref="DataRow"/> or null.</returns>
    public virtual DataRow FirstOrDefault() {
        this.DBQuery.Limit(1, this.DBQuery.QueryLimit?.Offset ?? 0);

        foreach (DataRow row in this.AsEnumerable()) {
            return row;
        }

        return null;
    }

    /// <summary>
    /// Returns the first row of the query result mapped to an object of type <typeparamref name="T"/>, or null if no rows are available.
    /// </summary>
    /// <typeparam name="T">The type to map the row to.</typeparam>
    /// <returns>The first object of type <typeparamref name="T"/> or null.</returns>
    public virtual T FirstOrDefault<T>() where T : class {
        T TRow = this.FirstOrDefault()?.GetObject<T>();
        ResultCache.Set(TRow);

        return TRow;
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result or null if no rows are available.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, with the first <see cref="DataRow"/> or null as the result.</returns>
    public virtual async Task<DataRow> FirstOrDefaultAsync() {
        this.DBQuery.Limit(1, this.DBQuery.QueryLimit?.Offset ?? 0);

        await foreach (DataRow row in this.AsEnumerableAsync()) {
            return row;
        }

        return null;
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result mapped to an object of type <typeparamref name="T"/>, or null if no rows are available.
    /// </summary>
    /// <typeparam name="T">The type to map the row to.</typeparam>
    /// <returns>A task representing the asynchronous operation, with the first object of type <typeparamref name="T"/> or null as the result.</returns>
    public virtual async Task<T> FirstOrDefaultAsync<T>() where T : class {
        T TRow = (await this.FirstOrDefaultAsync())?.GetObject<T>();
        ResultCache.Set(TRow);

        return TRow;
    }
    #endregion

    #region Tuple enumerators
    #region Tuple enumerators - FirstOrDefault
    /// <summary>
    /// Returns the first row of the query result mapped to a tuple of two objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <returns>A tuple of two objects or null.</returns>
    public virtual Tuple<T1, T2> FirstOrDefault<T1, T2>()
        where T1 : class
        where T2 : class
    {
        return this.FirstOrDefault()?.GetTuple<T1, T2>();
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result mapped to a tuple of two objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a tuple of two objects or null as the result.</returns>
    public virtual async Task<Tuple<T1, T2>> FirstOrDefaultAsync<T1, T2>()
        where T1 : class
        where T2 : class
    {
        return (await this.FirstOrDefaultAsync())?.GetTuple<T1, T2>();
    }

    /// <summary>
    /// Returns the first row of the query result mapped to a tuple of three objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <returns>A tuple of three objects or null.</returns>
    public virtual Tuple<T1, T2, T3> FirstOrDefault<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3>();
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result mapped to a tuple of three objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a tuple of three objects or null as the result.</returns>
    public virtual async Task<Tuple<T1, T2, T3>> FirstOrDefaultAsync<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class
    {
        return (await this.FirstOrDefaultAsync())?.GetTuple<T1, T2, T3>();
    }

    /// <summary>
    /// Returns the first row of the query result mapped to a tuple of four objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <returns>A tuple of four objects or null.</returns>
    public virtual Tuple<T1, T2, T3, T4> FirstOrDefault<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
    {
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4>();
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result mapped to a tuple of four objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a tuple of four objects or null as the result.</returns>
    public virtual async Task<Tuple<T1, T2, T3, T4>> FirstOrDefaultAsync<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
    {
        return (await this.FirstOrDefaultAsync())?.GetTuple<T1, T2, T3, T4>();
    }

    /// <summary>
    /// Returns the first row of the query result mapped to a tuple of five objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <returns>A tuple of five objects or null.</returns>
    public virtual Tuple<T1, T2, T3, T4, T5> FirstOrDefault<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
    {
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4, T5>();
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result mapped to a tuple of five objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a tuple of five objects or null as the result.</returns>
    public virtual async Task<Tuple<T1, T2, T3, T4, T5>> FirstOrDefaultAsync<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
    {
        return (await this.FirstOrDefaultAsync())?.GetTuple<T1, T2, T3, T4, T5>();
    }

    /// <summary>
    /// Returns the first row of the query result mapped to a tuple of six objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <returns>A tuple of six objects or null.</returns>
    public virtual Tuple<T1, T2, T3, T4, T5, T6> FirstOrDefault<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
    {
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4, T5, T6>();
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result mapped to a tuple of six objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a tuple of six objects or null as the result.</returns>
    public virtual async Task<Tuple<T1, T2, T3, T4, T5, T6>> FirstOrDefaultAsync<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
    {
        return (await this.FirstOrDefaultAsync())?.GetTuple<T1, T2, T3, T4, T5, T6>();
    }

    /// <summary>
    /// Returns the first row of the query result mapped to a tuple of seven objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <returns>A tuple of seven objects or null.</returns>
    public virtual Tuple<T1, T2, T3, T4, T5, T6, T7> FirstOrDefault<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
    {
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4, T5, T6, T7>();
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result mapped to a tuple of seven objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a tuple of seven objects or null as the result.</returns>
    public virtual async Task<Tuple<T1, T2, T3, T4, T5, T6, T7>> FirstOrDefaultAsync<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
    {
        return (await this.FirstOrDefaultAsync())?.GetTuple<T1, T2, T3, T4, T5, T6, T7>();
    }

    /// <summary>
    /// Returns the first row of the query result mapped to a tuple of eight objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <typeparam name="T8">The type of the eighth tuple item.</typeparam>
    /// <returns>A tuple of eight objects or null.</returns>
    public virtual Tuple<T1, T2, T3, T4, T5, T6, T7, T8> FirstOrDefault<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class
    {
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
    }

    /// <summary>
    /// Asynchronously returns the first row of the query result mapped to a tuple of eight objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <typeparam name="T8">The type of the eighth tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a tuple of eight objects or null as the result.</returns>
    public virtual async Task<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> FirstOrDefaultAsync<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class
    {
        return (await this.FirstOrDefaultAsync())?.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
    }
    #endregion

    #region Tuple enumerators - AsEnumerable
    /// <summary>
    /// Returns the query results as an enumerable of tuples of two objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <returns>An enumerable of tuples of two objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2>> AsEnumerable<T1, T2>()
        where T1 : class 
        where T2 : class
    {
        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2>();
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of tuples of two objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <returns>An async enumerable of tuples of two objects.</returns>
    public virtual async IAsyncEnumerable<Tuple<T1, T2>> AsEnumerableAsync<T1, T2>()
        where T1 : class
        where T2 : class
    {
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2>();
        }
    }

    /// <summary>
    /// Returns the query results as an enumerable of tuples of three objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <returns>An enumerable of tuples of three objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3>> AsEnumerable<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class
    {
        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2, T3>();
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of tuples of three objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <returns>An async enumerable of tuples of three objects.</returns>
    public virtual async IAsyncEnumerable<Tuple<T1, T2, T3>> AsEnumerableAsync<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class
    {

        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2, T3>();
        }
    }

    /// <summary>
    /// Returns the query results as an enumerable of tuples of four objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <returns>An enumerable of tuples of four objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4>> AsEnumerable<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        this.Execute();
        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2, T3, T4>();
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of tuples of four objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <returns>An async enumerable of tuples of four objects.</returns>
    public virtual async IAsyncEnumerable<Tuple<T1, T2, T3, T4>> AsEnumerableAsync<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        await this.ExecuteAsync();
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2, T3, T4>();
        }
    }

    /// <summary>
    /// Returns the query results as an enumerable of tuples of five objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <returns>An enumerable of tuples of five objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5>> AsEnumerable<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        this.Execute();
        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5>();
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of tuples of five objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <returns>An async enumerable of tuples of five objects.</returns>
    public virtual async IAsyncEnumerable<Tuple<T1, T2, T3, T4, T5>> AsEnumerableAsync<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        await this.ExecuteAsync();
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5>();
        }
    }

    /// <summary>
    /// Returns the query results as an enumerable of tuples of six objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <returns>An enumerable of tuples of six objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6>> AsEnumerable<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        this.Execute();
        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6>();
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of tuples of six objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <returns>An async enumerable of tuples of six objects.</returns>
    public virtual async IAsyncEnumerable<Tuple<T1, T2, T3, T4, T5, T6>> AsEnumerableAsync<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        await this.ExecuteAsync();
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6>();
        }
    }

    /// <summary>
    /// Returns the query results as an enumerable of tuples of seven objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <returns>An enumerable of tuples of seven objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7>> AsEnumerable<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        this.Execute();
        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7>();
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of tuples of seven objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <returns>An async enumerable of tuples of seven objects.</returns>
    public virtual async IAsyncEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7>> AsEnumerableAsync<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        await this.ExecuteAsync();
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7>();
        }
    }

    /// <summary>
    /// Returns the query results as an enumerable of tuples of eight objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <typeparam name="T8">The type of the eighth tuple item.</typeparam>
    /// <returns>An enumerable of tuples of eight objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> AsEnumerable<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        this.Execute();
        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
        }
    }

    /// <summary>
    /// Asynchronously returns the query results as an enumerable of tuples of eight objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <typeparam name="T8">The type of the eighth tuple item.</typeparam>
    /// <returns>An async enumerable of tuples of eight objects.</returns>
    public virtual async IAsyncEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> AsEnumerableAsync<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class
    {
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
        }
    }
    #endregion

    #region Tuple enumerators - ToList
    /// <summary>
    /// Returns the query results as a list of tuples of two objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <returns>A list of tuples of two objects.</returns>
    public virtual List<Tuple<T1, T2>> ToList<T1, T2>()
        where T1 : class
        where T2 : class 
    {
        return this.AsEnumerable().Select(row => row.GetTuple<T1, T2>()).ToList();
    }

    /// <summary>
    /// Asynchronously returns the query results as a list of tuples of two objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a list of tuples of two objects as the result.</returns>
    public virtual async Task<List<Tuple<T1, T2>>> ToListAsync<T1, T2>()
        where T1 : class
        where T2 : class
    {
        return await this.AsEnumerableAsync().Select(row => row.GetTuple<T1, T2>()).ToListAsync();
    }

    /// <summary>
    /// Returns the query results as a list of tuples of three objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <returns>A list of tuples of three objects.</returns>
    public virtual List<Tuple<T1, T2, T3>> ToList<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class
    {
        if (this.Result == null || this.DBQuery.HasChanged) {
            this.Execute();
        }

        return this.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3>()).ToList();
    }

    /// <summary>
    /// Asynchronously returns the query results as a list of tuples of three objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a list of tuples of three objects as the result.</returns>
    public virtual async Task<List<Tuple<T1, T2, T3>>> ToListAsync<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class
    {
        if (this.Result == null || this.DBQuery.HasChanged) {
            await this.ExecuteAsync();
        }

        return await this.AsEnumerableAsync().Select(row => row.GetTuple<T1, T2, T3>()).ToListAsync();
    }

    /// <summary>
    /// Returns the query results as a list of tuples of four objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <returns>A list of tuples of four objects.</returns>
    public virtual List<Tuple<T1, T2, T3, T4>> ToList<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        this.Execute();
        return this.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4>()).ToList();
    }

    /// <summary>
    /// Asynchronously returns the query results as a list of tuples of four objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a list of tuples of four objects as the result.</returns>
    public virtual async Task<List<Tuple<T1, T2, T3, T4>>> ToListAsync<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        await this.ExecuteAsync();
        return await this.AsEnumerableAsync().Select(row => row.GetTuple<T1, T2, T3, T4>()).ToListAsync();
    }

    /// <summary>
    /// Returns the query results as a list of tuples of five objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <returns>A list of tuples of five objects.</returns>
    public virtual List<Tuple<T1, T2, T3, T4, T5>> ToList<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        this.Execute();
        return this.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5>()).ToList();
    }

    /// <summary>
    /// Asynchronously returns the query results as a list of tuples of five objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a list of tuples of five objects as the result.</returns>
    public virtual async Task<List<Tuple<T1, T2, T3, T4, T5>>> ToListAsync<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        await this.ExecuteAsync();
        return await this.AsEnumerableAsync().Select(row => row.GetTuple<T1, T2, T3, T4, T5>()).ToListAsync();
    }

    /// <summary>
    /// Returns the query results as a list of tuples of six objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <returns>A list of tuples of six objects.</returns>
    public virtual List<Tuple<T1, T2, T3, T4, T5, T6>> ToList<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        this.Execute();
        return this.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6>()).ToList();
    }

    /// <summary>
    /// Asynchronously returns the query results as a list of tuples of six objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a list of tuples of six objects as the result.</returns>
    public virtual async Task<List<Tuple<T1, T2, T3, T4, T5, T6>>> ToListAsync<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        await this.ExecuteAsync();
        return await this.AsEnumerableAsync().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6>()).ToListAsync();
    }

    /// <summary>
    /// Returns the query results as a list of tuples of seven objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <returns>A list of tuples of seven objects.</returns>
    public virtual List<Tuple<T1, T2, T3, T4, T5, T6, T7>> ToList<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        this.Execute();
        return this.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6, T7>()).ToList();
    }

    /// <summary>
    /// Asynchronously returns the query results as a list of tuples of seven objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a list of tuples of seven objects as the result.</returns>
    public virtual async Task<List<Tuple<T1, T2, T3, T4, T5, T6, T7>>> ToListAsync<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        await this.ExecuteAsync();
        return await this.AsEnumerableAsync().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6, T7>()).ToListAsync();
    }

    /// <summary>
    /// Returns the query results as a list of tuples of eight objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <typeparam name="T8">The type of the eighth tuple item.</typeparam>
    /// <returns>A list of tuples of eight objects.</returns>
    public virtual List<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> ToList<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        this.Execute();
        return this.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>()).ToList();
    }

    /// <summary>
    /// Asynchronously returns the query results as a list of tuples of eight objects.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <typeparam name="T8">The type of the eighth tuple item.</typeparam>
    /// <returns>A task representing the asynchronous operation, with a list of tuples of eight objects as the result.</returns>
    public virtual async Task<List<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>>> ToListAsync<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        await this.ExecuteAsync();
        return await this.AsEnumerableAsync().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>()).ToListAsync();
    }
    #endregion

    #region Tuple enumerators - Iterate

    /// <summary>
    /// Iterates over the query results as tuples of two objects using the specified key field.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <param name="byKeyField">The key field to iterate by.</param>
    /// <returns>An enumerable of tuples of two objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2>> Iterate<T1, T2>(string byKeyField) 
        where T1 : class
        where T2 : class 
    {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of two objects using the specified key field selector.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <returns>An enumerable of tuples of two objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2>> Iterate<T1, T2>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class 
    {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of three objects using the specified key field.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <param name="byKeyField">The key field to iterate by.</param>
    /// <returns>An enumerable of tuples of three objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3>> Iterate<T1, T2, T3>(string byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2, T3>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of three objects using the specified key field selector.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <returns>An enumerable of tuples of three objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3>> Iterate<T1, T2, T3>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2, T3>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of four objects using the specified key field.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <param name="byKeyField">The key field to iterate by.</param>
    /// <returns>An enumerable of tuples of four objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4>> Iterate<T1, T2, T3, T4>(string byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2, T3, T4>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of four objects using the specified key field selector.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <returns>An enumerable of tuples of four objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4>> Iterate<T1, T2, T3, T4>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2, T3, T4>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of five objects using the specified key field.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <param name="byKeyField">The key field to iterate by.</param>
    /// <returns>An enumerable of tuples of five objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5>> Iterate<T1, T2, T3, T4, T5>(string byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2, T3, T4, T5>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of five objects using the specified key field selector.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <returns>An enumerable of tuples of five objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5>> Iterate<T1, T2, T3, T4, T5>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2, T3, T4, T5>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of six objects using the specified key field.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <param name="byKeyField">The key field to iterate by.</param>
    /// <returns>An enumerable of tuples of six objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6>> Iterate<T1, T2, T3, T4, T5, T6>(string byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of six objects using the specified key field selector.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <returns>An enumerable of tuples of six objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6>> Iterate<T1, T2, T3, T4, T5, T6>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of seven objects using the specified key field.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <param name="byKeyField">The key field to iterate by.</param>
    /// <returns>An enumerable of tuples of seven objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7>> Iterate<T1, T2, T3, T4, T5, T6, T7>(string byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of seven objects using the specified key field selector.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <returns>An enumerable of tuples of seven objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7>> Iterate<T1, T2, T3, T4, T5, T6, T7>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of eight objects using the specified key field.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <typeparam name="T8">The type of the eighth tuple item.</typeparam>
    /// <param name="byKeyField">The key field to iterate by.</param>
    /// <returns>An enumerable of tuples of eight objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> Iterate<T1, T2, T3, T4, T5, T6, T7, T8>(string byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
        }
    }

    /// <summary>
    /// Iterates over the query results as tuples of eight objects using the specified key field selector.
    /// </summary>
    /// <typeparam name="T1">The type of the first tuple item.</typeparam>
    /// <typeparam name="T2">The type of the second tuple item.</typeparam>
    /// <typeparam name="T3">The type of the third tuple item.</typeparam>
    /// <typeparam name="T4">The type of the fourth tuple item.</typeparam>
    /// <typeparam name="T5">The type of the fifth tuple item.</typeparam>
    /// <typeparam name="T6">The type of the sixth tuple item.</typeparam>
    /// <typeparam name="T7">The type of the seventh tuple item.</typeparam>
    /// <typeparam name="T8">The type of the eighth tuple item.</typeparam>
    /// <param name="byKeyField">The key field selector to iterate by.</param>
    /// <returns>An enumerable of tuples of eight objects.</returns>
    public virtual IEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> Iterate<T1, T2, T3, T4, T5, T6, T7, T8>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
        }
    }
    #endregion
    #endregion

    #endregion

    #region Transaction management
    /// <summary>
    /// Begins a new database transaction with the specified name.
    /// </summary>
    /// <remarks>This method initializes a new transaction in the database context. Ensure that any ongoing
    /// transactions are properly committed or rolled back before starting a new one to avoid conflicts.</remarks>
    /// <param name="transactionName">The name of the transaction. If not specified, an unnamed transaction will be started.</param>
    /// <returns><see langword="true"/> if the transaction was successfully started; otherwise, <see langword="false"/>.</returns>
    public virtual bool Begin(string transactionName = "") {
        this.DBQuery.Reset();
        this._ResetResult();

        this.DBQuery.Raw(string.Format(this._TransactionBeginQuery, transactionName));
        bool result = this.Execute<bool>();

        this.DBQuery.Reset();
        this._ResetResult();

        _logger.LogDebug("Started transaction {transactionName}", transactionName);
        if (!Transactions.Contains(transactionName)) {
            Transactions.Add(transactionName);
        }

        return result;
    }

    /// <summary>
    /// Commits the current transaction to the database.
    /// </summary>
    /// <remarks>This method finalizes the transaction and applies all changes made during the transaction to
    /// the database. Ensure that the transaction is properly initialized before calling this method.</remarks>
    /// <param name="transactionName">The name of the transaction to commit. If not specified, the default transaction is committed.</param>
    /// <returns><see langword="true"/> if the transaction was successfully committed; otherwise, <see langword="false"/>.</returns>
    public virtual bool Commit(string transactionName = "") {
        this.DBQuery.Reset();
        this._ResetResult();

        this.DBQuery.Raw(string.Format(this._TransactionCommitQuery, transactionName));
        bool result = this.Execute<bool>();

        this.DBQuery.Reset();
        this._ResetResult();

        _logger.LogDebug("Commited transaction {transactionName}", transactionName);
        if (Transactions.Contains(transactionName)) {
            Transactions.Remove(transactionName);
        }

        return result;
    }

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    /// <remarks>Use this method to undo changes made during a transaction. Ensure that the transaction name,
    /// if provided, matches the name of an active transaction. If no transaction name is specified, the method attempts
    /// to roll back the default transaction.</remarks>
    /// <param name="transactionName">The name of the transaction to roll back. If not specified, the default transaction is rolled back.</param>
    /// <returns><see langword="true"/> if the rollback operation succeeds; otherwise, <see langword="false"/>.</returns>
    public virtual bool Rollback(string transactionName = "") {
        this.DBQuery.Reset();
        this._ResetResult();

        this.DBQuery.Raw(string.Format(this._TransactionRollbackQuery, transactionName));
        bool result = this.Execute<bool>();

        this.DBQuery.Reset();
        this._ResetResult();

        _logger.LogDebug("Rolled back transaction {transactionName}", transactionName);
        if (Transactions.Contains(transactionName)) {
            Transactions.Remove(transactionName);
        }

        return result;
    }

    /// <summary>
    /// Alias for <see cref="Begin"/>. Begins a new database transaction.
    /// </summary>
    /// <param name="transactionName">The name of the transaction. If not specified, an unnamed transaction will be started.</param>
    /// <returns><see langword="true"/> if the transaction was successfully started; otherwise, <see langword="false"/>.</returns>
    public bool BeginTransaction(string transactionName = "") {
        return this.Begin(transactionName);
    }

    /// <summary>
    /// Alias for <see cref="Commit"/>. Commits the current database transaction.
    /// </summary>
    /// <param name="transactionName">The name of the transaction to commit. If not specified, the default transaction is committed.</param>
    /// <returns><see langword="true"/> if the transaction was successfully committed; otherwise, <see langword="false"/>.</returns>
    public bool CommitTransaction(string transactionName = "") {
        return this.Commit(transactionName);
    }

    /// <summary>
    /// Alias for <see cref="Rollback"/>. Rolls back the current database transaction.
    /// </summary>
    /// <param name="transactionName">The name of the transaction to roll back. If not specified, the default transaction is rolled back.</param>
    /// <returns><see langword="true"/> if the rollback operation succeeds; otherwise, <see langword="false"/>.</returns>
    public bool RollbackTransaction(string transactionName = "") {
        return this.Rollback(transactionName);
    }
    #endregion
}
