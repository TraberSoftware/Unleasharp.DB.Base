---
outline: deep
---

# ASP.Net
ASP.NET Core compatibility is provided out of the box. The ConnectorManager class handles all connection threading and should be registered as a singleton service.

## Program.cs
```csharp
builder.Services.AddSingleton<ConnectorManager>(services => {
    return new ConnectorManager(builder.Configuration.GetConnectionString("DefaultConnection"))
        .WithOnQueryExceptionAction((query, exception) => {
            Log.Error("A exception has occured when trying to execute the query {query}: {exception}", query, exception.Message);
        })
        .WithAfterQueryExecutionAction(query => Log.Information("Query executed: {query}", query))
    ;
});
```

## Controller.cs
```csharp
using DatabaseStructureSample;
using Microsoft.AspNetCore.Mvc;
using Unleasharp.DB.SQLite;

namespace AspNET.Controllers;

[ApiController]
[Route("[controller]")]
public class QueryBuilderController : Controller {
    private readonly ConnectorManager _db;

    public QueryBuilderController(ConnectorManager db) {
        _db = db;
    }

    [HttpPost()]
    public async Task<ActionResult<object>> Insert([FromBody] ExampleTable row) {
        long rowId = _db.QueryBuilder().Build(query => query
            .Insert()
            .Into<ExampleTable>()
            .Value<ExampleTable>(row)
        ).Execute<long>();

        return new OkObjectResult(new {
            Id = rowId
        });
    }
}
```
