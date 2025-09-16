using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime;
using System.Text;

namespace Unleasharp.DB.Base;

/// <summary>
/// Represents a generic database connector that provides functionality for managing database connections and
/// configuration settings.
/// </summary>
/// <remarks>This class serves as a base for implementing database connectors with customizable connection
/// settings and connection management functionality. It provides methods to connect to and disconnect from a database,
/// as well as properties to access connection-related information such as the connection state and timestamp.</remarks>
/// <typeparam name="DBConnectorType">The specific type of the database connector implementation.</typeparam>
/// <typeparam name="DBConnectorSettingsType">The type of the database connection settings, which must derive from <see cref="DbConnectionStringBuilder"/>.</typeparam>
public partial class Connector<DBConnectorType, DBConnectionType, DBConnectorSettingsType>
    where DBConnectorType         : Connector<DBConnectorType, DBConnectionType, DBConnectorSettingsType>
    where DBConnectionType        : DbConnection
    where DBConnectorSettingsType : DbConnectionStringBuilder
{

    /// <summary>
    /// Gets or sets the current database connection.
    /// </summary>
    public virtual DBConnectionType Connection { get; protected set; }

    /// <summary>
    /// Gets the <see cref="Type"/> of the database connector used by the application.
    /// </summary>
    public Type DatabaseConnectorType {
        get {
            return typeof(DBConnectorType);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the connection is currently active.
    /// </summary>
    public bool Connected {
        get {
            return this._Connected();
        }
    }

    /// <summary>
    /// Gets the timestamp of when the connection was established.
    /// </summary>
    public DateTime ConnectionTimestamp { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the <see cref="DbConnectionStringBuilder"/> instance used to construct and manage the connection string.
    /// </summary>
    public DbConnectionStringBuilder StringBuilder { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Connector"/> class.
    /// </summary>
    public Connector() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Connector"/> class with the specified database connector
    /// settings.
    /// </summary>
    /// <remarks>Ensure that the <paramref name="stringBuilder"/> parameter contains valid
    /// configuration settings required to establish a connection. Passing invalid or incomplete settings may result
    /// in connection failures.</remarks>
    /// <param name="stringBuilder">The settings used to configure the database connection.</param>
    public Connector(DBConnectorSettingsType stringBuilder) {
        this.StringBuilder = stringBuilder;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Connector"/> class using the specified connection string.
    /// </summary>
    /// <remarks>This constructor dynamically creates an instance of the <see cref="DBConnectorSettingsType"/>
    /// using the provided connection string. Ensure that the connection string is
    /// properly formatted  and compatible with the expected database type.</remarks>
    /// <param name="connectionString">The connection string used to configure the database connector settings.  Must be a valid connection string
    /// format.</param>
    public Connector(string connectionString) {
        this.StringBuilder = (DBConnectorSettingsType) Activator.CreateInstance(typeof(DBConnectorSettingsType), new object[] { connectionString });
    }

    #region Connection management
    /// <summary>
    /// Connect to the configured database server if connection is not already open
    /// </summary>
    /// <param name="force">If Force is set to True, the connection will be performed even if the current connection is already open</param>
    /// <returns>True if connection is open, False otherwise</returns>
    public bool Connect(bool force = false) {
        this._Connect(force);

        return Connected;
    }

    /// <summary>
    /// Disconnect from the database
    /// </summary>
    /// <returns>True if connection is closed, False otherwise</returns>
    public bool Disconnect() {
        this._Disconnect();

        return !Connected;
    }

    /// <summary>
    /// Check if the connection is open
    /// </summary>
    /// <returns>True if connection is open, False otherwise</returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual bool _Connected() {
        switch (this.Connection.State) {
            // If any of this cases, the connection is open
            case System.Data.ConnectionState.Open:
            case System.Data.ConnectionState.Fetching:
            case System.Data.ConnectionState.Executing:
                return true;
        }

        return false;
    }

    /// <summary>
    /// Connect to the configured database server if connection is not already open
    /// </summary>
    /// <param name="force">If Force is set to True, the connection will be performed even if the current connection is already open</param>
    /// <returns>True if connection is open, False otherwise</returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual bool _Connect(bool force = false) {
        if (this.Connection == null) {
            // The connection does not exist, create it
            this.Connection = (DBConnectionType) Activator.CreateInstance(typeof(DBConnectionType), new object[] { this.StringBuilder.ConnectionString });
        }

        if (
            !this._Connected()     // If not connected, it should be obvious to create the connection
            ||                     //
            (                      //
                force              // Reaching this statement means the connection is open but we are forcing the connection to be closed first
                &&                 //
                this._Disconnect() // Appending the disconnect disables the need to actively check again if connection is open to be closed
            )
        ) {
            this.Connection.Open();

            this.ConnectionTimestamp = DateTime.UtcNow;
        }

        return this._Connected();
    }

    /// <summary>
    /// Disconnect from the database
    /// </summary>
    /// <returns>True if connection is closed, False otherwise</returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual bool _Disconnect() {
        if (this.Connection != null) {
            this.Connection.Close();
        }

        return !this._Connected();
    }
    #endregion
}
