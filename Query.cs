using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Unleasharp.DB.Base.ExtensionMethods;
using Unleasharp.DB.Base.QueryBuilding;
using Unleasharp.DB.Base.SchemaDefinition;
using Unleasharp.ExtensionMethods;

namespace Unleasharp.DB.Base;

public class Query<DBQueryType> : Renderable
    where DBQueryType : Query<DBQueryType> 
{
    protected virtual DatabaseEngine _Engine { get; }

    #region Syntax sugar
    public virtual T GetThis<T>() where T : Query<DBQueryType> {
        return (T) this;
    }

    public static DBQueryType GetInstance() {
        return Activator.CreateInstance<DBQueryType>();
    }
    #endregion

    #region Query parameters
    public List<Select <DBQueryType>>        QuerySelect  { get; protected set; } = new List<Select <DBQueryType>>();
    public List<From   <DBQueryType>>        QueryFrom    { get; protected set; } = new List<From   <DBQueryType>>();
    public List<Join   <DBQueryType>>        QueryJoin    { get; protected set; } = new List<Join   <DBQueryType>>();
    public List<Where  <DBQueryType>>        QueryWhere   { get; protected set; } = new List<Where  <DBQueryType>>();
    public List<WhereIn<DBQueryType>>        QueryWhereIn { get; protected set; } = new List<WhereIn<DBQueryType>>();
    public List<Where  <DBQueryType>>        QueryHaving  { get; protected set; } = new List<Where  <DBQueryType>>();
    public List<Where  <DBQueryType>>        QuerySet     { get; protected set; } = new List<Where  <DBQueryType>>();
    public List<Dictionary<string, dynamic>> QueryValues  { get; protected set; } = new List<Dictionary<string, dynamic>>();
    public List<string>                      QueryColumns { get; protected set; } = new List<string>                     ();
    public FieldSelector                     QueryInto    { get; protected set; }
    public Type                              QueryCreate  { get; protected set; }
    public List<GroupBy>                     QueryGroup   { get; protected set; } = new List<GroupBy>();
    public List<OrderBy>                     QueryOrder   { get; protected set; } = new List<OrderBy>();
    public Limit                             QueryLimit   { get; protected set; }
    public QueryType                         QueryType    { get; protected set; } = QueryType.RAW;

    /// <summary>
    /// Raw query string rendered as reference but not for real usage
    /// As prepared queries are not rendered as-is, this is intended to be used
    /// as reference as the translated query that will be executed, but could
    /// be different of the real result.
    /// </summary>
    public string                            QueryRenderedString { get; protected set; }
    /// <summary>
    /// Pre-rendered data values for query rendering
    /// </summary>
    public Dictionary<string, string>        QueryRenderedData   { get; protected set; } = new Dictionary<string, string>();

    /// <summary>
    /// Query string with data placeholders for query preparation
    /// </summary>
    public string                            QueryPreparedString { get; protected set; }
    /// <summary>
    /// Data values for query preparation
    /// </summary>
    public Dictionary<string, PreparedValue> QueryPreparedData   { get; protected set; } = new Dictionary<string, PreparedValue>();

    public Query<DBQueryType> ParentQuery { get; protected set; }
    #region Constructor sugar

    public Query() {
        this.ParentQuery = this;
    }

    public Query(Query<DBQueryType> parentQuery) {
        this.ParentQuery = parentQuery;
    }

    public Query<DBQueryType> WithParentQuery(Query<DBQueryType> parentQuery) {
        this.ParentQuery = parentQuery;

        return (DBQueryType) this;
    }

    public virtual string PrepareQueryValue(dynamic queryValue, bool escape) {
        Query<DBQueryType> targetQuery = this.ParentQuery != null ? this.ParentQuery : this;
        string             label       = this.GetNextPreparedQueryValueLabel();

        targetQuery.QueryPreparedData.Add(label, new PreparedValue {
            Value       = queryValue,
            EscapeValue = escape
        });

        return label;
    }

    public virtual string GetNextPreparedQueryValueLabel() {
        Query<DBQueryType> targetQuery = this.ParentQuery != null ? this.ParentQuery : this;
        string             prefix      = "@prepared_query_value_";

        return $"{prefix}{targetQuery.QueryPreparedData.Count}"; ;
    }
    #endregion

    public virtual DBQueryType Reset() {
        this.QueryFrom   = new List<From  <DBQueryType>>();
        this.QuerySelect = new List<Select<DBQueryType>>();
        this.QueryJoin   = new List<Join  <DBQueryType>>();
        this.QueryWhere  = new List<Where <DBQueryType>>();
        this.QueryHaving = new List<Where <DBQueryType>>();
        this.QuerySet    = new List<Where <DBQueryType>>();
        this.QueryValues = new List<Dictionary<string, dynamic>>();
        this.QueryInto   = null;
        this.QueryGroup  = new List<GroupBy>();
        this.QueryOrder  = new List<OrderBy>();
        this.QueryLimit  = null;
        this.QueryType   = QueryType.RAW;

        this.ResetPreparedData();

        return (DBQueryType) this;
    }

    public virtual DBQueryType ResetPreparedData() {
        this.QueryRenderedString = null;
        this.QueryPreparedString = null;
        this.QueryRenderedData   = new Dictionary<string, string>();
        this.QueryPreparedData   = new Dictionary<string, PreparedValue>();

        return (DBQueryType)this;
    }
    #endregion

    #region Control parameters
    public bool HasChanged { get; private set; } = false;

    public void Touch() {
        this.HasChanged = true;
    }

    public void Untouch() {
        this.HasChanged = false;
    }
    #endregion

    #region Query rendering
    public virtual string RenderPrepared() {
        this._RenderPrepared();
        this.HasChanged = false;

        return this.QueryRenderedString;
    }

    public virtual void _RenderPrepared() {
        throw new NotImplementedException();
    }

    public virtual string Render() {
        this._Render();
        this.HasChanged = false;

        return this.QueryPreparedString;
    }

    public virtual void _Render() {
        if (this.HasChanged) {
            this.ResetPreparedData();
        }

        if (string.IsNullOrWhiteSpace(this.QueryPreparedString) || this.HasChanged) {
            switch (this.QueryType) {
                case QueryType.COUNT:
                    this._RenderCountQuery();
                    break;
                case QueryType.SELECT:
                    this._RenderSelectQuery();
                    break;
                case QueryType.INSERT:
                    this._RenderInsertQuery();
                    break;
                case QueryType.UPDATE:
                    this._RenderUpdateQuery();
                    break;
                case QueryType.DELETE:
                    this._RenderDeleteQuery();
                    break;
                case QueryType.CREATE:
                case QueryType.CREATE_TABLE:
                    this._RenderCreateQuery();
                    break;
                case QueryType.RAW:
                    break;
            }
        }
    }

    protected virtual void _RenderCountQuery() {
        List<string> queryGroups = new List<string> {
            _RenderCountSentence      (),
            _RenderFromSentence       (),
            _RenderJoinSentence       (),
            _RenderWhereSentence      (),
            _RenderGroupSentence      (),
            _RenderHavingSentence     (),
            _RenderSelectExtraSentence()
        };

        QueryPreparedString = string.Join(" ", queryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }


    protected virtual void _RenderSelectQuery() {
        List<string> queryGroups = new List<string> {
            _RenderSelectSentence     (),
            _RenderFromSentence       (),
            _RenderJoinSentence       (),
            _RenderWhereSentence      (),
            _RenderGroupSentence      (),
            _RenderHavingSentence     (),
            _RenderOrderSentence      (),
            _RenderLimitSentence      (),
            _RenderSelectExtraSentence()
        };

        QueryPreparedString = string.Join(" ", queryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    protected virtual void _RenderInsertQuery() {
        List<string> queryGroups = new List<string> {
            _RenderInsertIntoSentence  (),
            _RenderInsertValuesSentence(),
        };

        QueryPreparedString = string.Join(" ", queryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    protected virtual void _RenderUpdateQuery() {
        List<string> queryGroups = new List<string> {
            _RenderUpdateSentence     (),
            _RenderSetSentence        (),
            _RenderWhereSentence      (),
            _RenderOrderSentence      (),
            _RenderLimitSentence      ()
        };

        QueryPreparedString = string.Join(" ", queryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    protected virtual void _RenderDeleteQuery() {
        List<string> queryGroups = new List<string> {
            _RenderDeleteSentence(),
            _RenderWhereSentence (),
            _RenderOrderSentence (),
            _RenderLimitSentence ()
        };

        QueryPreparedString = string.Join(" ", queryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    protected virtual void _RenderCreateQuery() {
        QueryPreparedString = _RenderCreateSentence(this.QueryCreate);
    }
    #endregion

    #region Public query building methods
    public virtual DBQueryType SetQueryType(QueryType queryType) {
        this.QueryType = queryType;

        return (DBQueryType) this;
    }

    #region Query Building - Operations
    public virtual DBQueryType Select() {
        this.SetQueryType(QueryType.SELECT);

        return (DBQueryType)this;
    }

    public virtual DBQueryType Update() {
        this.SetQueryType(QueryType.UPDATE);

        return (DBQueryType)this;
    }

    public virtual DBQueryType Delete() {
        this.SetQueryType(QueryType.DELETE);

        return (DBQueryType)this;
    }

    public virtual DBQueryType Create() {
        this.SetQueryType(QueryType.CREATE);

        return (DBQueryType)this;
    }

    public virtual DBQueryType Insert() {
        this.SetQueryType(QueryType.INSERT);

        return (DBQueryType)this;
    }
    #endregion

    #region Query building - Select
    public virtual DBQueryType Select(Select<DBQueryType> select) {
        this.SetQueryType(QueryType.SELECT);

        this.QuerySelect.Add(select);

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType Select(FieldSelector field) {
        return this.Select(new Select<DBQueryType> {
            Field = field
        });
    }

    public virtual DBQueryType Select<T>() where T : class{
        Table table = typeof(T).GetCustomAttribute<Table>();
        if (table != null) {
            string tableName = typeof(T).GetTableName();

            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties()) {
                if (!propertyInfo.IsReadableSystemColumn(this._Engine)) {
                    continue;
                }

                string columnName = propertyInfo.Name;
                Column column     = propertyInfo.GetCustomAttribute<Column>();
                if (column != null) {
                    columnName = column.Name;
                }
                this.Select(new FieldSelector {
                    Table  = tableName, 
                    Field  = columnName,
                    Escape = true
                });
            }
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType Select<T>(Expression<Func<T, object>> expression) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            MemberInfo? columnMember = typeof(T).GetMember(columnName)?.FirstOrDefault();
            if (columnMember != null && columnMember.IsReadableSystemColumn(this._Engine)) {
                return this.Select(new FieldSelector {
                    Table = tableName,
                    Field = columnName
                });
            }
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType Select(Query<DBQueryType> subquery, string alias = null) {
        return this.Select(new Select<DBQueryType> {
            Subquery = subquery,
            Alias    = alias
        });
    }

    public virtual DBQueryType Select(string fieldName, bool escape = true) {
        return this.Select(new Select<DBQueryType> {
            Field = new FieldSelector(fieldName, escape)
        });
    }

    public virtual DBQueryType Select(List<string> fieldNames, bool escape = true) {
        foreach (string fieldName in fieldNames) {
            this.Select(fieldName, escape);
        }

        return (DBQueryType) this;
    }
    #endregion

    #region Query building - Set
    public virtual DBQueryType Set(Where<DBQueryType> setValue) {
        this.QuerySet.Add(setValue);

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType Set(List<Where<DBQueryType>> setValues) {
        foreach (Where<DBQueryType> setValue in setValues) {
            this.Set(setValue);
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType Set(string fieldName, dynamic value, bool escape = true) {
        return this.Set(new Where<DBQueryType> {
            Field       = new FieldSelector(fieldName),
            Value       = value,
            EscapeValue = escape
        });
    }

    public virtual DBQueryType Set(Dictionary<string, dynamic> row, bool escape = true) {
        foreach (KeyValuePair<string, dynamic> entry in row) {
            this.Set(entry.Key, entry.Value, escape);
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType Set<T>(Expression<Func<T, object>> expression, dynamic value, bool escape = true) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            MemberInfo? columnMember = typeof(T).GetMember(columnName)?.FirstOrDefault();
            if (columnMember != null && !columnMember.IsSystemColumn()) {
                return this.Set(
                    new FieldSelector(tableName, columnName, true),
                    value,
                    escape
                );
            }
        }
        return (DBQueryType)this;
    }
    #endregion

    #region Query building - Values
    public virtual DBQueryType Value(Dictionary<string, dynamic> row, bool skipNullValues = false) {
        foreach (string column in row.Keys) {
            if (!this.QueryColumns.Contains(column)) {
                this.QueryColumns.Add(column);
            }
        }
        this.QueryValues.Add(row);

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType Value<T>(T row, bool skipNullValues = false) where T : class {
        return this.Value(row.ToDynamicDictionary());
    }

    public virtual DBQueryType Value<T>(Action<T> action) where T : class {
        T row = Activator.CreateInstance<T>();
        if (action != null) {
            action.Invoke(row);
        }

        return this.Value<T>(row);
    }


    public virtual DBQueryType Value(object row, bool skipNullValues = false) {
        return this.Value(row.ToDynamicDictionary());
    }

    public virtual DBQueryType Values<T>(List<T> rows, bool skipNullValues = false) where T : class {
        foreach (T row in rows) {
            this.Value<T>(row, skipNullValues);
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType Values(List<object> rows, bool skipNullValues = false) {
        foreach (object row in rows) {
            this.Value(row, skipNullValues);
        }

        return (DBQueryType)this;
    }
    #endregion

    #region Query building - From/Into
    public virtual DBQueryType From(From<DBQueryType> fromSentence) {
        this.QueryFrom.Add(fromSentence);

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType From<TableClass>() {
        string tableName      = typeof(TableClass).Name;
        Table  tableAttribute = typeof(TableClass).GetCustomAttribute<Table>();
        if (tableAttribute != null) {
            tableName = tableAttribute.Name;
        }

        this.From(new From<DBQueryType> {
            Table       = tableName,
            EscapeTable = true,
        });

        return (DBQueryType) this;
    }

    public virtual DBQueryType From(string tableName) {
        return this.From(new From<DBQueryType> {
            Table       = tableName,
            EscapeTable = true
        });
    }

    public virtual DBQueryType Into(From<DBQueryType> fromSentence) {
        return this.From(fromSentence);
    }

    public virtual DBQueryType Into<TableClass>() {
        return this.From<TableClass>();
    }

    public virtual DBQueryType Into(string tableName) {
        return this.From(tableName);
    }
    #endregion

    #region Query building - Join
    public virtual DBQueryType Join(Join<DBQueryType> sentence) {
        this.QueryJoin.Add(sentence);

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType Join<JoinedTable, ParentTable>(Expression<Func<JoinedTable, object>> joinedTableExpression, Expression<Func<ParentTable, object>> parentTableExpression, WhereComparer comparer = WhereComparer.EQUALS, JoinDirection direction = JoinDirection.NONE) 
        where JoinedTable : class
        where ParentTable : class
    {
        string joinedTableName       = typeof(JoinedTable).GetTableName();
        string joinedTableColumnName = ExpressionHelper.ExtractColumnName<JoinedTable>(joinedTableExpression);
        string parentTableName       = typeof(ParentTable).GetTableName();
        string parentTableColumnName = ExpressionHelper.ExtractColumnName<ParentTable>(parentTableExpression);

        return this.Join(new Join<DBQueryType> {
            Direction   = direction,
            Table       = joinedTableName,
            EscapeTable = true,
            Condition   = new Where<DBQueryType> {
                Field       = new FieldSelector { Table = joinedTableName, Field = joinedTableColumnName, Escape = true },
                ValueField  = new FieldSelector { Table = parentTableName, Field = parentTableColumnName, Escape = true },
                Comparer    = comparer,
                EscapeValue = true
            }
        });
    }

    public virtual DBQueryType Join(string tableName, Where<DBQueryType> condition, JoinDirection direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = direction,
            Table       = tableName,
            EscapeTable = true,
            Condition   = condition
        });
    }

    public virtual DBQueryType Join(string tableName, string fieldLeft, string fieldRight, WhereComparer comparer = WhereComparer.EQUALS, JoinDirection direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = direction,
            Table       = tableName,
            EscapeTable = true,
            Condition   = new Where<DBQueryType> {
                Field       = new FieldSelector { Field = fieldLeft,  Escape = true },
                ValueField  = new FieldSelector { Field = fieldRight, Escape = true },
                Comparer    = comparer,
                EscapeValue = true
            }
        });
    }

    public virtual DBQueryType Join(string tableName, string tableLeft, string fieldLeft, string tableRight, string fieldRight, WhereComparer comparer = WhereComparer.EQUALS, JoinDirection direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = direction,
            Table       = tableName,
            EscapeTable = true,
            Condition   = new Where<DBQueryType> {
                Field       = new FieldSelector { Table = tableLeft,  Field = fieldLeft,  Escape = true },
                ValueField  = new FieldSelector { Table = tableRight, Field = fieldRight, Escape = true },
                Comparer    = comparer,
                EscapeValue = true
            }
        });
    }

    public virtual DBQueryType Join(string tableName, FieldSelector left, FieldSelector right, WhereComparer comparer = WhereComparer.EQUALS, JoinDirection direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = direction,
            Table       = tableName,
            EscapeTable = true,
            Condition   = new Where<DBQueryType> {
                Field       = left,
                ValueField  = right,
                Comparer    = comparer,
                EscapeValue = true
            }
        });
    }
    #endregion

    #region Query building - Where
    public virtual DBQueryType Where(Where<DBQueryType> whereSentence) {
        this.QueryWhere.Add(whereSentence);

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType Where(FieldSelector left, dynamic fieldValue) {
        return this.Where(new Where<DBQueryType> {
            Field = left,
            Value = fieldValue
        });
    }

    public virtual DBQueryType Where(string fieldName, dynamic fieldValue) {
        return this.Where(new Where<DBQueryType> {
            Field  = new FieldSelector(fieldName),
            Value = fieldValue
        });
    }

    public virtual DBQueryType Where<T>(Expression<Func<T, object>> expression, dynamic value) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            return this.Where(new FieldSelector {
                    Table  = tableName,
                    Field  = columnName,
                    Escape = true
                }, 
                value
            );
        }
        return (DBQueryType)this;
    }

    public virtual DBQueryType Where(FieldSelector left, FieldSelector right) {
        return this.Where(new Where<DBQueryType> {
            Field      = left,
            ValueField = right
        });
    }
    #endregion

    #region Query building - WhereIn
    public virtual DBQueryType WhereIn(WhereIn<DBQueryType> whereInSentence) {
        this.QueryWhereIn.Add(whereInSentence);

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType WhereIn(FieldSelector left, List<dynamic> fieldValues) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field  = left,
            Values = fieldValues
        });
    }

    public virtual DBQueryType WhereIn(string fieldName, List<dynamic> fieldValues) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field  = new FieldSelector(fieldName), 
            Values = fieldValues
        });
    }

    public virtual DBQueryType WhereIn<T>(Expression<Func<T, object>> expression, List<dynamic> fieldValues) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            return this.WhereIn(new FieldSelector {
                    Table  = tableName,
                    Field  = columnName,
                    Escape = true
                },
                fieldValues
            );
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType WhereIn(string fieldName, Query<DBQueryType> subquery) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field    = new FieldSelector(fieldName),
            Subquery = subquery
        });
    }
    #endregion

    #region Query building - Where LIKE
    public virtual DBQueryType WhereLike(FieldSelector left, string fieldValue) {
        return this.Where(new Where<DBQueryType> {
            Field       = left,
            Value       = fieldValue,
            Comparer    = WhereComparer.LIKE,
            EscapeValue = true
        });
    }

    public virtual DBQueryType WhereLike<T>(Expression<Func<T, object>> expression, string value, bool escape = true) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            return this.Where(new Where<DBQueryType> { 
                Field = new FieldSelector {
                    Table  = tableName,
                    Field  = columnName,
                    Escape = true
                },
                Value       = value,
                Comparer    = WhereComparer.LIKE,
                EscapeValue = escape
            });
        }

        return (DBQueryType)this;
    }

   
    public virtual DBQueryType WhereLike(string fieldName, string fieldValue) {
        return this.WhereLike(
            new FieldSelector(fieldName),
            fieldValue
        );
    }

    public virtual DBQueryType WhereLikeLeft(string fieldName, string fieldValue) {
        return this.WhereLike(
            new FieldSelector(fieldName),
            $"%{fieldValue.TrimStart('%')}"
        );
    }

    public virtual DBQueryType WhereLikeRight(string fieldName, string fieldValue) {
        return this.WhereLike(
            new FieldSelector(fieldName),
            $"{fieldValue.TrimEnd('%')}%"
        );
    }
    #endregion

    #region Query building - Group By
    public virtual DBQueryType GroupBy(GroupBy group) {
        this.QueryGroup.Add(group);

        this.Touch();
        return (DBQueryType) this;
    }
    public virtual DBQueryType GroupBy(FieldSelector field) {
        return this.GroupBy(new GroupBy {
            Field = field
        });
    }

    public virtual DBQueryType GroupBy<T>(Expression<Func<T, object>> expression) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            return this.GroupBy(new FieldSelector {
                Table  = tableName,
                Field  = columnName,
                Escape = true
            });
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType GroupBy(string fieldName, string tableName = null, bool escape = true) {
        return this.GroupBy(new FieldSelector(tableName, fieldName, escape));
    }
    #endregion

    #region Query building - Order By
    public virtual DBQueryType OrderBy(OrderBy orderSentence) {
        this.QueryOrder.Add(orderSentence);

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType OrderBy(FieldSelector field, OrderDirection direction = OrderDirection.ASC) {
        return this.OrderBy(new OrderBy {
            Field     = field,
            Direction = direction
        });
    }

    public virtual DBQueryType OrderBy<T>(Expression<Func<T, object>> expression, OrderDirection direction = OrderDirection.ASC) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            return this.OrderBy(new FieldSelector {
                Table  = tableName,
                Field  = columnName,
                Escape = true
            }, direction);
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType OrderBy(string fieldName, OrderDirection direction = OrderDirection.ASC, bool escapeField = true) {
        return this.OrderBy(new QueryBuilding.OrderBy {
            Field     = new FieldSelector {
                Field  = fieldName,
                Escape = escapeField,
            },
            Direction = direction
        });
    }
    #endregion

    #region Query building - Limit
    public virtual DBQueryType Limit(Limit limitSentence) {
        this.QueryLimit = limitSentence;

        this.Touch();
        return (DBQueryType) this;
    }

    public virtual DBQueryType Limit(long count, long offset = 0) {
        return this.Limit(new Limit {
            Count  = count,
            Offset = offset
        });
    }
    #endregion

    #region Query building - Create
    public DBQueryType CreateTable(Type tableType) {
        this.SetQueryType(QueryType.CREATE);

        this.QueryCreate = tableType;

        this.Touch();
        return (DBQueryType)this;
    }

    public DBQueryType CreateTable<T>() where T : class {
        return this.CreateTable(typeof(T));
    }

    public DBQueryType Create<T>() where T : class {
        return this.CreateTable<T>();
    }
    #endregion

    #region Query building - Raw
    public DBQueryType Raw(string query, string queryPreparedString) {
        this.SetQueryType(QueryType.RAW);

        this.QueryRenderedString = query;
        this.QueryPreparedString = queryPreparedString;

        this.Untouch();
        return (DBQueryType)this;
    }
    public DBQueryType Raw(string query) {
        return this.Raw(query, query);
    }

    public DBQueryType Raw(string query, Dictionary<string, PreparedValue> queryPreparedData) {
        this.SetQueryType(QueryType.RAW);

        this.QueryPreparedData = queryPreparedData;

        return this.Raw(query);
    }

    public DBQueryType Raw(string query, Dictionary<string, dynamic> queryPreparedData) {
        this.SetQueryType(QueryType.RAW);

        return this.Raw(query, queryPreparedData.ToDictionary(
            dataItem => dataItem.Key, 
            dataItem => new PreparedValue { Value = dataItem.Value, EscapeValue = true }
        ));
    }
    #endregion
    #endregion

    #region Query sentence rendering virtual methods
    protected virtual string _RenderCountSentence()        { throw new NotImplementedException(); }
    protected virtual string _RenderSelectSentence()       { throw new NotImplementedException(); }
    protected virtual string _RenderFromSentence()         { throw new NotImplementedException(); }
    protected virtual string _RenderJoinSentence()         { throw new NotImplementedException(); }
    protected virtual string _RenderWhereSentence()        { throw new NotImplementedException(); }
    protected virtual string _RenderGroupBy()              { throw new NotImplementedException(); }
    protected virtual string _RenderOrderBy()              { throw new NotImplementedException(); }
    protected virtual string _RenderGroupSentence()        { throw new NotImplementedException(); }
    protected virtual string _RenderHavingSentence()       { throw new NotImplementedException(); }
    protected virtual string _RenderOrderSentence()        { throw new NotImplementedException(); }
    protected virtual string _RenderLimitSentence()        { throw new NotImplementedException(); }
    protected virtual string _RenderSelectExtraSentence()  { throw new NotImplementedException(); }

    protected virtual string _RenderDeleteSentence()       { throw new NotImplementedException(); }
    protected virtual string _RenderUpdateSentence()       { throw new NotImplementedException(); }
    protected virtual string _RenderSetSentence()          { throw new NotImplementedException(); }
    protected virtual string _RenderInsertIntoSentence()   { throw new NotImplementedException(); }
    protected virtual string _RenderInsertValuesSentence() { throw new NotImplementedException(); }

    protected virtual string _RenderCreateSentence<T>() where T : Table { throw new NotImplementedException(); }
    protected virtual string _RenderCreateSentence(Type tableType)      { throw new NotImplementedException(); }
    #endregion

    #region Helpers
    public ColumnDataType? GetColumnDataType(string typeString) {
        return typeString.ToLowerInvariant() switch {
            "bool"      => ColumnDataType.Boolean,
            "boolean"   => ColumnDataType.Boolean,
            "int16"     => ColumnDataType.Int16,
            "int"       => ColumnDataType.Int,
            "int32"     => ColumnDataType.Int32,
            "int64"     => ColumnDataType.Int64,
            "uint16"    => ColumnDataType.UInt16,
            "uint32"    => ColumnDataType.UInt32,
            "uint"      => ColumnDataType.UInt,
            "uint64"    => ColumnDataType.UInt64,
            "decimal"   => ColumnDataType.Decimal,
            "float"     => ColumnDataType.Float,
            "double"    => ColumnDataType.Double,
            "text"      => ColumnDataType.Text,
            "char"      => ColumnDataType.Char,
            "varchar"   => ColumnDataType.Varchar,
            "enum"      => ColumnDataType.Enum,
            "date"      => ColumnDataType.Date,
            "datetime"  => ColumnDataType.DateTime,
            "time"      => ColumnDataType.Time,
            "timestamp" => ColumnDataType.Timestamp,
            "binary"    => ColumnDataType.Binary,
            "guid"      => ColumnDataType.Guid,
            "uuid"      => ColumnDataType.Guid,
            "json"      => ColumnDataType.Json,
            "xml"       => ColumnDataType.Xml,

            _ => null
        };
    }
    #endregion
}
