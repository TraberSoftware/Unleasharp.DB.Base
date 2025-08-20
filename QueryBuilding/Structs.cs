using System.ComponentModel;

namespace Unleasharp.DB.Base.QueryBuilding;

#region Query enums
public enum QueryType {
    [Description("")]
    NONE,

    // Non-specific query
    [Description("RAW")]
    RAW,

    // Row queries
    [Description("SELECT")]
    SELECT,
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
}

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

public enum WhereOperator {
    [Description("")]
    NONE,
    [Description("AND")]
    AND,
    [Description("OR")]
    OR
}

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

public enum OrderDirection {
    [Description("")]
    NONE,
    [Description("ASC")]
    ASC,
    [Description("DESC")]
    DESC
}

public enum IndexExtension {
    [Description("")]
    NONE,
    [Description("FULLTEXT")]
    FULLTEXT,
    [Description("SPATIAL")]
    SPATIAL
}

public enum IndexType {
    [Description("")]
    NONE,
    [Description("BTREE")]
    BTREE,
    [Description("HASH")]
    HASH
}

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
