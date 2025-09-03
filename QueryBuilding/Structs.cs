using System.ComponentModel;

namespace Unleasharp.DB.Base.QueryBuilding;

#region Query enums

/// <summary>
/// Represents the types of queries that can be executed in a database context.
/// </summary>
/// <remarks>This enumeration defines various query types, including general-purpose queries (e.g., <see
/// cref="RAW"/>), row-level operations (e.g., <see cref="SELECT"/>, <see cref="INSERT"/>), and table-level operations
/// (e.g., <see cref="CREATE_TABLE"/>). Each query type corresponds to a specific kind of database operation.</remarks>
public enum QueryType {
    [Description("")]
    NONE,

    // Non-specific query
    [Description("RAW")]
    RAW,

    // Row queries
    [Description("SELECT")]
    SELECT,
    [Description("SELECT_UNION")]
    SELECT_UNION,
    [Description("INSERT")]
    INSERT,
    [Description("UPDATE")]
    UPDATE,
    [Description("DELETE")]
    DELETE,

    // Special queries
    [Description("COUNT")]
    COUNT,

    // Table queries
    [Description("CREATE")]
    CREATE,
    [Description("CREATE_TABLE")]
    CREATE_TABLE,
    [Description("CREATE_TYPE")]
    CREATE_TYPE,
}

/// <summary>
/// Specifies the comparison operators that can be used in filtering or query conditions.
/// </summary>
/// <remarks>This enumeration defines a set of comparison operators commonly used in query expressions. Each
/// member corresponds to a specific operator, such as equality, inequality, or pattern matching. The <see
/// cref="DescriptionAttribute"/> applied to each member provides the textual representation of the operator, which can
/// be useful for generating query strings or debugging.</remarks>
public enum WhereComparer {
    [Description("")]
    NONE,
    [Description("=")]
    EQUALS,
    [Description("<>")]
    NOT_EQUALS,
    [Description(">")]
    GREATER,
    [Description(">=")]
    GREATER_EQUALS,
    [Description("<")]
    LOWER,
    [Description("<=")]
    LOWER_EQUALS,
    [Description("NOT LIKE")]
    NOT_LIKE,
    [Description("LIKE")]
    LIKE,
    [Description("LIKE")]
    LIKE_LEFT,
    [Description("LIKE")]
    LIKE_RIGHT,
    [Description("IS")]
    IS,
    [Description("IS NOT")]
    IS_NOT,
    [Description("IN")]
    IN,
    [Description("NOT IN")]
    NOT_IN
}

/// <summary>
/// Specifies the logical operators that can be used to combine conditions in a query.
/// </summary>
/// <remarks>This enumeration defines the operators available for combining multiple conditions in a query. Use
/// <see cref="AND"/> to require all conditions to be true, <see cref="OR"/> to require at least one condition to be
/// true,  or <see cref="NONE"/> when no operator is specified.</remarks>
public enum WhereOperator {
    [Description("")]
    NONE,
    [Description("AND")]
    AND,
    [Description("OR")]
    OR
}

/// <summary>
/// Specifies the direction or type of a join operation in a database query.
/// </summary>
/// <remarks>This enumeration defines various types of join operations that can be used in query construction.
/// Each value corresponds to a specific join type, such as INNER, OUTER, LEFT, or RIGHT joins.</remarks>
public enum JoinDirection {
    [Description("")]
    NONE,
    [Description("INNER")]
    INNER,
    [Description("OUTER")]
    OUTER,
    [Description("LEFT")]
    LEFT,
    [Description("LEFT OUTER")]
    LEFT_OUTER,
    [Description("RIGHT")]
    RIGHT,
    [Description("RIGHT OUTER")]
    RIGHT_OUTER,
    [Description("STRAIGHT")]
    STRAIGHT,
    [Description("CROSS")]
    CROSS
}

/// <summary>
/// Specifies the type of UNION operation to perform between two SELECT queries.
/// </summary>
/// <remarks>
/// This enumeration defines the available set operations for combining the results of multiple SELECT statements.
/// <see cref="UNION"/> returns distinct rows from both queries, <see cref="UNION_ALL"/> returns all rows including duplicates,
/// <see cref="INTERSECT"/> returns only rows present in both queries, and <see cref="EXCEPT"/> returns rows from the first query that are not present in the second.
/// </remarks>
public enum UnionType {
    [Description("")]
    NONE,
    [Description("UNION")]
    UNION,
    [Description("UNION ALL")]
    UNION_ALL,
    [Description("INTERSECT")]
    INTERSECT,
    [Description("EXCEPT")]
    EXCEPT
}

/// <summary>
/// Specifies the direction of sorting for an order operation.
/// </summary>
/// <remarks>This enumeration is used to indicate whether the sorting should be in ascending or descending order,
/// or if no specific sorting direction is applied.</remarks>
public enum OrderDirection {
    [Description("")]
    NONE,
    [Description("ASC")]
    ASC,
    [Description("DESC")]
    DESC
}

/// <summary>
/// Specifies the type of index extension to be applied to a database index.
/// </summary>
/// <remarks>This enumeration is used to define additional characteristics or behaviors for database indexes. The
/// values correspond to specific index extensions, such as full-text search or spatial indexing.</remarks>
public enum IndexExtension {
    [Description("")]
    NONE,
    [Description("FULLTEXT")]
    FULLTEXT,
    [Description("SPATIAL")]
    SPATIAL
}

/// <summary>
/// Specifies the type of index used in a database or data structure.
/// </summary>
/// <remarks>This enumeration defines the available index types, such as <see cref="BTREE"/> and <see
/// cref="HASH"/>,  which can be used to optimize data retrieval operations. The <see cref="NONE"/> value indicates that
/// no index is applied.</remarks>
public enum IndexType {
    [Description("")]
    NONE,
    [Description("BTREE")]
    BTREE,
    [Description("HASH")]
    HASH
}

/// <summary>
/// Represents the type of a database key, such as primary, unique, or foreign.
/// </summary>
/// <remarks>This enumeration is used to specify the role of a key in a database schema.  For example, a primary
/// key uniquely identifies a record, while a foreign key establishes a relationship between tables.</remarks>
public enum KeyType {
    [Description("")]
    NONE,
    [Description("PRIMARY")]
    PRIMARY,
    [Description("UNIQUE")]
    UNIQUE,
    [Description("FOREIGN")]
    FOREIGN
}
#endregion
