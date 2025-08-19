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

    public Query(Query<DBQueryType> ParentQuery) {
        this.ParentQuery = ParentQuery;
    }

    public Query<DBQueryType> WithParentQuery(Query<DBQueryType> ParentQuery) {
        this.ParentQuery = ParentQuery;

        return (DBQueryType) this;
    }

    public string PrepareQueryValue(dynamic QueryValue, bool Escape) {
        string Prefix      = "@prepared_query_value_";
        Query<DBQueryType> TargetQuery = this.ParentQuery != null ? this.ParentQuery : this;
        string Label       = $"{Prefix}{TargetQuery.QueryPreparedData.Count}";

        TargetQuery.QueryPreparedData.Add(Label, new PreparedValue {
            Value       = QueryValue,
            EscapeValue = Escape
        });

        return Label;
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
    public virtual string RenderPrepared(bool Force = true) {
        this._RenderPrepared(Force);

        return this.QueryRendered;
    }

    public virtual void _RenderPrepared(bool Force = true) {
        throw new NotImplementedException();
    }

    public virtual string Render(bool Force = true) {
        this._Render(Force);

        return this.QueryPreparedString;
    }

    public virtual void _Render(bool Force = true) {
        if (Force) {
            this.ResetPreparedData();
        }

        if (string.IsNullOrWhiteSpace(this.QueryPreparedString) || Force) {
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
        List<string> QueryGroups = new List<string> {
            _RenderCountSentence      (),
            _RenderFromSentence       (),
            _RenderJoinSentence       (),
            _RenderWhereSentence      (),
            _RenderGroupSentence      (),
            _RenderHavingSentence     (),
            _RenderSelectExtraSentence()
        };

        QueryPreparedString = string.Join(" ", QueryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }


    protected virtual void _RenderSelectQuery() {
        List<string> QueryGroups = new List<string> {
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

        QueryPreparedString = string.Join(" ", QueryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    protected virtual void _RenderInsertQuery() {
        List<string> QueryGroups = new List<string> {
            _RenderInsertIntoSentence  (),
            _RenderInsertValuesSentence(),
        };

        QueryPreparedString = string.Join(" ", QueryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    protected virtual void _RenderUpdateQuery() {
        List<string> QueryGroups = new List<string> {
            _RenderUpdateSentence     (),
            _RenderSetSentence        (),
            _RenderWhereSentence      (),
            _RenderOrderSentence      (),
            _RenderLimitSentence      ()
        };

        QueryPreparedString = string.Join(" ", QueryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    protected virtual void _RenderDeleteQuery() {
        List<string> QueryGroups = new List<string> {
            _RenderDeleteSentence(),
            _RenderWhereSentence (),
            _RenderOrderSentence (),
            _RenderLimitSentence ()
        };

        QueryPreparedString = string.Join(" ", QueryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    protected virtual void _RenderCreateQuery() {
        QueryPreparedString = _RenderCreateSentence(this.QueryCreate);
    }
    #endregion

    #region Public query building methods
    public virtual DBQueryType SetQueryType(QueryType QueryType) {
        this.QueryType = QueryType;

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
    public virtual DBQueryType Select(Select<DBQueryType> Select) {
        this.SetQueryType(QueryType.SELECT);

        this.QuerySelect.Add(Select);

        return (DBQueryType) this;
    }

    public virtual DBQueryType Select(FieldSelector Field) {
        return this.Select(new Select<DBQueryType> {
            Field = Field
        });
    }

    public virtual DBQueryType Select(Query<DBQueryType> Subquery, string Alias = null) {
        return this.Select(new Select<DBQueryType> {
            Subquery = Subquery,
            Alias    = Alias
        });
    }

    public virtual DBQueryType Select(string FieldName, bool Escape = true) {
        return this.Select(new Select<DBQueryType> {
            Field = new FieldSelector(FieldName, Escape)
        });
    }

    public virtual DBQueryType Select(List<string> FieldNames, bool Escape = true) {
        foreach (string FieldName in FieldNames) {
            this.Select(FieldName, Escape);
        }

        return (DBQueryType) this;
    }
    #endregion

    #region Query building - Values for INSERT/SET
    public virtual DBQueryType Set(Where<DBQueryType> SetValue) {
        this.QuerySet.Add(SetValue);

        return (DBQueryType) this;
    }

    public virtual DBQueryType Set(string FieldName, dynamic Value, bool Escape = true) {
        return this.Set(new Where<DBQueryType> {
            Field       = new FieldSelector(FieldName),
            Value       = Value,
            EscapeValue = Escape
        });
    }

    public virtual DBQueryType Value(Dictionary<string, dynamic> Row, bool SkipNullValues = true) {
        foreach (string Column in Row.Keys) {
            if (!this.QueryColumns.Contains(Column)) {
                this.QueryColumns.Add(Column);
            }
        }
        this.QueryValues.Add(Row);

        return (DBQueryType) this;
    }

    public virtual DBQueryType Value<T>(T Row, bool SkipNullValues = true) where T : class {
        return this.Value(Row.ToDynamicDictionary());
    }

    public virtual DBQueryType Value(object Row, bool SkipNullValues = true) {
        return this.Value(Row.ToDynamicDictionary());
    }

    public virtual DBQueryType Values<T>(List<T> Rows, bool SkipNullValues = true) where T : class {
        foreach (object Row in Rows) {
            this.Value(Row, SkipNullValues);
        }

        return (DBQueryType)this;
    }

    public virtual DBQueryType Values(List<object> Rows, bool SkipNullValues = true) {
        foreach (object Row in Rows) {
            this.Value(Row, SkipNullValues);
        }

        return (DBQueryType)this;
    }
    #endregion

    #region Query building - From/Into
    public virtual DBQueryType From(From<DBQueryType> FromSentence) {
        this.QueryFrom.Add(FromSentence);

        return (DBQueryType) this;
    }

    public virtual DBQueryType From<TableClass>() {
        string TableName      = typeof(TableClass).Name;
        Table  TableAttribute = typeof(TableClass).GetCustomAttribute<Table>();
        if (TableAttribute != null) {
            TableName = TableAttribute.Name;
        }

        this.QueryFrom.Add(new From<DBQueryType> {
            Table       = TableName,
            EscapeTable = true,
        });

        return (DBQueryType) this;
    }

    public virtual DBQueryType From(string TableName) {
        return this.From(new From<DBQueryType> {
            Table       = TableName,
            EscapeTable = true
        });
    }

    public virtual DBQueryType Into(From<DBQueryType> FromSentence) {
        return this.From(FromSentence);
    }

    public virtual DBQueryType Into<TableClass>() {
        return this.From<TableClass>();
    }

    public virtual DBQueryType Into(string TableName) {
        return this.From(TableName);
    }
    #endregion

    #region Query building - Join
    public virtual DBQueryType Join(Join<DBQueryType> Sentence) {
        this.QueryJoin.Add(Sentence);

        return (DBQueryType) this;
    }

    public virtual DBQueryType Join(string TableName, Where<DBQueryType> Condition, JoinDirection Direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = Direction,
            Table       = TableName,
            EscapeTable = true,
            Condition   = Condition
        });
    }

    public virtual DBQueryType Join(string TableName, string FieldLeft, string FieldRight, WhereComparer Comparer = WhereComparer.EQUALS, JoinDirection Direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = Direction,
            Table       = TableName,
            EscapeTable = true,
            Condition   = new Where<DBQueryType> {
                Field       = new FieldSelector { Field = FieldLeft,  Escape = true },
                ValueField  = new FieldSelector { Field = FieldRight, Escape = true },
                Comparer    = Comparer,
                EscapeValue = true
            }
        });
    }

    public virtual DBQueryType Join(string TableName, string TableLeft, string FieldLeft, string TableRight, string FieldRight, WhereComparer Comparer = WhereComparer.EQUALS, JoinDirection Direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = Direction,
            Table       = TableName,
            EscapeTable = true,
            Condition   = new Where<DBQueryType> {
                Field       = new FieldSelector { Table = TableLeft,  Field = FieldLeft,  Escape = true },
                ValueField  = new FieldSelector { Table = TableRight, Field = FieldRight, Escape = true },
                Comparer    = Comparer,
                EscapeValue = true
            }
        });
    }

    public virtual DBQueryType Join(string TableName, FieldSelector Left, FieldSelector Right, WhereComparer Comparer = WhereComparer.EQUALS, JoinDirection Direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = Direction,
            Table       = TableName,
            EscapeTable = true,
            Condition   = new Where<DBQueryType> {
                Field       = Left,
                ValueField  = Right,
                Comparer    = Comparer,
                EscapeValue = true
            }
        });
    }
    #endregion

    #region Query building - Where
    public virtual DBQueryType Where(Where<DBQueryType> WhereSentence) {
        this.QueryWhere.Add(WhereSentence);

        return (DBQueryType) this;
    }

    public virtual DBQueryType Where(string FieldName, dynamic FieldValue) {
        return this.Where(new Where<DBQueryType> {
            Field  = new FieldSelector(FieldName),
            Value = FieldValue
        });
    }

    public virtual DBQueryType Where(FieldSelector Left, FieldSelector Right) {
        return this.Where(new Where<DBQueryType> {
            Field      = Left,
            ValueField = Right
        });
    }
    #endregion

    #region Query building - WhereIn
    public virtual DBQueryType WhereIn(WhereIn<DBQueryType> WhereInSentence) {
        this.QueryWhereIn.Add(WhereInSentence);

        return (DBQueryType) this;
    }

    public virtual DBQueryType WhereIn(string FieldName, List<dynamic> FieldValues) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field  = new FieldSelector(FieldName), 
            Values = FieldValues
        });
    }

    public virtual DBQueryType WhereIn(string FieldName, Query<DBQueryType> Subquery) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field    = new FieldSelector(FieldName),
            Subquery = Subquery
        });
    }
    #endregion

    #region Query building - Group By
    public virtual DBQueryType GroupBy(GroupBy Group) {
        this.QueryGroup.Add(Group);

        return (DBQueryType) this;
    }
    public virtual DBQueryType GroupBy(FieldSelector Field) {
        return this.GroupBy(new GroupBy {
            Field = Field
        });
    }

    public virtual DBQueryType GroupBy(string FieldName, string TableName = null, bool Escape = true) {
        return this.GroupBy(new FieldSelector(TableName, FieldName, Escape));
    }
    #endregion

    #region Query building - Order By
    public virtual DBQueryType OrderBy(OrderBy OrderSentence) {
        this.QueryOrder.Add(OrderSentence);

        return (DBQueryType) this;
    }

    public virtual DBQueryType OrderBy(string FieldName, OrderDirection Direction = OrderDirection.ASC, bool EscapeField = true) {
        return this.OrderBy(new QueryBuilding.OrderBy {
            Field     = new FieldSelector {
                Field  = FieldName,
                Escape = EscapeField,
            },
            Direction = Direction
        });
    }
    #endregion

    #region Query building - Limit
    public virtual DBQueryType Limit(Limit LimitSentence) {
        this.QueryLimit = LimitSentence;

        return (DBQueryType) this;
    }

    public virtual DBQueryType Limit(long Count, long Offset = 0) {
        return this.Limit(new Limit {
            Count  = Count,
            Offset = Offset
        });
    }
    #endregion

    #region Query building - Create
    public DBQueryType Create(Type TableType) {
        this.SetQueryType(QueryType.CREATE);

        this.QueryCreate = TableType;

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
    protected virtual string _RenderCreateSentence(Type TableType)      { throw new NotImplementedException(); }
    #endregion
}
