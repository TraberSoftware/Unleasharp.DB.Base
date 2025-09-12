# 🛠️ Unleasharp.DB.Base

[![NuGet version (Unleasharp.DB.Base)](https://img.shields.io/nuget/v/Unleasharp.DB.Base.svg?style=flat-square)](https://www.nuget.org/packages/Unleasharp.DB.Base/)
[![Github Pages](https://img.shields.io/badge/home-Github_Pages_-blue)](https://trabersoftware.github.io/Unleasharp.DB.Base)
[![Documentation](https://img.shields.io/badge/dev-Documentation-blue)](https://trabersoftware.github.io/Unleasharp.DB.Base/docs/)

![Unleasharp.DB.Base](https://socialify.git.ci/TraberSoftware/Unleasharp.DB.Base/image?description=1&font=Inter&logo=https%3A%2F%2Fraw.githubusercontent.com%2FTraberSoftware%2FUnleasharp%2Frefs%2Fheads%2Fmain%2Fassets%2Flogo-small.png&name=1&owner=1&pattern=Circuit+Board&theme=Light)

A lightweight, database-agnostic library for .NET that provides connection management, query building, and data serialization capabilities.

## 🎯 Key Concepts

This library provides a foundation for database operations with the following core features:

### 🔌 Connection Handling
- Automatic connection creation and management
- Configurable automatic connection regeneration after specified intervals
- Ensures database connections are always open and ready for use

### 🧵 Threading Support
- Thread-safe connection management through `ConnectorManager`
- Each thread receives its own dedicated database connection instance

### 📝 Query Generation
- **Query** class: Holds SQL query parameters in an engine-agnostic manner
- **QueryBuilder** class: Executes queries against the database and maps results to specified types
- Follows fluent interface pattern for intuitive query building

### 🔄 Serialization
- Automatic mapping of database results to C# objects using generic type parameters
- Supports both simple class mapping and attribute-based schema definitions

## 🔧 Query Generation Architecture

The library follows a **CRTP (Curiously Recurring Template Pattern)** approach where:
- The base `Query` class provides engine-agnostic functionality
- Engine-specific implementations handle the actual SQL rendering
- Query building follows standard SQL syntax with fluent method chaining

## 📖 Documentation Resources

- 📚 **[GitHub Pages Documentation](https://trabersoftware.github.io/Unleasharp.DB.Base/docs/)** - Complete documentation
- 🎯 **[Getting Started Guide](https://trabersoftware.github.io/Unleasharp.DB.Base/docs/getting-started/)** - Quick start guide

## 🚀 Database Engine Implementations

- ✅ **MySQL** - [Unleasharp.DB.MySQL](https://github.com/TraberSoftware/Unleasharp.DB.MySQL)
- ✅ **SQLite** - [Unleasharp.DB.SQLite](https://github.com/TraberSoftware/Unleasharp.DB.SQLite)
- ✅ **PostgreSQL** - [Unleasharp.DB.PostgreSQL](https://github.com/TraberSoftware/Unleasharp.DB.PostgreSQL)
- ✅ **MSSQL** - [Unleasharp.DB.MSSQL](https://github.com/TraberSoftware/Unleasharp.DB.MSSQL)

## 📦 Dependencies

- [Unleasharp](https://github.com/TraberSoftware/Unleasharp) - Multipurpose library

## 📋 Version Compatibility

This library targets .NET 6.0 and later versions. For specific version requirements, please check the package dependencies.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
