using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Threading;
using Unleasharp.DB.Base.QueryBuilding;

namespace Unleasharp.DB.Base {
    /// <summary>
    /// The ConnectorManager class handles the DB connections with the given provider,
    /// ensures the connections are alive and regenerates them if needed.
    /// 
    /// It ensures thread-safety by spawning thread-owned connections
    /// </summary>
    /// <typeparam name="DBConnectorType">The Type of the DB Connector to spawn connections of</typeparam>
    public class ConnectorManager<DBConnectorManagerType, DBConnectorType, DBConnectorSettingsType> 
        where DBConnectorType         : Connector<DBConnectorType, DBConnectorSettingsType>
        where DBConnectorSettingsType : DbConnectionStringBuilder
        where DBConnectorManagerType  : ConnectorManager<DBConnectorManagerType, DBConnectorType, DBConnectorSettingsType>
    {
        private Dictionary<long, DBConnectorType> __ConnectorStack = new Dictionary<long, DBConnectorType>();

        public DBConnectorSettingsType ConnectionStringBuilder { get; private set; }
        public  string                 ConnectionString        { get; private set; }


        #region Automatic connection management settings
        public bool     AutomaticConnectionRenewal         { get; private set; } = true;
        public TimeSpan AutomaticConnectionRenewalInterval { get; private set; } = TimeSpan.FromSeconds(900);
        #endregion

        public ConnectorManager() { }

        public ConnectorManager(DBConnectorSettingsType ConnectionStringBuilder) {
            this.ConnectionStringBuilder = ConnectionStringBuilder;
        }

        public ConnectorManager(string ConnectionString) {
            this.ConnectionString = ConnectionString;
        }

        public DBConnectorManagerType Configure(Action<DBConnectorSettingsType> Action) {
            this.ConnectionStringBuilder = (DBConnectorSettingsType) Activator.CreateInstance<DBConnectorSettingsType>();

            Action.Invoke(this.ConnectionStringBuilder);

            return (DBConnectorManagerType) this;
        }

        #region Setup methods
        public DBConnectorManagerType WithAutomaticConnectionRenewal(bool Enabled = true) {
            this.AutomaticConnectionRenewal = Enabled;

            return (DBConnectorManagerType) this;
        }

        public DBConnectorManagerType WithAutomaticConnectionRenewalInterval(TimeSpan Interval) {
            this.AutomaticConnectionRenewalInterval = Interval;

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
            DBConnectorType DBconnector = this.__GenerateDBTypeInstance();
            this.__Initialize(DBconnector);

            return DBconnector;
        }

        /// <summary>
        /// Return a DBType DB Connector instance for the current thread ID.
        /// The thread ID is retrieved by Thread.CurrentThread.ManagedThreadId
        /// This adds up to ensure thead-safety of the requests
        /// </summary>
        /// <returns>The DBType instance for the current thread</returns>
        public DBConnectorType GetInstance() {
            DBConnectorType DBconnector = null;
            long            ThreadID    = Thread.CurrentThread.ManagedThreadId;

            if (this.__ConnectorStack.ContainsKey(ThreadID)) {
                DBconnector = this.__ConnectorStack[ThreadID];
            }

            // Instance exists, but is disconnected
            if (DBconnector != null && !DBconnector.Connected) {
                DBconnector = null;
                this.__ConnectorStack.Remove(ThreadID);
            }

            // Instance does not exist, create it
            if (DBconnector == null) {
                DBconnector = this.__GenerateDBTypeInstance();

                this.__ConnectorStack.Add(ThreadID, DBconnector);
            }

            this.__Initialize(DBconnector);

            return DBconnector;
        }

        /// <summary>
        /// Initialize the DBType DB connection, ensuring that it is connected
        /// or reconnecting if the connection meets the AutomaticRenewalInterval limit
        /// </summary>
        /// <typeparam name="DBType">DB Connector Type</typeparam>
        /// <param name="DBConnector">The DBConnector to initialize</param>
        public void __Initialize<DBType>(DBType DBConnector) where DBType : Unleasharp.DB.Base.Connector<DBType, DBConnectorSettingsType> {
            bool Force = (this.AutomaticConnectionRenewal && this.AutomaticConnectionRenewalInterval.TotalMilliseconds > 0)
                ? DateTime.UtcNow - DBConnector.ConnectionTimestamp >= this.AutomaticConnectionRenewalInterval
                : false
            ;

            DBConnector.Connect(Force);
        }

        /// <summary>
        /// Creates an instance of the <see cref="DBConnectorType"/> class using the current connection settings.
        /// </summary>
        /// <remarks>The method attempts to initialize a <see cref="DBConnectorType"/> instance using either the 
        /// <see cref="ConnectionString"/> or <see cref="Settings"/> property, depending on which is available. If
        /// neither is set, the method returns <see langword="null"/>.</remarks>
        /// <returns>An instance of the <see cref="DBConnectorType"/> class initialized with the connection settings, 
        /// or <see langword="null"/> if no valid settings are provided.</returns>
        private DBConnectorType __GenerateDBTypeInstance() {
            object SettingsSource = !string.IsNullOrWhiteSpace(this.ConnectionString) ? this.ConnectionString : this.ConnectionStringBuilder;

            if (SettingsSource != null) {
                return (DBConnectorType) Activator.CreateInstance(typeof(DBConnectorType), new object[] { SettingsSource });
            }

            return null;
        }
        #endregion
    }
}