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

namespace Unleasharp.DB.Base {
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

        public QueryBuilder(DBConnectorType Connector) {
            this.Connector = Connector;

            DBQuery = Activator.CreateInstance<DBQueryType>();
        }

        public QueryBuilder(DBConnectorType Connector, DBQueryType Query) {
            this.Connector = Connector;

            DBQuery = Query;
        }

        public QueryBuilderType Build(Action<DBQueryType> Action) {
            Action.Invoke(this.DBQuery);

            return (QueryBuilderType) this;
        }

        #region Query execution
        protected void _ResetResult() {
            this.Result       = null;
            this.AffectedRows = 0;
            this.TotalCount   = 0;
        }

        #region Query execution aliases
        public virtual QueryBuilderType Execute(bool Force = false) {
            // Don't execute the query twice
            if (this.Result == null || Force) {
                this._Execute();
            }

            return (QueryBuilderType) this;
        }

        public virtual async Task<QueryBuilderType> ExecuteAsync(bool Force = false) {
            // Don't execute the query twice
            if (this.Result == null || Force) {
                await this._ExecuteAsync();
            }

            return (QueryBuilderType)this;
        }
        #endregion

        protected virtual bool _Execute() {
            throw new NotImplementedException();
        }

        protected virtual async Task<bool> _ExecuteAsync() {
            throw new NotImplementedException();
        }
        #endregion

        #region Data iteration
        public virtual int Pages() {
            int Total = this.Count();
            if (this.DBQuery.QueryLimit != null && this.DBQuery.QueryLimit.Count > 0) {
                return (int) Math.Ceiling(Decimal.Divide(this.DBQuery.QueryLimit.Count, Total));
            }

            return 0;
        }

        public virtual int Count() {
            // Set the QueryType to COUNT but store the current type to restore it later
            QueryType CurrentQueryType = this.DBQuery.QueryType;
            this.DBQuery.SetQueryType(QueryBuilding.QueryType.COUNT);

            // Forcefully execute the query to retrieve the count
            this._Execute();

            // Set the query type back
            this.DBQuery.SetQueryType(CurrentQueryType);

            return this.TotalCount;
        }

        public virtual IEnumerable<T> Iterate<T>(string ByKeyField) where T : class {
            foreach (DataRow Row in this.Iterate(new FieldSelector(ByKeyField))) {
                yield return Row.GetObject<T>();
            }
        }

        public virtual IEnumerable<T> Iterate<T>(FieldSelector ByKeyField) where T : class {
            foreach (DataRow Row in this.Iterate(ByKeyField)) {
                yield return Row.GetObject<T>();
            }
        }

        public virtual IEnumerable<DataRow> Iterate(FieldSelector ByKeyField) {
            ulong LastId   = 0;
            bool  EndFound = false;

            DBQueryType OriginalQuery = this.DBQuery.DeepCopy();

            while (!EndFound) {
                this._ResetResult();
                DBQueryType CachedQuery = OriginalQuery.DeepCopy();

                CachedQuery.Where(new Where<DBQueryType> {
                    Field    = ByKeyField,
                    Operator = WhereOperator.AND,
                    Comparer = WhereComparer.GREATER,
                    Value    = LastId
                });
                this.DBQuery = CachedQuery;

                IEnumerable<DataRow> ResultEnumerator = this.AsEnumerable();
                if (!ResultEnumerator.Any()) {
                    EndFound = true;
                }

                foreach (DataRow Row in ResultEnumerator) {
                    try {
                        if (Row[ByKeyField.Field].TryConvert<ulong>(out ulong LatestRowId)) { 
                            if (LatestRowId > LastId) {
                                LastId = LatestRowId;
                            }
                        }
                        else {
                            EndFound = true;
                            break;
                        }
                    }
                    catch (Exception ex) {
                        EndFound = true;
                    }

                    yield return Row;
                }
            }

            this.DBQuery = OriginalQuery;
        }

        public virtual IEnumerable<DataRow> AsEnumerable() {
            this.Execute();

            foreach (DataRow Row in this.Result?.Rows) {
                yield return Row;
            }
        }

        public virtual IEnumerable<T> AsEnumerable<T>() where T : class {
            this.Execute();

            foreach (DataRow Row in this.AsEnumerable()) {
                yield return Row.GetObject<T>();
            }
        }

        public virtual async IAsyncEnumerable<DataRow> AsEnumerableAsync() {
            await this.ExecuteAsync();

            foreach (DataRow Row in this.Result?.Rows) {
                yield return Row;
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

            return this.Result.AsEnumerable().Select(row => row.GetObject<T>()).ToList();
        }

        public virtual async Task<List<T>> ToListAsync<T>() where T : class{
            await this.ExecuteAsync();

            return this.Result.AsEnumerable().Select(row => row.GetObject<T>()).ToList();
        }

        public virtual DataRow FirstOrDefault() {
            this.Execute();

            foreach (DataRow Row in this.AsEnumerable()) {
                return Row;
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
}
