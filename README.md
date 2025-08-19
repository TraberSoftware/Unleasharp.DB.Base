# Unleasharp.DB.Base
QueryBuilder-based generic database client abstraction. This is the base for all the database engine implementations.

## Concepts
This library deals with: 
 * Connection handling: It creates, regenerates automatically after X time, and makes sure a given database connection is open
 * Threading: The ConnectorManager class handles the connections internally so only one connection is generated for each thread.
 * Query generation: The Query class holds the parameters for a SQL query. The QueryBuilder class holds a Query, and can perform the operations against the database, and retrieves the result of the query.
 * Serialization: The QueryBuilder class returns the Query result as the class you specify.

## Query Generation
Each database engine will have its own classes for the specific engine implementation, but Query should remain as much engine-agnostic as possible.

To do so, the Query class follows the CRTP patterns, it doesn't matter the implementation, the rendering is handled by the base but performed in the implementation.

The Query class follows a QueryBuilder pattern with chained functions to achieve transparent query generation, just using code to parametrice the query.


## Database Engine Implementations
 * MySQL - https://github.com/TraberSoftware/Unleasharp.DB.MySQL
 * SQLite - WIP
 * PostgreSQL - WIP
 * MSSQL - WIP
