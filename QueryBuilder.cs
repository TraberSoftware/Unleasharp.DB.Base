using Baksteen.Extensions.DeepCopy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unleasharp.DB.Base.ExtensionMethods;
using Unleasharp.DB.Base.QueryBuilding;
using Unleasharp.DB.Base.SchemaDefinition;
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

    public Action<DBQueryType>            BeforeQueryExecutionAction { get; protected set; }
    public Action<DBQueryType>            AfterQueryExecutionAction  { get; protected set; }
    public Action<DBQueryType, Exception> OnQueryExceptionAction     { get; private   set; }

    public QueryBuilder(DBConnectorType connector) {
        this.Connector = connector;

            this.DBQuery = Activator.CreateInstance<DBQueryType>();
    }

    public QueryBuilder(DBConnectorType connector, DBQueryType query) {
        this.Connector = connector;

            this.DBQuery = query;
    }

    public QueryBuilderType WithOnQueryExceptionAction(Action<DBQueryType, Exception> onQueryExceptionAction) {
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
            this.OnQueryExceptionAction.Invoke(this.DBQuery, ex);
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

    #region Data iteration - Iterate
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
    #endregion

    #region Data iteration - AsEnumerable
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
    #endregion

    #region Data iteration - ToList
    public virtual List<T> ToList<T>() where T : class{
        this.Execute();

        return this.Result?.AsEnumerable().Select(row => row.GetObject<T>()).ToList();
    }

    public virtual async Task<List<T>> ToListAsync<T>() where T : class{
        await this.ExecuteAsync();

        return this.Result?.AsEnumerable().Select(row => row.GetObject<T>()).ToList();
    }
    #endregion

    #region Data Iteration - FirstOrDefault
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

    #region Tuple enumerators
    #region Tuple enumerators - FirstOrDefault
    public virtual Tuple<T1, T2> FirstOrDefault<T1, T2>()
        where T1 : class
        where T2 : class {
        this.Execute();
        return this.FirstOrDefault()?.GetTuple<T1, T2>();
    }

    public virtual async Task<Tuple<T1, T2>> FirstOrDefaultAsync<T1, T2>()
        where T1 : class
        where T2 : class {
        await this.ExecuteAsync();
        return this.FirstOrDefault<T1, T2>();
    }

    public virtual Tuple<T1, T2, T3> FirstOrDefault<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class {
        this.Execute();
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3>();
    }

    public virtual async Task<Tuple<T1, T2, T3>> FirstOrDefaultAsync<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class {
        await this.ExecuteAsync();
        return this.FirstOrDefault<T1, T2, T3>();
    }

    public virtual Tuple<T1, T2, T3, T4> FirstOrDefault<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        this.Execute();
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4>();
    }

    public virtual async Task<Tuple<T1, T2, T3, T4>> FirstOrDefaultAsync<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        await this.ExecuteAsync();
        return this.FirstOrDefault<T1, T2, T3, T4>();
    }

    public virtual Tuple<T1, T2, T3, T4, T5> FirstOrDefault<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        this.Execute();
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4, T5>();
    }

    public virtual async Task<Tuple<T1, T2, T3, T4, T5>> FirstOrDefaultAsync<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        await this.ExecuteAsync();
        return this.FirstOrDefault<T1, T2, T3, T4, T5>();
    }

    public virtual Tuple<T1, T2, T3, T4, T5, T6> FirstOrDefault<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        this.Execute();
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4, T5, T6>();
    }

    public virtual async Task<Tuple<T1, T2, T3, T4, T5, T6>> FirstOrDefaultAsync<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        await this.ExecuteAsync();
        return this.FirstOrDefault<T1, T2, T3, T4, T5, T6>();
    }

    public virtual Tuple<T1, T2, T3, T4, T5, T6, T7> FirstOrDefault<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        this.Execute();
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4, T5, T6, T7>();
    }

    public virtual async Task<Tuple<T1, T2, T3, T4, T5, T6, T7>> FirstOrDefaultAsync<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        await this.ExecuteAsync();
        return this.FirstOrDefault<T1, T2, T3, T4, T5, T6, T7>();
    }

    public virtual Tuple<T1, T2, T3, T4, T5, T6, T7, T8> FirstOrDefault<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        this.Execute();
        return this.FirstOrDefault()?.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
    }

    public virtual async Task<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> FirstOrDefaultAsync<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        await this.ExecuteAsync();
        return this.FirstOrDefault<T1, T2, T3, T4, T5, T6, T7, T8>();
    }
    #endregion

    #region Tuple enumerators - AsEnumerable
    public virtual IEnumerable<Tuple<T1, T2>> AsEnumerable<T1, T2>()
        where T1 : class 
        where T2 : class 
    {
        this.Execute();

        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2>();
        }
    }

    public virtual async IAsyncEnumerable<Tuple<T1, T2>> AsEnumerableAsync<T1, T2>()
        where T1 : class
        where T2 : class 
    {
        await this.ExecuteAsync();

        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2>();
        }
    }

    public virtual IEnumerable<Tuple<T1, T2, T3>> AsEnumerable<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class {
        this.Execute();
        foreach (DataRow row in this.AsEnumerable()) {
            yield return row.GetTuple<T1, T2, T3>();
        }
    }

    public virtual async IAsyncEnumerable<Tuple<T1, T2, T3>> AsEnumerableAsync<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class {
        await this.ExecuteAsync();
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2, T3>();
        }
    }

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

    public virtual async IAsyncEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> AsEnumerableAsync<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class
        where T8 : class {
        await this.ExecuteAsync();
        await foreach (DataRow row in this.AsEnumerableAsync()) {
            yield return row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>();
        }
    }
    #endregion

    #region Tuple enumerators - ToList
    public virtual List<Tuple<T1, T2>> ToList<T1, T2>()
        where T1 : class
        where T2 : class 
    {
        this.Execute();

        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2>()).ToList();
    }

    public virtual async Task<List<Tuple<T1, T2>>> ToListAsync<T1, T2>()
        where T1 : class
        where T2 : class {
        await this.ExecuteAsync();

        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2>()).ToList();
    }

    public virtual List<Tuple<T1, T2, T3>> ToList<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class {
        this.Execute();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3>()).ToList();
    }

    public virtual async Task<List<Tuple<T1, T2, T3>>> ToListAsync<T1, T2, T3>()
        where T1 : class
        where T2 : class
        where T3 : class {
        await this.ExecuteAsync();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3>()).ToList();
    }

    public virtual List<Tuple<T1, T2, T3, T4>> ToList<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        this.Execute();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4>()).ToList();
    }

    public virtual async Task<List<Tuple<T1, T2, T3, T4>>> ToListAsync<T1, T2, T3, T4>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        await this.ExecuteAsync();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4>()).ToList();
    }

    public virtual List<Tuple<T1, T2, T3, T4, T5>> ToList<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        this.Execute();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5>()).ToList();
    }

    public virtual async Task<List<Tuple<T1, T2, T3, T4, T5>>> ToListAsync<T1, T2, T3, T4, T5>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class {
        await this.ExecuteAsync();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5>()).ToList();
    }

    public virtual List<Tuple<T1, T2, T3, T4, T5, T6>> ToList<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        this.Execute();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6>()).ToList();
    }

    public virtual async Task<List<Tuple<T1, T2, T3, T4, T5, T6>>> ToListAsync<T1, T2, T3, T4, T5, T6>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class {
        await this.ExecuteAsync();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6>()).ToList();
    }

    public virtual List<Tuple<T1, T2, T3, T4, T5, T6, T7>> ToList<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        this.Execute();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6, T7>()).ToList();
    }

    public virtual async Task<List<Tuple<T1, T2, T3, T4, T5, T6, T7>>> ToListAsync<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
        where T5 : class
        where T6 : class
        where T7 : class {
        await this.ExecuteAsync();
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6, T7>()).ToList();
    }

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
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>()).ToList();
    }

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
        return this.Result?.AsEnumerable().Select(row => row.GetTuple<T1, T2, T3, T4, T5, T6, T7, T8>()).ToList();
    }
    #endregion

    #region Tuple enumerators - Iterate

    public virtual IEnumerable<Tuple<T1, T2>> Iterate<T1, T2>(string byKeyField) 
        where T1 : class
        where T2 : class 
    {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2>();
        }
    }

    public virtual IEnumerable<Tuple<T1, T2>> Iterate<T1, T2>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class 
    {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2>();
        }
    }

    public virtual IEnumerable<Tuple<T1, T2, T3>> Iterate<T1, T2, T3>(string byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2, T3>();
        }
    }

    public virtual IEnumerable<Tuple<T1, T2, T3>> Iterate<T1, T2, T3>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2, T3>();
        }
    }

    public virtual IEnumerable<Tuple<T1, T2, T3, T4>> Iterate<T1, T2, T3, T4>(string byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        foreach (DataRow row in this.Iterate(new FieldSelector(byKeyField))) {
            yield return row.GetTuple<T1, T2, T3, T4>();
        }
    }

    public virtual IEnumerable<Tuple<T1, T2, T3, T4>> Iterate<T1, T2, T3, T4>(FieldSelector byKeyField)
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class {
        foreach (DataRow row in this.Iterate(byKeyField)) {
            yield return row.GetTuple<T1, T2, T3, T4>();
        }
    }

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
}
