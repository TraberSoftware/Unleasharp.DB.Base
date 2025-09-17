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
/// It ensures thread-safety by spawning thread-owned connections.
/// </summary>
/// <typeparam name="DBConnectorType">The Type of the DB Connector to spawn connections of</typeparam>
public class ConnectorManager<DBConnectorManagerType, DBConnectorType, DBConnectorSettingsType, DBConnectionType, DBQueryBuilderType, DBQueryType> 
    where DBConnectionType        : DbConnection
    where DBConnectorSettingsType : DbConnectionStringBuilder
    where DBConnectorType         : Connector<DBConnectorType, DBConnectionType, DBConnectorSettingsType>
    where DBConnectorManagerType  : ConnectorManager<DBConnectorManagerType, DBConnectorType, DBConnectorSettingsType, DBConnectionType, DBQueryBuilderType, DBQueryType>
    where DBQueryBuilderType      : QueryBuilder<DBQueryBuilderType, DBConnectorType, DBQueryType, DBConnectionType, DBConnectorSettingsType>
    where DBQueryType             : Query<DBQueryType>
{
    private object                               _connectorStackLock    = new object();
    private Dictionary<long, DBConnectorType>    _connectorStack        = new Dictionary<long, DBConnectorType>();
    private object                               _queryBuilderStackLock = new object();
    private Dictionary<long, DBQueryBuilderType> _queryBuilderStack     = new Dictionary<long, DBQueryBuilderType>();

    /// <summary>
    /// Gets or sets the connection string builder used for configuring the database connection.
    /// </summary>
    public DBConnectorSettingsType ConnectionStringBuilder { get; protected set; }
    /// <summary>
    /// Gets or sets the raw connection string used for the database connection.
    /// </summary>
    public string                  ConnectionString        { get; protected set; }

    #region Automatic connection management settings
    /// <summary>
    /// Gets a value indicating whether automatic connection renewal is enabled.
    /// </summary>
    public bool     AutomaticConnectionRenewal         { get; private set; } = true;
    /// <summary>
    /// Gets the interval for automatic connection renewal.
    /// </summary>
    public TimeSpan AutomaticConnectionRenewalInterval { get; private set; } = TimeSpan.FromSeconds(900);
    #endregion

    #region Query control callbacks
    /// <summary>
    /// Gets or sets the action to execute when a query exception occurs.
    /// </summary>
    public Action<DBQueryType, Exception> OnQueryExceptionAction {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the action to execute before query execution.
    /// </summary>
    public Action<DBQueryType> BeforeQueryExecutionAction {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the action to execute after query execution.
    /// </summary>
    public Action<DBQueryType> AfterQueryExecutionAction {
        get; private set;
    }

    /// <summary>
    /// Sets the action to execute when a query exception occurs.
    /// </summary>
    /// <param name="onQueryExceptionAction">The exception action.</param>
    /// <returns>The current connector manager instance.</returns>
    public DBConnectorManagerType WithOnQueryExceptionAction(Action<DBQueryType, Exception> onQueryExceptionAction) {
        this.OnQueryExceptionAction = onQueryExceptionAction;

        return (DBConnectorManagerType) this;
    }

    /// <summary>
    /// Sets the action to execute before query execution.
    /// </summary>
    /// <param name="beforeQueryExecutionAction">The action to execute before query execution.</param>
    /// <returns>The current connector manager instance.</returns>
    public DBConnectorManagerType WithBeforeQueryExecutionAction(Action<DBQueryType> beforeQueryExecutionAction) {
        this.BeforeQueryExecutionAction = beforeQueryExecutionAction;

        return (DBConnectorManagerType) this;
    }

    /// <summary>
    /// Sets the action to execute after query execution.
    /// </summary>
    /// <param name="afterQueryExecutionAction">The action to execute after query execution.</param>
    /// <returns>The current connector manager instance.</returns>
    public DBConnectorManagerType WithAfterQueryExecutionAction(Action<DBQueryType> afterQueryExecutionAction) {
        this.AfterQueryExecutionAction = afterQueryExecutionAction;

        return (DBConnectorManagerType)this;
    }
    #endregion

    #region Setup actions
    /// <summary>
    /// Gets or sets the action to execute for connection setup.
    /// </summary>
    public Action<DBConnectionType> ConnectionSetupAction { get; private set; }

    /// <summary>
    /// Gets or sets the function to instantiate a query builder.
    /// </summary>
    public Func<DBQueryBuilderType,      DBQueryBuilderType> QueryBuilderInstantiationFunction                  { get; private set; }

    /// <summary>
    /// Gets or sets the function to instantiate a DB connector from a string builder.
    /// </summary>
    public Func<DBConnectorSettingsType, DBConnectorType>    DBConnectionFromStringBuilderInstantiationFunction { get; private set; }

    /// <summary>
    /// Gets or sets the function to instantiate a DB connector from a connection string.
    /// </summary>
    public Func<string,                  DBConnectorType>    DBConnectionFromStringInstantiationFunction        { get; private set; }
    #endregion

    /// <summary>
    /// Initializes a new instance of ConnectorManager with default connection settings.
    /// </summary>
    public ConnectorManager() { }

    /// <summary>
    /// Initializes a new instance of ConnectorManager with a custom connection string builder.
    /// </summary>
    /// <param name="connectionStringBuilder">The connection string builder containing configuration details.</param>
    public ConnectorManager(DBConnectorSettingsType connectionStringBuilder) {
        this.ConnectionStringBuilder = connectionStringBuilder;
    }

    /// <summary>
    /// Initializes a new instance of ConnectorManager with a raw connection string.
    /// </summary>
    /// <param name="connectionString">The full connection string</param>
    public ConnectorManager(string connectionString) {
        this.ConnectionString = connectionString;
    }

    /// <summary>
    /// Configures the connection string builder using the specified action.
    /// </summary>
    /// <param name="action">The action to configure the connection string builder.</param>
    /// <returns>The current connector manager instance.</returns>
    public DBConnectorManagerType Configure(Action<DBConnectorSettingsType> action) {
        this.ConnectionStringBuilder = (DBConnectorSettingsType) Activator.CreateInstance<DBConnectorSettingsType>();

        action.Invoke(this.ConnectionStringBuilder);

        return (DBConnectorManagerType) this;
    }

    #region Setup methods
    /// <summary>
    /// Enables or disables automatic connection renewal.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    /// <returns>The current connector manager instance.</returns>
    public DBConnectorManagerType WithAutomaticConnectionRenewal(bool enabled = true) {
        this.AutomaticConnectionRenewal = enabled;

        return (DBConnectorManagerType) this;
    }

    /// <summary>
    /// Sets the interval for automatic connection renewal.
    /// </summary>
    /// <param name="interval">The renewal interval.</param>
    /// <returns>The current connector manager instance.</returns>
    public DBConnectorManagerType WithAutomaticConnectionRenewalInterval(TimeSpan interval) {
        this.AutomaticConnectionRenewalInterval = interval;

        return (DBConnectorManagerType) this;
    }

    /// <summary>
    /// Sets the action to execute for connection setup.
    /// </summary>
    /// <param name="action">The connection setup action.</param>
    /// <returns>The current connector manager instance.</returns>
    public DBConnectorManagerType WithConnectionSetup(Action<DBConnectionType> action) {
        this.ConnectionSetupAction = action;

        return (DBConnectorManagerType) this;
    }
    #endregion

    #region DB Query Builder
    /// <summary>
    /// Return a DB Type Query Builder instance for the current thread ID.
    /// The thread ID is retrieved by Thread.CurrentThread.ManagedThreadId.
    /// This adds up to ensure thread-safety of the requests.
    /// </summary>
    /// <returns>The QueryBuilder instance for the current thread.</returns>
    public DBQueryBuilderType QueryBuilder() {
        DBQueryBuilderType queryBuilder = null;
        long threadId = Thread.CurrentThread.ManagedThreadId;

        lock (_queryBuilderStackLock) {
            if (this._queryBuilderStack.ContainsKey(threadId)) {
                queryBuilder = this._queryBuilderStack[threadId];
            }

            // Instance does not exist, create it
            if (queryBuilder == null) {
                queryBuilder = __GenerateDBQueryBuilderInstance();

                this._queryBuilderStack.Add(threadId, queryBuilder);
            }
        }

        return queryBuilder;
    }

    /// <summary>
    /// Creates a query builder instance using a detached connection instance.
    /// This allows queries to be built independently of the current connection context,
    /// useful for scenarios requiring isolated query execution.
    /// </summary>
    /// <returns>A new QueryBuilder instance bound to a detached connection.</returns>
    public virtual DBQueryBuilderType DetachedQueryBuilder() {
        return ((DBQueryBuilderType) Activator.CreateInstance(typeof(DBQueryBuilderType), new object[] { this.GetDetachedConnector() }))
            .WithOnQueryExceptionAction(this.OnQueryExceptionAction)
            .WithBeforeQueryExecutionAction(this.BeforeQueryExecutionAction)
            .WithAfterQueryExecutionAction(this.AfterQueryExecutionAction)
        ;
    }

    /// <summary>
    /// Creates a query builder instance using a detached connection instance and a pre-defined query object.
    /// </summary>
    /// <param name="query">The query object to initialize with.</param>
    /// <returns>A new QueryBuilder instance bound to a detached connection and initialized with the query.</returns>
    public virtual DBQueryBuilderType DetachedQueryBuilder(DBQueryType query) {
        return ((DBQueryBuilderType)Activator.CreateInstance(typeof(DBQueryBuilderType), new object[] { this.GetDetachedConnector(), query }))
            .WithOnQueryExceptionAction(this.OnQueryExceptionAction)
            .WithBeforeQueryExecutionAction(this.BeforeQueryExecutionAction)
            .WithAfterQueryExecutionAction(this.AfterQueryExecutionAction)
        ;
    }

    /// <summary>
    /// Creates a query builder instance using the current connection manager's attached instance.
    /// The resulting builder can execute queries against the current database connection.
    /// </summary>
    /// <returns>A new QueryBuilder instance bound to the current connection.</returns>
    protected virtual DBQueryBuilderType __GenerateDBQueryBuilderInstance() {
        return ((DBQueryBuilderType) Activator.CreateInstance(typeof(DBQueryBuilderType), new object[] { this.GetThreadConnector() }))
            .WithOnQueryExceptionAction(this.OnQueryExceptionAction)
            .WithBeforeQueryExecutionAction(this.BeforeQueryExecutionAction)
            .WithAfterQueryExecutionAction(this.AfterQueryExecutionAction)
        ;
    }
    #endregion

    #region DB Connector Handler
    /// <summary>
    /// Return a DBType DB Connector instance for the current thread ID.
    /// The thread ID is retrieved by Thread.CurrentThread.ManagedThreadId.
    /// This adds up to ensure thread-safety of the requests.
    /// </summary>
    /// <returns>The DBType instance for the current thread.</returns>
    public DBConnectorType GetThreadConnector() {
        DBConnectorType dbConnector = null;
        long            threadId    = Thread.CurrentThread.ManagedThreadId;

        lock (_connectorStackLock) {
            if (this._connectorStack.ContainsKey(threadId)) {
                dbConnector = this._connectorStack[threadId];
            }

            // Instance exists, but is disconnected
            if (dbConnector != null && !dbConnector.Connected) {
                dbConnector = null;
                this._connectorStack.Remove(threadId);
            }

            // Instance does not exist, create it
            if (dbConnector == null) {
                dbConnector = this.__GenerateDBTypeInstance();

                this._connectorStack.Add(threadId, dbConnector);
            }
        }

        this.__InitializeConnector(dbConnector);

        return dbConnector;
    }

    /// <summary>
    /// Generate a detached and not-managed instance of the DBType DB Connector,
    /// mostly intended to be used as a single-use connection.
    /// </summary>
    /// <returns>A brand-new DBType instance, initialized and ready to run queries.</returns>
    public DBConnectorType GetDetachedConnector() {
        DBConnectorType dBconnector = this.__GenerateDBTypeInstance();
        this.__InitializeConnector(dBconnector);

        return dBconnector;
    }

    /// <summary>
    /// Initialize the DBType DB connection, ensuring that it is connected
    /// or reconnecting if the connection meets the AutomaticRenewalInterval limit.
    /// </summary>
    /// <typeparam name="DBType">DB Connector Type.</typeparam>
    /// <param name="dbConnector">The DBConnector to initialize.</param>
    public void __InitializeConnector<DBType>(DBType dbConnector) where DBType : Unleasharp.DB.Base.Connector<DBType, DBConnectionType, DBConnectorSettingsType> {
        bool force = (this.AutomaticConnectionRenewal && this.AutomaticConnectionRenewalInterval.TotalMilliseconds > 0)
            ? DateTime.UtcNow - dbConnector.ConnectionTimestamp >= this.AutomaticConnectionRenewalInterval
            : false
        ;

        dbConnector.Connect(force);
    }

    /// <summary>
    /// Creates an instance of the <see cref="DBConnectorType"/> class using the current connection settings.
    /// </summary>
    /// <remarks>
    /// The method attempts to initialize a <see cref="DBConnectorType"/> instance using either the
    /// <see cref="ConnectionString"/> or <see cref="ConnectionStringBuilder"/> property, depending on which is available.
    /// If neither is set, the method returns <see langword="null"/>.
    /// </remarks>
    /// <returns>
    /// An instance of the <see cref="DBConnectorType"/> class initialized with the connection settings,
    /// or <see langword="null"/> if no valid settings are provided.
    /// </returns>
    protected virtual DBConnectorType __GenerateDBTypeInstance() {
        object settingsSource = !string.IsNullOrWhiteSpace(this.ConnectionString) ? this.ConnectionString : this.ConnectionStringBuilder;

        if (settingsSource != null) {
            return (DBConnectorType) Activator.CreateInstance(typeof(DBConnectorType), new object[] { settingsSource });
        }

        return null;
    }
    #endregion
}