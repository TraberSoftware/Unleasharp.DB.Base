---
outline: deep
---

# 🗺️ Data Mapping
One of the basics to start performing operations with the database is mapping the data. Unleasharp.DB handles data serialization to C# classes, but requires to setup previously the data structure of the classes for a better mapping.

## Fields vs Properties
While most of the ORMs out there require you to use Properties when defining the data classes, Unleasharp.DB does not. Fields can be used as long as there are correctly defined by either the Field name or the Column Attribute. However, we highly encourage to use Properties when possible.
