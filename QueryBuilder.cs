using Baksteen.Extensions.DeepCopy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unleasharp.DB.Base.ExtensionMethods;
using Unleasharp.DB.Base.QueryBuilding;
using Unleasharp.ExtensionMethods;

namespace Unleasharp.DB.Base;

public class QueryBuilder<QueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType>
    where DBConnectionType        : DbConnection
    where DBConnectorSettingsType : DbConnectionStringBuilder
    where DBConnectorType         : Connector<DBConnectorType, DBConnectorSettingsType>
    where DBQueryType             : Query<DBQueryType>
    where QueryBuilderType        : QueryBuilder<QueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType>
{
    public DBConnectorType Connector    { get; private set;   }
    public DBQueryType     DBQuery      { get; private set;   }
                                        
    public DataTable       Result       { get; protected set; } = null;
    public int             AffectedRows { get; protected set; } = 0;
    public int             TotalCount   { get; protected set; } = 0;
    public object          ScalarValue  { get; protected set; } = null;

    public Action<DBQueryType> BeforeQueryExecutionAction { get; protected set; }
    public Action<DBQueryType> AfterQueryExecutionAction  { get; protected set; }
    public Action<Exception>   OnQueryExceptionAction     { get; private   set; }

    public QueryBuilder(DBConnectorType connector) {
        this.Connector = connector;

            this.DBQuery = Activator.CreateInstance<DBQueryType>();
    }

    public QueryBuilder(DBConnectorType connector, DBQueryType query) {
        this.Connector = connector;

            this.DBQuery = query;
    }

    public QueryBuilderType WithOnQueryExceptionAction(Action<Exception> onQueryExceptionAction) {
        this.OnQueryExceptionAction = onQueryExceptionAction;

        return (QueryBuilderType)this;
    }

    public QueryBuilderType WithBeforeQueryExecutionAction(Action<DBQueryType> beforeQueryExecutionAction) {
        this.BeforeQueryExecutionAction = beforeQueryExecutionAction;

        return (QueryBuilderType)this;
    }

    public QueryBuilderType WithAfterQueryExecutionAction(Action<DBQueryType> afterQueryExecutionAction) {
        this.AfterQueryExecutionAction = afterQueryExecutionAction;

        return (QueryBuilderType)this;
    }

    protected void _OnQueryException(Exception ex) {
        if (this.OnQueryExceptionAction != null) {
            this.OnQueryExceptionAction.Invoke(ex);
        }
    }

    protected void _BeforeQueryExecution() {
        this._ResetResult();
        if (this.BeforeQueryExecutionAction != null) {
            this.BeforeQueryExecutionAction.Invoke(this.DBQuery);
        }
    }

    protected void _AfterQueryExecution() {
        if (this.Result != null && this.Result.Rows.Count > 0 && this.Result.Columns.Count > 0) {
            this.ScalarValue = this.Result.Rows[0][0];
        }

        if (this.AfterQueryExecutionAction != null) {
            this.AfterQueryExecutionAction.Invoke(this.DBQuery);
        }
    }

    public QueryBuilderType Build(Action<DBQueryType> action) {
        action.Invoke(this.DBQuery);

        return (QueryBuilderType) this;
    }

    #region Query execution
    protected void _ResetResult() {
        this.Result       = null;
        this.AffectedRows = 0;
        this.TotalCount   = 0;
        this.ScalarValue  = null;
    }

    #region Query execution aliases
    public virtual QueryBuilderType Execute(bool force = false) {
        // Don't execute the query twice
        if (this.Result == null || force) {
            this._BeforeQueryExecution();
            this._Execute();
            this._AfterQueryExecution();
        }

        return (QueryBuilderType) this;
    }

    public virtual T ExecuteScalar<T>(bool force = false) {
        // Don't execute the query twice
        if (this.Result == null || force) {
            this._BeforeQueryExecution();
            T scalarValue = this._ExecuteScalar<T>();
            this._AfterQueryExecution();

            this.ScalarValue = scalarValue;
            return scalarValue;
        }

        return default(T);
    }

    public virtual async Task<QueryBuilderType> ExecuteAsync(bool force = false) {
        // Don't execute the query twice
        if (this.Result == null || force) {
            this._BeforeQueryExecution();
            await this._ExecuteAsync();
            this._AfterQueryExecution();
        }

        return (QueryBuilderType)this;
    }
    #endregion

    protected virtual bool _Execute() {
        throw new NotImplementedException();
    }

    protected virtual T _ExecuteScalar<T>() {
        throw new NotImplementedException();
    }

    protected virtual async Task<bool> _ExecuteAsync() {
        throw new NotImplementedException();
    }
    #endregion

    #region Data iteration
    public virtual int Pages() {
        int total = this.Count();
        if (this.DBQuery.QueryLimit != null && this.DBQuery.QueryLimit.Count > 0) {
            return (int) Math.Ceiling(Decimal.Divide(this.DBQuery.QueryLimit.Count, total));
        }

        return 0;
    }

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

        return this.TotalCount;
    }

    public virtual IEnumerable<T> Iterate<T>(string byKeyField) where T : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetObject<T>();
        }
    }

    public virtual IEnumerable<T> Iterate<T>(FieldSelector byKeyField) where T : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetObject<T>();
        }
    }

    public virtual IEnumerable<DataRow> Iterate(FieldSelector byKeyField) {
        ulong lastId   = 0;
        bool  endFound = false;

        DBQueryType originalQuery = this.DBQuery.DeepCopy();

        while (!endFound) {
            this._ResetResult();
            DBQueryType cachedQuery = originalQuery.DeepCopy();

            cachedQuery.Where(new Where<DBQueryType> {
                Field    = byKeyField,
                Operator = WhereOperator.AND,
                Comparer = WhereComparer.GREATER,
                Value    = lastId
            });
            this.DBQuery = cachedQuery;

            IEnumerable<DataRow> resultEnumerator = this.AsEnumerable();
            if (resultEnumerator == null || !resultEnumerator.Any()) {
                endFound = true;
                break;
            }

            foreach (DataRow row in resultEnumerator) {
                try {
                    if (row[byKeyField.Field].TryConvert<ulong>(out ulong latestRowId)) { 
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

    public virtual IEnumerable<DataRow> AsEnumerable() {
        this.Execute();

        foreach (DataRow row in this.Result?.Rows) {
            yield return row;
        }
    }

    public virtual IEnumerable<T> AsEnumerable<T>() where T : class {
        this.Execute();

        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetObject<T>();
        }
    }

    public virtual async IAsyncEnumerable<DataRow> AsEnumerableAsync() {
        await this.ExecuteAsync();

        foreach (DataRow row in this.Result?.Rows) {
            yield return row;
        }
    }

    public virtual async IAsyncEnumerable<T> AsEnumerableAsync<T>() where T : class {
        await this.ExecuteAsync();

        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetObject<T>();
        }
    }

    public virtual List<T> ToList<T>() where T : class{
        this.Execute();

        return this.Result?.AsEnumerable().Select(row => row.GetObject<T>()).ToList();
    }

    public virtual async Task<List<T>> ToListAsync<T>() where T : class{
        await this.ExecuteAsync();

        return this.Result?.AsEnumerable().Select(row => row.GetObject<T>()).ToList();
    }

    public virtual DataRow FirstOrDefault() {
        this.Execute();

        foreach (DataRow row in this.AsEnumerable()) {
            return row;
        }

        return null;
    }

    public virtual T FirstOrDefault<T>() where T : class {
        this.Execute();

        return this.FirstOrDefault()?.GetObject<T>();
    }

    public virtual async Task<DataRow> FirstOrDefaultAsync() {
        await this.ExecuteAsync();

        return this.FirstOrDefault();
    }

    public virtual async Task<T> FirstOrDefaultAsync<T>() where T : class {
        await this.ExecuteAsync();

        return this.FirstOrDefault<T>();
    }
    #endregion
}
