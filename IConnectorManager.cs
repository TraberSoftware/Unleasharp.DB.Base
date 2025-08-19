using System.Data.Common;
using Unleasharp.DB.Base;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base {
    public interface IConnectorManager<QueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType>
        where DBConnectionType        : DbConnection
        where DBConnectorSettingsType : DbConnectionStringBuilder
        where DBConnectorType         : Connector<DBConnectorType, DBConnectorSettingsType>
        where DBQueryType             : Query<DBQueryType>
        where QueryBuilderType        : QueryBuilder<QueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType>
    {
        public QueryBuilderType QueryBuilder();
    }
}
