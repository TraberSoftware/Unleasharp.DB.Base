---
outline: deep
---

# DuckDB

> 📝 **Note**: The DuckDB implementation is currently on `beta` version, and is derived from the PostgreSQL implementation.

This page documents the DuckDB-specific features exposed by the Unleasharp.DB Query Builder.

## Auto Increment Sequences

DuckDB does not provide a dedicated `AUTO INCREMENT` column attribute at table-creation time the same way some other engines do. Instead, you create and manage sequences explicitly and use the sequence in the column default via `nextval(...)`.

To create a sequence with the `Query Builder` use `Query.CreateSequence()` and then set the column default to use the sequence, for example `Default = "nextval('sequence_name')"`.

```csharp
dbConnector.QueryBuilder().Build(query => query
    .CreateSequence("seq_example_table_id")
).Execute();
```

### `CreateSequence()` Parameters
| Parameter      | Description                                                                                                             | Default     |
|----------------|------------------------------------------------------------------------------------------------------------------------:|-------------|
| `sequenceName` | The name of the sequence to create. Used by nextval()/default expressions and must be unique within the database.       | `Required`  |
| `start`        | The initial value of the sequence (first value returned).                                                               | 1           |
| `increment`    | Step size between successive sequence values (positive or negative for descending sequences).                           | 1           |
| `maxValue`     | Maximum value the sequence can produce. Use -1 to indicate no maximum (unbounded).                                      | -1 (no max) |
| `cycle`        | If true, the sequence will wrap around to the start (or min) when reaching max; if false, it will error when exhausted. | false       |

### Column With Sequence
```csharp
[Table("example_table")]
[UniqueKey (typeof(ExampleTable), nameof(ExampleTable.Id))]
public class ExampleTable {
    [Column("id", ColumnDataType.UInt64, PrimaryKey = true, Unsigned = true, AutoIncrement = true, Default = "nextval('seq_example_table_id')")]
    public ulong? Id {
        get; set;
    }
    [Column("_mediumtext", ColumnDataType.Text, Length = -1)]
    public string MediumText {
        get; set;
    }
    [Column("_longtext", ColumnDataType.Text, Length = -1)]
    public string Longtext {
        get; set;
    }
}
```

## CSV

DuckDB provides first-class CSV support (functions like read_csv and COPY FROM). The Query Builder exposes helpers for common CSV workflows while preserving the full parameter surface available in DuckDB.

See the DuckDB docs for the complete set of CSV parameters:
https://duckdb.org/docs/stable/data/csv/overview#parameters

The examples below use the provided sample flights.csv and a simple Unleasharp.DB mapping class.

> flights.csv
```
FlightDate|UniqueCarrier|OriginCityName|DestCityName
1988-01-01|AA|New York, NY|Los Angeles, CA
1988-01-02|AA|New York, NY|Los Angeles, CA
1988-01-03|AA|New York, NY|Los Angeles, CA
```

> Flights.cs
```csharp
[Table("flights")]
public class Flights {
    [Column("FlightDate", ColumnDataType.Date)]
    public DateOnly FlightDate     { get; set; }

    [Column("UniqueCarrier", ColumnDataType.Varchar)]
    public string   UniqueCarrier  { get; set; }

    [Column("OriginCityName", ColumnDataType.Varchar)]
    public string   OriginCityName { get; set; }

    [Column("DestCityName", ColumnDataType.Varchar)]
    public string   DestCityName   { get; set; }
}
```


### CSV to Table

This workflow copies rows from a CSV file into a database table (equivalent to `COPY table_name FROM 'csv_file.csv'`). 

> 📝 **Note**: With the current Query Builder you must create the destination table beforehand (for example with `CreateTable<T>()`).

```csharp
var readCSVFunction = new Unleasharp.DB.DuckDB.Functions.ReadCSVFunction {
    Path    = "flights.csv",
    Delim   = "|",
    Header  = true,
    Columns = new Dictionary<string, string> {
        {"FlightDate",     "DATE"    },
        {"UniqueCarrier",  "VARCHAR" },
        {"OriginCityName", "VARCHAR" },
        {"DestCityName",   "VARCHAR" },
    }
};

dbConnector.QueryBuilder().Build(query => query.CreateTable<Flights>()).Execute<bool>();
int insertedFromCSV = dbConnector.QueryBuilder().Build(query => query
    .CopyIntoFromCSV<Flights>(readCSVFunction)
).Execute<int>();
```

If the table already exists you may also target a table by name (no class mapping required) and dump the CSV contents directly into it:

```csharp
var readCSVFunction = new Unleasharp.DB.DuckDB.Functions.ReadCSVFunction {
    Path    = "flights.csv",
    Delim   = "|",
    Header  = true,
    Columns = new Dictionary<string, string> {
        {"FlightDate",     "DATE"    },
        {"UniqueCarrier",  "VARCHAR" },
        {"OriginCityName", "VARCHAR" },
        {"DestCityName",   "VARCHAR" },
    }
};

int insertedFromCSV = dbConnector.QueryBuilder().Build(query => query
    .CopyIntoFromCSV("raw_table_name", readCSVFunction)
).Execute<int>();
```


### CSV to Rows

DuckDB allows direct interaction with CSV data using regular queries, reading data from a CSV file.

This method insert the data from a CSV file into a table. It is the equivalent to `SELECT * FROM read_csv('csv_file.csv')`.

```csharp
var readCSVFunction = new Unleasharp.DB.DuckDB.Functions.ReadCSVFunction {
    Path    = "flights.csv",
    Delim   = "|",
    Header  = true,
    Columns = new Dictionary<string, string> {
        {"FlightDate",     "DATE"    },
        {"UniqueCarrier",  "VARCHAR" },
        {"OriginCityName", "VARCHAR" },
        {"DestCityName",   "VARCHAR" },
    }
};

List<Flights> csvFlights = dbConnector.QueryBuilder().Build(query => query
    .Select()
    .From(readCSVFunction)
).ToList<Flights>();
