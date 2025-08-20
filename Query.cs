using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    public List<GroupBy> QueryGroup   { get; protected set; } = new List<GroupBy>();
    public List<OrderBy> QueryOrder   { get; protected set; } = new List<OrderBy>();
    public Limit         QueryLimit   { get; protected set; }


    public QueryType     QueryType    { get; protected set; } = QueryType.RAW;

    /// <summary>
    /// Raw query string rendered as reference but not for real usage
    /// As prepared queries are not rendered as-is, this is intended to be used
    /// as reference as the translated query that will be executed, but could
    /// be different of the real result.
    /// </summary>
    public string                            QueryRendered       { get; protected set; }
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

    public string PrepareQueryValue(dynamic queryValue, bool escape) {
        string prefix      = "@prepared_query_value_";
        Query<DBQueryType> targetQuery = this.ParentQuery != null ? this.ParentQuery : this;
        string label       = $"{prefix}{targetQuery.QueryPreparedData.Count}";

        targetQuery.QueryPreparedData.Add(label, new PreparedValue {
            Value       = queryValue,
            EscapeValue = escape
        });

        return label;
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
        this.QueryRendered       = null;
        this.QueryPreparedString = null;
        this.QueryRenderedData   = new Dictionary<string, string>();
        this.QueryPreparedData   = new Dictionary<string, PreparedValue>();

        return (DBQueryType)this;
    }
    #endregion

    #region Query rendering
    public virtual string RenderPrepared(bool force = true) {
        this._RenderPrepared(force);

        return this.QueryRendered;
    }

    public virtual void _RenderPrepared(bool force = true) {
        throw new NotImplementedException();
    }

    public virtual string Render(bool force = true) {
        this._Render(force);

        return this.QueryPreparedString;
    }

    public virtual void _Render(bool force = true) {
        if (force) {
            this.ResetPreparedData();
        }

        if (string.IsNullOrWhiteSpace(this.QueryPreparedString) || force) {
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

        return (DBQueryType) this;
    }

    public virtual DBQueryType Select(FieldSelector field) {
        return this.Select(new Select<DBQueryType> {
            Field = field
        });
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

    #region Query building - Values for INSERT/SET
    public virtual DBQueryType Set(Where<DBQueryType> setValue) {
        this.QuerySet.Add(setValue);

        return (DBQueryType) this;
    }

    public virtual DBQueryType Set(string fieldName, dynamic value, bool escape = true) {
        return this.Set(new Where<DBQueryType> {
            Field       = new FieldSelector(fieldName),
            Value       = value,
            EscapeValue = escape
        });
    }

    public virtual DBQueryType Value(Dictionary<string, dynamic> row, bool skipNullValues = true) {
        foreach (string column in row.Keys) {
            if (!this.QueryColumns.Contains(column)) {
                this.QueryColumns.Add(column);
            }
        }
        this.QueryValues.Add(row);

        return (DBQueryType) this;
    }

    public virtual DBQueryType Value<T>(T row, bool skipNullValues = true) where T : class {
        return this.Value(row.ToDynamicDictionary());
    }

    public virtual DBQueryType Value(object row, bool skipNullValues = true) {
        return this.Value(row.ToDynamicDictionary());
    }

    public virtual DBQueryType Values<T>(List<T> rows, bool skipNullValues = true) where T : class {
        foreach (object row in rows) {
            this.Value(row, skipNullValues);
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType Values(List<object> rows, bool skipNullValues = true) {
        foreach (object row in rows) {
            this.Value(row, skipNullValues);
        }

        return (DBQueryType)this;
    }
    #endregion

    #region Query building - From/Into
    public virtual DBQueryType From(From<DBQueryType> fromSentence) {
        this.QueryFrom.Add(fromSentence);

        return (DBQueryType) this;
    }

    public virtual DBQueryType From<TableClass>() {
        string tableName      = typeof(TableClass).Name;
        Table  tableAttribute = typeof(TableClass).GetCustomAttribute<Table>();
        if (tableAttribute != null) {
            tableName = tableAttribute.Name;
        }

        this.QueryFrom.Add(new From<DBQueryType> {
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

        return (DBQueryType) this;
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

        return (DBQueryType) this;
    }

    public virtual DBQueryType Where(string fieldName, dynamic fieldValue) {
        return this.Where(new Where<DBQueryType> {
            Field  = new FieldSelector(fieldName),
            Value = fieldValue
        });
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

        return (DBQueryType) this;
    }

    public virtual DBQueryType WhereIn(string fieldName, List<dynamic> fieldValues) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field  = new FieldSelector(fieldName), 
            Values = fieldValues
        });
    }

    public virtual DBQueryType WhereIn(string fieldName, Query<DBQueryType> subquery) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field    = new FieldSelector(fieldName),
            Subquery = subquery
        });
    }
    #endregion

    #region Query building - Group By
    public virtual DBQueryType GroupBy(GroupBy group) {
        this.QueryGroup.Add(group);

        return (DBQueryType) this;
    }
    public virtual DBQueryType GroupBy(FieldSelector field) {
        return this.GroupBy(new GroupBy {
            Field = field
        });
    }

    public virtual DBQueryType GroupBy(string fieldName, string tableName = null, bool escape = true) {
        return this.GroupBy(new FieldSelector(tableName, fieldName, escape));
    }
    #endregion

    #region Query building - Order By
    public virtual DBQueryType OrderBy(OrderBy orderSentence) {
        this.QueryOrder.Add(orderSentence);

        return (DBQueryType) this;
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
    public DBQueryType Create(Type tableType) {
        this.SetQueryType(QueryType.CREATE);

        this.QueryCreate = tableType;

        return (DBQueryType)this;
    }

    public DBQueryType Create<T>() {
        return this.Create(typeof(T));
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
}
