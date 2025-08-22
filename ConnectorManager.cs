using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Threading;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base;

/// <summary>
/// The ConnectorManager class handles the DB connections with the given provider,
/// ensures the connections are alive and regenerates them if needed.
/// 
/// It ensures thread-safety by spawning thread-owned connections
/// </summary>
/// <typeparam name="DBConnectorType">The Type of the DB Connector to spawn connections of</typeparam>
public class ConnectorManager<DBConnectorManagerType, DBConnectorType, DBConnectorSettingsType, DBConnectionType, DBQueryBuilderType, DBQueryType> 
    where DBConnectorType         : Connector<DBConnectorType, DBConnectorSettingsType>
    where DBConnectorSettingsType : DbConnectionStringBuilder
    where DBConnectorManagerType  : ConnectorManager<DBConnectorManagerType, DBConnectorType, DBConnectorSettingsType, DBConnectionType, DBQueryBuilderType, DBQueryType>
    where DBConnectionType        : DbConnection
	where DBQueryBuilderType      : QueryBuilder<DBQueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType>
    where DBQueryType             : Query<DBQueryType>
{
    private Dictionary<long, DBConnectorType> _connectorStack = new Dictionary<long, DBConnectorType>();

    public DBConnectorSettingsType ConnectionStringBuilder { get; protected set; }
    public string                  ConnectionString        { get; protected set; }


    #region Automatic connection management settings
    public bool     AutomaticConnectionRenewal         { get; private set; } = true;
    public TimeSpan AutomaticConnectionRenewalInterval { get; private set; } = TimeSpan.FromSeconds(900);
	#endregion

	#region Query control callbacks
	public Action<Exception> OnQueryExceptionAction {
		get; private set;
	}
	#endregion

    #region Setup actions
    public Action<DBConnectionType> ConnectionSetupAction {
	    get; private set;
    }

    public Func<DBQueryBuilderType,      DBQueryBuilderType> QueryBuilderInstantiationFunction {
        get; private set;
    }

    public Func<DBConnectorSettingsType, DBConnectorType>    DBConnectionFromStringBuilderInstantiationFunction {
        get; private set;
    }

    public Func<string,                  DBConnectorType>    DBConnectionFromStringInstantiationFunction {
        get; private set;
    }
    #endregion

	/// <summary>
	/// Initializes a new instance of ConnectorManager with default connection settings.
	/// </summary>
	public ConnectorManager() { }

	/// <summary>
	/// Initializes a new instance of ConnectorManager with a custom connection string builder.
	/// </summary>
	/// <param name="stringBuilder">The connection string builder containing configuration details</param>
	public ConnectorManager(DBConnectorSettingsType connectionStringBuilder) {
        this.ConnectionStringBuilder = connectionStringBuilder;
    }

	/// <summary>
	/// Initializes a new instance of ConnectorManager with a raw connection string.
	/// </summary>
	/// <param name="connectionString">The full MySQL connection string</param>
	public ConnectorManager(string connectionString) {
        this.ConnectionString = connectionString;
    }

	/// <summary>
	/// Creates a query builder instance using the current connection manager's attached instance.
	/// The resulting builder can execute queries against the current database connection.
	/// </summary>
	/// <returns>A new QueryBuilder instance bound to the current connection</returns>
	public virtual DBQueryBuilderType QueryBuilder() {
        return ((DBQueryBuilderType) Activator.CreateInstance(typeof(DBQueryBuilderType), new object[] { this.GetInstance() }))
            .WithOnQueryExceptionAction(this.OnQueryExceptionAction)
        ;
	}

	/// <summary>
	/// Creates a query builder instance using a detached connection instance.
	/// This allows queries to be built independently of the current connection context,
	/// useful for scenarios requiring isolated query execution.
	/// </summary>
	/// <returns>A new QueryBuilder instance bound to a detached connection</returns>
	public virtual DBQueryBuilderType DetachedQueryBuilder() {
		return ((DBQueryBuilderType) Activator.CreateInstance(typeof(DBQueryBuilderType), new object[] { this.GetDetachedInstance() }))
			.WithOnQueryExceptionAction(this.OnQueryExceptionAction)
		;
	}

	/// <summary>
	/// Creates a query builder instance using the current connection manager's attached instance
	/// and a pre-defined query object.
	/// </summary>
	/// <param name="query">The query object to initialize with</param>
	/// <returns>A new QueryBuilder instance bound to the current connection and initialized with the query</returns>
	public virtual DBQueryBuilderType QueryBuilder(DBQueryType query) {
		return ((DBQueryBuilderType)Activator.CreateInstance(typeof(DBQueryBuilderType), new object[] { this.GetInstance(), query }))
			.WithOnQueryExceptionAction(this.OnQueryExceptionAction)
		;
	}

	/// <summary>
	/// Creates a query builder instance using a detached connection instance and a pre-defined query object.
	/// </summary>
	/// <param name="query">The query object to initialize with</param>
	/// <returns>A new QueryBuilder instance bound to a detached connection and initialized with the query</returns>
	public virtual DBQueryBuilderType DetachedQueryBuilder(DBQueryType query) {
		return ((DBQueryBuilderType)Activator.CreateInstance(typeof(DBQueryBuilderType), new object[] { this.GetDetachedInstance(), query }))
			.WithOnQueryExceptionAction(this.OnQueryExceptionAction)
		;
	}

	public DBConnectorManagerType Configure(Action<DBConnectorSettingsType> action) {
        this.ConnectionStringBuilder = (DBConnectorSettingsType) Activator.CreateInstance<DBConnectorSettingsType>();

        action.Invoke(this.ConnectionStringBuilder);

        return (DBConnectorManagerType) this;
    }

    #region Setup methods
    public DBConnectorManagerType WithAutomaticConnectionRenewal(bool enabled = true) {
        this.AutomaticConnectionRenewal = enabled;

        return (DBConnectorManagerType) this;
    }

    public DBConnectorManagerType WithAutomaticConnectionRenewalInterval(TimeSpan interval) {
        this.AutomaticConnectionRenewalInterval = interval;

        return (DBConnectorManagerType) this;
    }

	public DBConnectorManagerType WithOnQueryExceptionAction(Action<Exception> onQueryExceptionAction) {
		this.OnQueryExceptionAction = onQueryExceptionAction;

		return (DBConnectorManagerType) this;
	}

	public DBConnectorManagerType WithConnectionSetup(Action<DBConnectionType> action) {
		this.ConnectionSetupAction = action;

		return (DBConnectorManagerType) this;
	}
	#endregion

	#region DB Connection handler
	/// <summary>
	/// Generate a detacched and not-managed instance of the DBType DB Connector,
	/// mostly intended to be used as a single-use connection
	/// </summary>
	/// <returns>A brand-new DBType instance, initialized and ready to run queries</returns>
	public DBConnectorType GetDetachedInstance() {
        DBConnectorType dBconnector = this.__GenerateDBTypeInstance();
        this.__Initialize(dBconnector);

        return dBconnector;
    }

    /// <summary>
    /// Return a DBType DB Connector instance for the current thread ID.
    /// The thread ID is retrieved by Thread.CurrentThread.ManagedThreadId
    /// This adds up to ensure thead-safety of the requests
    /// </summary>
    /// <returns>The DBType instance for the current thread</returns>
    public DBConnectorType GetInstance() {
        DBConnectorType dBconnector = null;
        long            threadId    = Thread.CurrentThread.ManagedThreadId;

        if (this._connectorStack.ContainsKey(threadId)) {
            dBconnector = this._connectorStack[threadId];
        }

        // Instance exists, but is disconnected
        if (dBconnector != null && !dBconnector.Connected) {
            dBconnector = null;
            this._connectorStack.Remove(threadId);
        }

        // Instance does not exist, create it
        if (dBconnector == null) {
            dBconnector = this.__GenerateDBTypeInstance();

            this._connectorStack.Add(threadId, dBconnector);
        }

        this.__Initialize(dBconnector);

        return dBconnector;
    }

    /// <summary>
    /// Initialize the DBType DB connection, ensuring that it is connected
    /// or reconnecting if the connection meets the AutomaticRenewalInterval limit
    /// </summary>
    /// <typeparam name="DBType">DB Connector Type</typeparam>
    /// <param name="dbConnector">The DBConnector to initialize</param>
    public void __Initialize<DBType>(DBType dbConnector) where DBType : Unleasharp.DB.Base.Connector<DBType, DBConnectorSettingsType> {
        bool force = (this.AutomaticConnectionRenewal && this.AutomaticConnectionRenewalInterval.TotalMilliseconds > 0)
            ? DateTime.UtcNow - dbConnector.ConnectionTimestamp >= this.AutomaticConnectionRenewalInterval
            : false
        ;

        dbConnector.Connect(force);
    }

    /// <summary>
    /// Creates an instance of the <see cref="DBConnectorType"/> class using the current connection settings.
    /// </summary>
    /// <remarks>The method attempts to initialize a <see cref="DBConnectorType"/> instance using either the 
    /// <see cref="ConnectionString"/> or <see cref="Settings"/> property, depending on which is available. If
    /// neither is set, the method returns <see langword="null"/>.</remarks>
    /// <returns>An instance of the <see cref="DBConnectorType"/> class initialized with the connection settings, 
    /// or <see langword="null"/> if no valid settings are provided.</returns>
    protected virtual DBConnectorType __GenerateDBTypeInstance() {
        object settingsSource = !string.IsNullOrWhiteSpace(this.ConnectionString) ? this.ConnectionString : this.ConnectionStringBuilder;

        if (settingsSource != null) {
            return (DBConnectorType) Activator.CreateInstance(typeof(DBConnectorType), new object[] { settingsSource });
        }

        return null;
    }
    #endregion
}