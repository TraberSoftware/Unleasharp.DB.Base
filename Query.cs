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

/// <summary>
/// Represents a generic query builder for constructing and managing database queries.
/// </summary>
/// <remarks>
/// The <see cref="Query{DBQueryType}"/> class provides a flexible and extensible framework for building
/// database queries programmatically. It supports various query operations such as SELECT, INSERT, UPDATE, DELETE, and
/// CREATE, as well as advanced features like joins, grouping, ordering, and parameterized queries. This class is
/// designed to be extended and customized for specific database engines or query requirements.
/// </remarks>
/// <typeparam name="DBQueryType">
/// The specific type of the query, which must inherit from <see cref="Query{DBQueryType}"/>. This allows for fluent API
/// usage and type-safe query chaining.
/// </typeparam>
public class Query<DBQueryType> : Renderable
    where DBQueryType : Query<DBQueryType> 
{
    /// <summary>
    /// Gets the database engine associated with this query.
    /// </summary>
    protected virtual DatabaseEngine _Engine                 { get; }

    /// <summary>
    /// Gets the prefix used for preparing query parameter names.
    /// </summary>
    protected virtual string         _QueryPreparationPrefix { get; } = "@prepared_query_value_";

    #region Syntax sugar
    /// <summary>
    /// Returns the current instance cast to the specified query type.
    /// </summary>
    /// <typeparam name="T">The query type to cast to.</typeparam>
    /// <returns>The current instance as type <typeparamref name="T"/>.</returns>
    public virtual T GetThis<T>() where T : Query<DBQueryType> {
        return (T) this;
    }

    /// <summary>
    /// Creates a new instance of the query type.
    /// </summary>
    /// <returns>A new instance of <typeparamref name="DBQueryType"/>.</returns>
    public static DBQueryType GetInstance() {
        return Activator.CreateInstance<DBQueryType>();
    }
    #endregion

    #region Query parameters
    /// <summary>
    /// Gets or sets a value indicating whether the query should use DISTINCT.
    /// </summary>
    public bool QueryDistinct                              { get; protected set; } = false;

    /// <summary>
    /// Gets the list of SELECT fields or expressions for the query.
    /// </summary>
    public List<Select <DBQueryType>> QuerySelect          { get; protected set; } = new List<Select <DBQueryType>>();

    /// <summary>
    /// Gets the list of FROM clauses for the query.
    /// </summary>
    public List<From   <DBQueryType>> QueryFrom            { get; protected set; } = new List<From   <DBQueryType>>();

    /// <summary>
    /// Gets the list of JOIN clauses for the query.
    /// </summary>
    public List<Join   <DBQueryType>> QueryJoin            { get; protected set; } = new List<Join   <DBQueryType>>();

    /// <summary>
    /// Gets the list of WHERE conditions for the query.
    /// </summary>
    public List<Where  <DBQueryType>> QueryWhere           { get; protected set; } = new List<Where  <DBQueryType>>();

    /// <summary>
    /// Gets the list of WHERE IN conditions for the query.
    /// </summary>
    public List<WhereIn<DBQueryType>> QueryWhereIn         { get; protected set; } = new List<WhereIn<DBQueryType>>();

    /// <summary>
    /// Gets the list of HAVING conditions for the query.
    /// </summary>
    public List<Where  <DBQueryType>> QueryHaving          { get; protected set; } = new List<Where  <DBQueryType>>();

    /// <summary>
    /// Gets the list of SET clauses for UPDATE queries.
    /// </summary>
    public List<Where  <DBQueryType>> QuerySet             { get; protected set; } = new List<Where  <DBQueryType>>();

    /// <summary>
    /// Gets the list of UNION clauses for SELECT UNION queries.
    /// </summary>
    public List<Union  <DBQueryType>> QueryUnion           { get; protected set; } = new List<Union  <DBQueryType>>();

    /// <summary>
    /// Gets the list of value dictionaries for INSERT queries.
    /// </summary>
    public List<Dictionary<string, dynamic>> QueryValues   { get; protected set; } = new List<Dictionary<string, dynamic>>();

    /// <summary>
    /// Gets the list of columns involved in the query.
    /// </summary>
    public List<string> QueryColumns                       { get; protected set; } = new List<string>();

    /// <summary>
    /// Gets or sets the target field selector for INTO clauses.
    /// </summary>
    public FieldSelector QueryInto                         { get; protected set; }

    /// <summary>
    /// Gets or sets the type to create for CREATE TABLE queries.
    /// </summary>
    public Type QueryCreate                                { get; protected set; }

    /// <summary>
    /// Gets the list of GROUP BY clauses for the query.
    /// </summary>
    public List<GroupBy> QueryGroup                        { get; protected set; } = new List<GroupBy>();

    /// <summary>
    /// Gets the list of ORDER BY clauses for the query.
    /// </summary>
    public List<OrderBy> QueryOrder                        { get; protected set; } = new List<OrderBy>();

    /// <summary>
    /// Gets or sets the LIMIT clause for the query.
    /// </summary>
    public Limit QueryLimit                                { get; protected set; }

    /// <summary>
    /// Gets or sets the type of the query (SELECT, INSERT, etc.).
    /// </summary>
    public QueryType QueryType                             { get; protected set; } = QueryType.RAW;

    /// <summary>
    /// Gets or set the conflict resolution strategy to be used when a conflict occurs during an INSERT.
    /// </summary>
    public OnInsertConflict QueryOnConflict                { get; protected set; } = OnInsertConflict.NONE;

    /// <summary>
    /// Gets the name of the key column used to resolve conflicts during INSERT operations.
    /// </summary>
    public string QueryOnConflictKeyColumn                 { get; protected set; } = string.Empty;

    /// <summary>
    /// Raw query string rendered as reference but not for real usage.
    /// As prepared queries are not rendered as-is, this is intended to be used
    /// as reference as the translated query that will be executed, but could
    /// be different from the real result.
    /// </summary>
    public string                            QueryRenderedString { get; protected set; }

    /// <summary>
    /// Pre-rendered data values for query rendering.
    /// </summary>
    public Dictionary<string, string>        QueryRenderedData   { get; protected set; } = new Dictionary<string, string>();

    /// <summary>
    /// Query string with data placeholders for query preparation.
    /// </summary>
    public string                            QueryPreparedString { get; protected set; }

    /// <summary>
    /// Data values for query preparation.
    /// </summary>
    public Dictionary<string, PreparedValue> QueryPreparedData   { get; protected set; } = new Dictionary<string, PreparedValue>();

    /// <summary>
    /// Gets or sets the parent query for nested queries.
    /// </summary>
    public Query<DBQueryType> ParentQuery { get; protected set; }

    /// <summary>
    /// Gets or sets the alias to use for a union query
    /// </summary>
    public string QueryUnionAlias { get; protected set; }

    #region Constructor sugar

    /// <summary>
    /// Initializes a new instance of the <see cref="Query{DBQueryType}"/> class.
    /// </summary>
    public Query() {
        this.ParentQuery = this;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Query{DBQueryType}"/> class with a parent query.
    /// </summary>
    /// <param name="parentQuery">The parent query.</param>
    public Query(Query<DBQueryType> parentQuery) {
        this.ParentQuery = parentQuery;
    }

    /// <summary>
    /// Sets the parent query for this instance.
    /// </summary>
    /// <param name="parentQuery">The parent query.</param>
    /// <returns>The current query instance.</returns>
    public Query<DBQueryType> WithParentQuery(Query<DBQueryType> parentQuery) {
        this.ParentQuery = parentQuery;
        return (DBQueryType) this;
    }

    /// <summary>
    /// Prepares a value for parameterized queries and returns its label.
    /// </summary>
    /// <param name="queryValue">The value to prepare.</param>
    /// <param name="escape">Whether to escape the value.</param>
    /// <returns>The label for the prepared value.</returns>
    public virtual string PrepareQueryValue(dynamic queryValue, bool escape) {
        Query<DBQueryType> targetQuery = this.ParentQuery != null ? this.ParentQuery : this;
        string             label       = this.GetNextPreparedQueryValueLabel();

        targetQuery.QueryPreparedData.Add(label, new PreparedValue {
            Value       = queryValue,
            EscapeValue = escape
        });

        return label;
    }

    /// <summary>
    /// Gets the next label for a prepared query value.
    /// </summary>
    /// <returns>The label string.</returns>
    public virtual string GetNextPreparedQueryValueLabel() {
        Query<DBQueryType> targetQuery = this.ParentQuery != null ? this.ParentQuery : this;
        return $"{this._QueryPreparationPrefix}{targetQuery.QueryPreparedData.Count}";
    }
    #endregion

    /// <summary>
    /// Resets the query to its initial state.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Reset() {
        this.QueryDistinct   = false;
        this.QueryFrom       = new List<From  <DBQueryType>>();
        this.QuerySelect     = new List<Select<DBQueryType>>();
        this.QueryJoin       = new List<Join  <DBQueryType>>();
        this.QueryWhere      = new List<Where <DBQueryType>>();
        this.QueryHaving     = new List<Where <DBQueryType>>();
        this.QuerySet        = new List<Where <DBQueryType>>();
        this.QueryUnion      = new List<Union <DBQueryType>>();
        this.QueryValues     = new List<Dictionary<string, dynamic>>();
        this.QueryInto       = null;
        this.QueryGroup      = new List<GroupBy>();
        this.QueryOrder      = new List<OrderBy>();
        this.QueryLimit      = null;
        this.QueryType       = QueryType.RAW;
        this.QueryOnConflict = OnInsertConflict.NONE;
        this.ParentQuery     = null;
        this.QueryUnionAlias = null;

        this.ResetPreparedData();

        return (DBQueryType) this;
    }

    /// <summary>
    /// Resets the prepared data for the query.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType ResetPreparedData() {
        this.QueryRenderedString = null;
        this.QueryPreparedString = null;
        this.QueryRenderedData   = new Dictionary<string, string>();
        this.QueryPreparedData   = new Dictionary<string, PreparedValue>();

        return (DBQueryType)this;
    }
    #endregion

    #region Control parameters
    /// <summary>
    /// Gets a value indicating whether the query has changed since last render.
    /// </summary>
    public bool HasChanged { get; private set; } = false;

    /// <summary>
    /// Marks the query as changed.
    /// </summary>
    public void Touch() {
        this.HasChanged = true;
    }

    /// <summary>
    /// Marks the query as unchanged.
    /// </summary>
    public void Untouch() {
        this.HasChanged = false;
    }
    #endregion

    #region Query rendering
    /// <summary>
    /// Renders the prepared query string and resets the change flag.
    /// </summary>
    /// <returns>The rendered query string.</returns>
    public virtual string RenderPrepared() {
        this._RenderPrepared();
        this.HasChanged = false;

        return this.QueryRenderedString;
    }

    /// <summary>
    /// Renders the prepared query. Must be implemented in derived classes.
    /// </summary>
    public virtual void _RenderPrepared() {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Renders the query string and resets the change flag.
    /// </summary>
    /// <returns>The rendered query string.</returns>
    public virtual string Render() {
        this._Render();
        this.HasChanged = false;

        return this.QueryPreparedString;
    }

    /// <summary>
    /// Renders the query string based on the query type.
    /// </summary>
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
                case QueryType.SELECT_UNION:
                    this._RenderSelectUnionQuery();
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

    /// <summary>
    /// Renders a COUNT query.
    /// </summary>
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

    /// <summary>
    /// Renders a SELECT query.
    /// </summary>
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

    /// <summary>
    /// Renders a SELECT UNION query.
    /// </summary>
    protected virtual void _RenderSelectUnionQuery() {
        List<string> queryGroups = new List<string> {
            _RenderSelectSentence     (),
            _RenderUnionSentence      (),
            _RenderWhereSentence      (),
            _RenderGroupSentence      (),
            _RenderHavingSentence     (),
            _RenderOrderSentence      (),
            _RenderLimitSentence      ()
        };

        QueryPreparedString = string.Join(" ", queryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    /// <summary>
    /// Renders an INSERT query.
    /// </summary>
    protected virtual void _RenderInsertQuery() {
        List<string> queryGroups = new List<string> {
            _RenderInsertIntoSentence      (),
            _RenderInsertValuesSentence    (),
            _RenderInsertOnConflictSentence()
        };

        QueryPreparedString = string.Join(" ", queryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    /// <summary>
    /// Renders an UPDATE query.
    /// </summary>
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

    /// <summary>
    /// Renders a DELETE query.
    /// </summary>
    protected virtual void _RenderDeleteQuery() {
        List<string> queryGroups = new List<string> {
            _RenderDeleteSentence(),
            _RenderWhereSentence (),
            _RenderOrderSentence (),
            _RenderLimitSentence ()
        };

        QueryPreparedString = string.Join(" ", queryGroups.Where(group => !string.IsNullOrWhiteSpace(group)));
    }

    /// <summary>
    /// Renders a CREATE query.
    /// </summary>
    protected virtual void _RenderCreateQuery() {
        QueryPreparedString = _RenderCreateSentence(this.QueryCreate);
    }
    #endregion

    #region Public query building methods
    /// <summary>
    /// Sets the query type.
    /// </summary>
    /// <param name="queryType">The query type.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType SetQueryType(QueryType queryType) {
        this.QueryType = queryType;

        return (DBQueryType) this;
    }

    #region Query Building - Operations
    /// <summary>
    /// Sets the query type to SELECT.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Select() {
        this.SetQueryType(QueryType.SELECT);

        return (DBQueryType)this;
    }

    /// <summary>
    /// Sets the query type to UPDATE.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Update() {
        this.SetQueryType(QueryType.UPDATE);

        return (DBQueryType)this;
    }

    /// <summary>
    /// Sets the query type to DELETE.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Delete() {
        this.SetQueryType(QueryType.DELETE);

        return (DBQueryType)this;
    }

    /// <summary>
    /// Sets the query type to CREATE.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Create() {
        this.SetQueryType(QueryType.CREATE);

        return (DBQueryType)this;
    }

    /// <summary>
    /// Sets the query type to INSERT.
    /// </summary>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Insert() {
        this.SetQueryType(QueryType.INSERT);

        return (DBQueryType)this;
    }
    #endregion

    #region Query building - Distinct
    /// <summary>
    /// Sets whether the query should use DISTINCT.
    /// </summary>
    /// <param name="distinct">True to use DISTINCT; otherwise, false.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Distinct(bool distinct = true) {
        this.QueryDistinct = distinct;

        return (DBQueryType) this;
    }
    #endregion

    #region Query building - Select
    /// <summary>
    /// Adds a SELECT clause to the query.
    /// </summary>
    /// <param name="select">The select clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Select(Select<DBQueryType> select) {
        this.SetQueryType(QueryType.SELECT);

        this.QuerySelect.Add(select);

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds a SELECT clause for a specific field.
    /// </summary>
    /// <param name="field">The field selector.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Select(FieldSelector field) {
        return this.Select(new Select<DBQueryType> {
            Field = field
        });
    }

    /// <summary>
    /// Adds SELECT clauses for all readable columns of the specified type.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Select<T>() where T : class{
        Table table = typeof(T).GetCustomAttribute<Table>();
        if (table != null) {
            string tableName = typeof(T).GetTableName();

            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties()) {
                if (!propertyInfo.IsReadableSystemColumn(this._Engine)) {
                    continue;
                }

                string         columnName = propertyInfo.Name;
                NamedStructure column     = propertyInfo.GetCustomAttribute<NamedStructure>();
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

    /// <summary>
    /// Adds a SELECT clause for a specific property using an expression.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <param name="expression">The property expression.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Select<T>(Expression<Func<T, object>> expression) where T : class {
        string tableName    = typeof(T).GetTableName();
        string propertyName = ExpressionHelper.ExtractPropertyName(expression);
        string columnName   = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            MemberInfo? columnMember = typeof(T).GetMember(propertyName)?.FirstOrDefault();
            if (columnMember != null && columnMember.IsReadableSystemColumn(this._Engine)) {
                return this.Select(new FieldSelector {
                    Table = tableName,
                    Field = columnName
                });
            }
        }

        return (DBQueryType)this;
    }

    /// <summary>
    /// Adds a SELECT clause for a subquery.
    /// </summary>
    /// <param name="subquery">The subquery.</param>
    /// <param name="alias">The alias for the subquery.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Select(Query<DBQueryType> subquery, string alias = null) {
        return this.Select(new Select<DBQueryType> {
            Subquery = subquery,
            Alias    = alias
        });
    }

    /// <summary>
    /// Adds a SELECT clause for a field name.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="escape">Whether to escape the field name.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Select(string fieldName, bool escape = true) {
        return this.Select(new Select<DBQueryType> {
            Field = new FieldSelector(fieldName, escape)
        });
    }

    /// <summary>
    /// Adds SELECT clauses for a list of field names.
    /// </summary>
    /// <param name="fieldNames">The field names.</param>
    /// <param name="escape">Whether to escape the field names.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Select(List<string> fieldNames, bool escape = true) {
        foreach (string fieldName in fieldNames) {
            this.Select(fieldName, escape);
        }

        return (DBQueryType) this;
    }
    #endregion

    #region Query building - Union
    /// <summary>
    /// Adds a UNION clause to the query using the specified <see cref="Union{DBQueryType}"/> object.
    /// </summary>
    /// <param name="union">The <see cref="Union{DBQueryType}"/> object representing the query to union.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Union(Union<DBQueryType> union) {
        this.QueryType = QueryType.SELECT_UNION;
        this.QueryUnion.Add(union);

        return (DBQueryType)this;
    }

    /// <summary>
    /// Adds a UNION clause to the query using the specified query and union type.
    /// </summary>
    /// <param name="query">The query to union with the current query.</param>
    /// <param name="type">The type of union operation (e.g., UNION, UNION ALL, INTERSECT, EXCEPT).</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Union(DBQueryType query, UnionType type = UnionType.UNION) {
        return this.Union(new Union<DBQueryType>() {
            Query = query,
            Type = type
        });
    }

    /// <summary>
    /// Adds a UNION ALL clause to the query using the specified query.
    /// </summary>
    /// <param name="query">The query to union with the current query using UNION ALL.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType UnionAll(DBQueryType query) {
        return this.Union(new Union<DBQueryType>() {
            Query = query,
            Type = UnionType.UNION_ALL
        });
    }

    /// <summary>
    /// Adds an INTERSECT clause to the query using the specified query.
    /// </summary>
    /// <param name="query">The query to intersect with the current query.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Intersect(DBQueryType query) {
        return this.Union(new Union<DBQueryType>() {
            Query = query,
            Type = UnionType.INTERSECT
        });
    }

    /// <summary>
    /// Adds an EXCEPT clause to the query using the specified query.
    /// </summary>
    /// <param name="query">The query to except from the current query.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Except(DBQueryType query) {
        return this.Union(new Union<DBQueryType>() {
            Query = query,
            Type = UnionType.EXCEPT
        });
    }

    /// <summary>
    /// Adds multiple UNION clauses to the query using the specified queries.
    /// </summary>
    /// <param name="queries">An array of queries to union with the current query.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Union(params DBQueryType[] queries) {
        foreach (DBQueryType query in queries) {
            this.Union(query);
        }

        return (DBQueryType)this;
    }

    /// <summary>
    /// Sets an alias for the result of a UNION query and returns the updated query object.
    /// </summary>
    /// <remarks>Use this method to assign a specific alias to the result of a UNION query, which can be
    /// useful when referencing the query result in subsequent operations or when working with complex
    /// queries.</remarks>
    /// <param name="alias">The alias to assign to the UNION query result. Cannot be null or empty.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType UnionAlias(string alias) {
        this.QueryUnionAlias = alias;

        return (DBQueryType) this;
    }
    #endregion

    #region Query building - Set
    /// <summary>
    /// Adds a SET clause for UPDATE queries.
    /// </summary>
    /// <param name="setValue">The SET clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Set(Where<DBQueryType> setValue) {
        this.QuerySet.Add(setValue);

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds multiple SET clauses for UPDATE queries.
    /// </summary>
    /// <param name="setValues">The SET clauses.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Set(List<Where<DBQueryType>> setValues) {
        foreach (Where<DBQueryType> setValue in setValues) {
            this.Set(setValue);
        }

        return (DBQueryType)this;
    }

    /// <summary>
    /// Adds a SET clause for a specific field and value.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="escape">Whether to escape the value.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Set(string fieldName, dynamic value, bool escape = true) {
        return this.Set(new Where<DBQueryType> {
            Field       = new FieldSelector(fieldName),
            Value       = value,
            EscapeValue = escape
        });
    }

    /// <summary>
    /// Adds SET clauses for a dictionary of field-value pairs.
    /// </summary>
    /// <param name="row">The field-value dictionary.</param>
    /// <param name="escape">Whether to escape the values.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Set(Dictionary<string, dynamic> row, bool escape = true) {
        foreach (KeyValuePair<string, dynamic> entry in row) {
            this.Set(entry.Key, entry.Value, escape);
        }

        return (DBQueryType)this;
    }

    /// <summary>
    /// Adds a SET clause for a property using an expression.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <param name="expression">The property expression.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="escape">Whether to escape the value.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Set<T>(Expression<Func<T, object>> expression, dynamic value, bool escape = true) where T : class {
        string tableName    = typeof(T).GetTableName();
        string propertyName = ExpressionHelper.ExtractPropertyName(expression);
        string columnName   = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            MemberInfo? columnMember = typeof(T).GetMember(propertyName)?.FirstOrDefault();
            if (columnMember != null && !columnMember.IsSystemColumn()) {
                return this.Set(
                    columnName,
                    value,
                    escape
                );
            }
        }
        return (DBQueryType)this;
    }
    #endregion

    #region Query building - Values
    /// <summary>
    /// Adds a value dictionary for INSERT queries.
    /// </summary>
    /// <param name="row">The value dictionary.</param>
    /// <param name="skipNullValues">Whether to skip null values.</param>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds a value object for INSERT queries.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="row">The value object.</param>
    /// <param name="skipNullValues">Whether to skip null values.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Value<T>(T row, bool skipNullValues = false) where T : class {
        return this.Value(row.ToDynamicDictionaryForInsert());
    }

    /// <summary>
    /// Adds a value object for INSERT queries using an action.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="action">The action to populate the value object.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Value<T>(Action<T> action) where T : class {
        T row = Activator.CreateInstance<T>();
        if (action != null) {
            action.Invoke(row);
        }

        return this.Value<T>(row);
    }

    /// <summary>
    /// Adds a value object for INSERT queries.
    /// </summary>
    /// <param name="row">The value object.</param>
    /// <param name="skipNullValues">Whether to skip null values.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Value(object row, bool skipNullValues = false) {
        return this.Value(row.ToDynamicDictionaryForInsert());
    }

    /// <summary>
    /// Adds multiple value objects for INSERT queries.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="rows">The list of value objects.</param>
    /// <param name="skipNullValues">Whether to skip null values.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Values<T>(List<T> rows, bool skipNullValues = false) where T : class {
        foreach (T row in rows) {
            this.Value<T>(row, skipNullValues);
        }

        return (DBQueryType)this;
    }

    /// <summary>
    /// Adds multiple value objects for INSERT queries.
    /// </summary>
    /// <param name="rows">The list of value objects.</param>
    /// <param name="skipNullValues">Whether to skip null values.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Values(List<object> rows, bool skipNullValues = false) {
        foreach (object row in rows) {
            this.Value(row, skipNullValues);
        }

        return (DBQueryType)this;
    }
    #endregion

    #region Query Building - On Conflict
    public virtual DBQueryType OnConflict(OnInsertConflict onConflict, string keyColumnName = "") {
        this.QueryOnConflict          = onConflict;
        this.QueryOnConflictKeyColumn = keyColumnName;

        this.Touch();
        return (DBQueryType)this;
    }

    public virtual DBQueryType OnConflict<T>(OnInsertConflict onConflict, Expression<Func<T, object>> expression) where T : class {
        string tableName    = typeof(T).GetTableName();
        string propertyName = ExpressionHelper.ExtractPropertyName(expression);
        string columnName   = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            return this.OnConflict(onConflict, columnName);
        }
        return (DBQueryType)this;
    }
    #endregion

    #region Query building - From/Into
    /// <summary>
    /// Adds a FROM clause to the query.
    /// </summary>
    /// <param name="fromSentence">The FROM clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType From(From<DBQueryType> fromSentence) {
        this.QueryFrom.Add(fromSentence);

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds a FROM clause for a specific table type.
    /// </summary>
    /// <typeparam name="TableClass">The table type.</typeparam>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds a FROM clause for a table name.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType From(string tableName) {
        return this.From(new From<DBQueryType> {
            Table       = tableName,
            EscapeTable = true
        });
    }

    /// <summary>
    /// Adds a subquery as a FROM clause.
    /// </summary>
    /// <param name="subquery">The subquery.</param>
    /// <param name="alias">The subquery alias.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType From(DBQueryType subquery, string alias = "") {
        return this.From(new From<DBQueryType> {
            Subquery   = subquery,
            TableAlias = alias
        });
    }

    /// <summary>
    /// Adds an INTO clause to the query.
    /// </summary>
    /// <param name="fromSentence">The INTO clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Into(From<DBQueryType> fromSentence) {
        return this.From(fromSentence);
    }

    /// <summary>
    /// Adds an INTO clause for a specific table type.
    /// </summary>
    /// <typeparam name="TableClass">The table type.</typeparam>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Into<TableClass>() {
        return this.From<TableClass>();
    }

    /// <summary>
    /// Adds an INTO clause for a table name.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Into(string tableName) {
        return this.From(tableName);
    }
    #endregion

    #region Query building - Join
    /// <summary>
    /// Adds a JOIN clause to the query.
    /// </summary>
    /// <param name="sentence">The JOIN clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Join(Join<DBQueryType> sentence) {
        this.QueryJoin.Add(sentence);

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds a JOIN clause using expressions for joined and parent tables.
    /// </summary>
    /// <typeparam name="JoinedTable">The joined table type.</typeparam>
    /// <typeparam name="ParentTable">The parent table type.</typeparam>
    /// <param name="joinedTableExpression">The joined table property expression.</param>
    /// <param name="parentTableExpression">The parent table property expression.</param>
    /// <param name="comparer">The comparison operator.</param>
    /// <param name="direction">The join direction.</param>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds a JOIN clause for a table name and condition.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="condition">The join condition.</param>
    /// <param name="direction">The join direction.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Join(string tableName, Where<DBQueryType> condition, JoinDirection direction = JoinDirection.NONE) {
        return this.Join(new Join<DBQueryType> {
            Direction   = direction,
            Table       = tableName,
            EscapeTable = true,
            Condition   = condition
        });
    }

    /// <summary>
    /// Adds a JOIN clause for table and field names.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="fieldLeft">The left field name.</param>
    /// <param name="fieldRight">The right field name.</param>
    /// <param name="comparer">The comparison operator.</param>
    /// <param name="direction">The join direction.</param>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds a JOIN clause for table and field names with table references.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="tableLeft">The left table name.</param>
    /// <param name="fieldLeft">The left field name.</param>
    /// <param name="tableRight">The right table name.</param>
    /// <param name="fieldRight">The right field name.</param>
    /// <param name="comparer">The comparison operator.</param>
    /// <param name="direction">The join direction.</param>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds a JOIN clause for table and field selectors.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="left">The left field selector.</param>
    /// <param name="right">The right field selector.</param>
    /// <param name="comparer">The comparison operator.</param>
    /// <param name="direction">The join direction.</param>
    /// <returns>The current query instance.</returns>
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
    /// <summary>
    /// Adds a WHERE clause to the query.
    /// </summary>
    /// <param name="whereSentence">The WHERE clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Where(Where<DBQueryType> whereSentence) {
        this.QueryWhere.Add(whereSentence);

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds a WHERE clause for a field selector and value.
    /// </summary>
    /// <param name="left">The field selector.</param>
    /// <param name="fieldValue">The value to compare.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Where(FieldSelector left, dynamic fieldValue) {
        return this.Where(new Where<DBQueryType> {
            Field = left,
            Value = fieldValue
        });
    }

    /// <summary>
    /// Adds a WHERE clause for a field name and value.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="fieldValue">The value to compare.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Where(string fieldName, dynamic fieldValue) {
        return this.Where(new Where<DBQueryType> {
            Field  = new FieldSelector(fieldName),
            Value = fieldValue
        });
    }

    /// <summary>
    /// Adds a WHERE clause for a property using an expression.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <param name="expression">The property expression.</param>
    /// <param name="value">The value to compare.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Where<T>(Expression<Func<T, object>> expression, dynamic value, bool escape = true) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            return this.Where(new FieldSelector {
                    Table  = tableName,
                    Field  = columnName,
                    Escape = escape
                },
                value
            );
        }
        return (DBQueryType)this;
    }

    /// <summary>
    /// Adds a WHERE clause for a property using an expression.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <param name="expression">The property expression.</param>
    /// <param name="where">The filtering condition to apply, including the field and comparison logic.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Where<T>(Expression<Func<T, object>> expression, Where<DBQueryType> where) where T : class {
        string tableName  = typeof(T).GetTableName();
        string columnName = ExpressionHelper.ExtractColumnName<T>(expression);

        if (!string.IsNullOrWhiteSpace(columnName)) {
            where.Field = new FieldSelector(tableName, columnName, true);

            return this.Where(where);
        }
        return (DBQueryType)this;
    }

    /// <summary>
    /// Adds a WHERE clause comparing two field selectors.
    /// </summary>
    /// <param name="left">The left field selector.</param>
    /// <param name="right">The right field selector.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Where(FieldSelector left, FieldSelector right) {
        return this.Where(new Where<DBQueryType> {
            Field      = left,
            ValueField = right
        });
    }
    #endregion

    #region Query building - WhereIn
    /// <summary>
    /// Adds a WHERE IN clause to the query.
    /// </summary>
    /// <param name="whereInSentence">The WHERE IN clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType WhereIn(WhereIn<DBQueryType> whereInSentence) {
        this.QueryWhereIn.Add(whereInSentence);

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds a WHERE IN clause for a field selector and values.
    /// </summary>
    /// <param name="left">The field selector.</param>
    /// <param name="fieldValues">The list of values.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType WhereIn(FieldSelector left, List<dynamic> fieldValues) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field  = left,
            Values = fieldValues
        });
    }

    /// <summary>
    /// Adds a WHERE IN clause for a field name and values.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="fieldValues">The list of values.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType WhereIn(string fieldName, List<dynamic> fieldValues) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field  = new FieldSelector(fieldName), 
            Values = fieldValues
        });
    }

    /// <summary>
    /// Adds a WHERE IN clause for a property using an expression and values.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <param name="expression">The property expression.</param>
    /// <param name="fieldValues">The list of values.</param>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds a WHERE IN clause for a field name and subquery.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="subquery">The subquery.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType WhereIn(string fieldName, Query<DBQueryType> subquery) {
        return this.WhereIn(new WhereIn<DBQueryType> {
            Field    = new FieldSelector(fieldName),
            Subquery = subquery
        });
    }
    #endregion

    #region Query building - Where LIKE
    /// <summary>
    /// Adds a WHERE LIKE clause for a field selector and value.
    /// </summary>
    /// <param name="left">The field selector.</param>
    /// <param name="fieldValue">The value to compare.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType WhereLike(FieldSelector left, string fieldValue) {
        return this.Where(new Where<DBQueryType> {
            Field       = left,
            Value       = fieldValue,
            Comparer    = WhereComparer.LIKE,
            EscapeValue = true
        });
    }

    /// <summary>
    /// Adds a WHERE LIKE clause for a property using an expression and value.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <param name="expression">The property expression.</param>
    /// <param name="value">The value to compare.</param>
    /// <param name="escape">Whether to escape the value.</param>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds a WHERE LIKE clause for a field name and value.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="fieldValue">The value to compare.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType WhereLike(string fieldName, string fieldValue) {
        return this.WhereLike(
            new FieldSelector(fieldName),
            fieldValue
        );
    }

    /// <summary>
    /// Adds a WHERE LIKE clause for a field name and value, matching the left side.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="fieldValue">The value to compare.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType WhereLikeLeft(string fieldName, string fieldValue) {
        return this.WhereLike(
            new FieldSelector(fieldName),
            $"%{fieldValue.TrimStart('%')}"
        );
    }

    /// <summary>
    /// Adds a WHERE LIKE clause for a field name and value, matching the right side.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="fieldValue">The value to compare.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType WhereLikeRight(string fieldName, string fieldValue) {
        return this.WhereLike(
            new FieldSelector(fieldName),
            $"{fieldValue.TrimEnd('%')}%"
        );
    }
    #endregion

    #region Query building - Group By
    /// <summary>
    /// Adds a GROUP BY clause to the query.
    /// </summary>
    /// <param name="group">The GROUP BY clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType GroupBy(GroupBy group) {
        this.QueryGroup.Add(group);

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds a GROUP BY clause for a field selector.
    /// </summary>
    /// <param name="field">The field selector.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType GroupBy(FieldSelector field) {
        return this.GroupBy(new GroupBy {
            Field = field
        });
    }

    /// <summary>
    /// Adds a GROUP BY clause for a property using an expression.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <param name="expression">The property expression.</param>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds a GROUP BY clause for a field name and optional table name.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="escape">Whether to escape the field name.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType GroupBy(string fieldName, string tableName = null, bool escape = true) {
        return this.GroupBy(new FieldSelector(tableName, fieldName, escape));
    }
    #endregion

    #region Query building - Order By
    /// <summary>
    /// Adds an ORDER BY clause to the query.
    /// </summary>
    /// <param name="orderSentence">The ORDER BY clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType OrderBy(OrderBy orderSentence) {
        this.QueryOrder.Add(orderSentence);

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds an ORDER BY clause for a field selector and direction.
    /// </summary>
    /// <param name="field">The field selector.</param>
    /// <param name="direction">The order direction.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType OrderBy(FieldSelector field, OrderDirection direction = OrderDirection.ASC) {
        return this.OrderBy(new OrderBy {
            Field     = field,
            Direction = direction
        });
    }

    /// <summary>
    /// Adds an ORDER BY clause for a property using an expression and direction.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <param name="expression">The property expression.</param>
    /// <param name="direction">The order direction.</param>
    /// <returns>The current query instance.</returns>
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

    /// <summary>
    /// Adds an ORDER BY clause for a field name and direction.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="direction">The order direction.</param>
    /// <param name="escapeField">Whether to escape the field name.</param>
    /// <returns>The current query instance.</returns>
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
    /// <summary>
    /// Adds a LIMIT clause to the query.
    /// </summary>
    /// <param name="limitSentence">The LIMIT clause.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Limit(Limit limitSentence) {
        this.QueryLimit = limitSentence;

        this.Touch();
        return (DBQueryType) this;
    }

    /// <summary>
    /// Adds a LIMIT clause with count and offset.
    /// </summary>
    /// <param name="count">The number of rows to limit.</param>
    /// <param name="offset">The offset for the limit.</param>
    /// <returns>The current query instance.</returns>
    public virtual DBQueryType Limit(long count, long offset = 0) {
        return this.Limit(new Limit {
            Count  = count,
            Offset = offset
        });
    }
    #endregion

    #region Query building - Create
    /// <summary>
    /// Sets the query to CREATE TABLE for the specified type.
    /// </summary>
    /// <param name="tableType">The table type.</param>
    /// <returns>The current query instance.</returns>
    public DBQueryType CreateTable(Type tableType) {
        this.SetQueryType(QueryType.CREATE);

        this.QueryCreate = tableType;

        this.Touch();
        return (DBQueryType)this;
    }

    /// <summary>
    /// Sets the query to CREATE TABLE for the specified type.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <returns>The current query instance.</returns>
    public DBQueryType CreateTable<T>() where T : class {
        return this.CreateTable(typeof(T));
    }

    /// <summary>
    /// Sets the query to CREATE TABLE for the specified type.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <returns>The current query instance.</returns>
    public DBQueryType Create<T>() where T : class {
        return this.CreateTable<T>();
    }
    #endregion

    #region Query building - Raw
    /// <summary>
    /// Sets the query to RAW with the specified query and prepared string.
    /// </summary>
    /// <param name="query">The raw query string.</param>
    /// <param name="queryPreparedString">The prepared query string.</param>
    /// <returns>The current query instance.</returns>
    public DBQueryType Raw(string query, string queryPreparedString) {
        this.SetQueryType(QueryType.RAW);

        this.QueryRenderedString = query;
        this.QueryPreparedString = queryPreparedString;

        this.Untouch();
        return (DBQueryType)this;
    }

    /// <summary>
    /// Sets the query to RAW with the specified query string.
    /// </summary>
    /// <param name="query">The raw query string.</param>
    /// <returns>The current query instance.</returns>
    public DBQueryType Raw(string query) {
        return this.Raw(query, query);
    }

    /// <summary>
    /// Sets the query to RAW with the specified query string and prepared data.
    /// </summary>
    /// <param name="query">The raw query string.</param>
    /// <param name="queryPreparedData">The prepared data dictionary.</param>
    /// <returns>The current query instance.</returns>
    public DBQueryType Raw(string query, Dictionary<string, PreparedValue> queryPreparedData) {
        this.SetQueryType(QueryType.RAW);

        this.QueryPreparedData = queryPreparedData;

        return this.Raw(query);
    }

    /// <summary>
    /// Sets the query to RAW with the specified query string and prepared data.
    /// </summary>
    /// <param name="query">The raw query string.</param>
    /// <param name="queryPreparedData">The prepared data dictionary.</param>
    /// <returns>The current query instance.</returns>
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
    /// <summary>
    /// Renders the COUNT sentence for the query.
    /// </summary>
    /// <returns>The COUNT sentence string.</returns>
    protected virtual string _RenderCountSentence()        { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the SELECT sentence for the query.
    /// </summary>
    /// <returns>The SELECT sentence string.</returns>
    protected virtual string _RenderSelectSentence()       { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the FROM sentence for the query.
    /// </summary>
    /// <returns>The FROM sentence string.</returns>
    protected virtual string _RenderFromSentence()         { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the JOIN sentence for the query.
    /// </summary>
    /// <returns>The JOIN sentence string.</returns>
    protected virtual string _RenderJoinSentence()         { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the WHERE sentence for the query.
    /// </summary>
    /// <returns>The WHERE sentence string.</returns>
    protected virtual string _RenderWhereSentence()        { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the UNION sentence for the query.
    /// </summary>
    /// <returns>The UNION sentence string.</returns>
    protected virtual string _RenderUnionSentence()       { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the GROUP BY sentence for the query.
    /// </summary>
    /// <returns>The GROUP BY sentence string.</returns>
    protected virtual string _RenderGroupSentence()        { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the HAVING sentence for the query.
    /// </summary>
    /// <returns>The HAVING sentence string.</returns>
    protected virtual string _RenderHavingSentence()       { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the ORDER sentence for the query.
    /// </summary>
    /// <returns>The ORDER sentence string.</returns>
    protected virtual string _RenderOrderSentence()        { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the LIMIT sentence for the query.
    /// </summary>
    /// <returns>The LIMIT sentence string.</returns>
    protected virtual string _RenderLimitSentence()        { throw new NotImplementedException(); }

    /// <summary>
    /// Renders any extra SELECT sentence for the query.
    /// </summary>
    /// <returns>The extra SELECT sentence string.</returns>
    protected virtual string _RenderSelectExtraSentence()  { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the DELETE sentence for the query.
    /// </summary>
    /// <returns>The DELETE sentence string.</returns>
    protected virtual string _RenderDeleteSentence()       { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the UPDATE sentence for the query.
    /// </summary>
    /// <returns>The UPDATE sentence string.</returns>
    protected virtual string _RenderUpdateSentence()       { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the SET sentence for the query.
    /// </summary>
    /// <returns>The SET sentence string.</returns>
    protected virtual string _RenderSetSentence()          { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the INSERT INTO sentence for the query.
    /// </summary>
    /// <returns>The INSERT INTO sentence string.</returns>
    protected virtual string _RenderInsertIntoSentence()   { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the INSERT VALUES sentence for the query.
    /// </summary>
    /// <returns>The INSERT VALUES sentence string.</returns>
    protected virtual string _RenderInsertValuesSentence() { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the ON CONFLICT/ON DUPLICATE KEY sentence for the query.
    /// </summary>
    /// <returns>The ON CONFLICT/ON DUPLICATE KEY sentence string.</returns>
    protected virtual string _RenderInsertOnConflictSentence() { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the CREATE sentence for the specified table type.
    /// </summary>
    /// <typeparam name="T">The table type.</typeparam>
    /// <returns>The CREATE sentence string.</returns>
    protected virtual string _RenderCreateSentence<T>() where T : Table { throw new NotImplementedException(); }

    /// <summary>
    /// Renders the CREATE sentence for the specified table type.
    /// </summary>
    /// <param name="tableType">The table type.</param>
    /// <returns>The CREATE sentence string.</returns>
    protected virtual string _RenderCreateSentence(Type tableType)      { throw new NotImplementedException(); }
    #endregion

    #region Helpers
    /// <summary>
    /// Gets the <see cref="ColumnDataType"/> for a given type string.
    /// </summary>
    /// <param name="typeString">The type string.</param>
    /// <returns>The corresponding <see cref="ColumnDataType"/>, or null if not found.</returns>
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