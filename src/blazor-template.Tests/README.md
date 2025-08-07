# Blazor Template Tests

This test project contains unit and integration tests for the Blazor Template application.

## Test Structure

- **BasicTests.cs** - Basic unit tests for core data models and configuration
- **Data/ApplicationDbContextTests.cs** - Database context and entity tests using Entity Framework InMemory provider

## Running Tests

From the solution root:
```bash
dotnet test
```

From the test project directory:
```bash
cd src/blazor-template.Tests
dotnet test
```

## Test Framework

- **xUnit** - Main testing framework
- **Entity Framework InMemory** - In-memory database for data tests
- **Moq** - Mocking framework (available for future use)
- **ASP.NET Core Testing** - Integration testing support (available for future use)

## Current Coverage

- ✅ Basic data model validation
- ✅ Database context operations
- ✅ Entity relationships and constraints
- ✅ Configuration model structure

## Future Enhancements

The test project is set up for expansion with:
- Service layer unit tests
- API integration tests
- Authentication and authorization tests
- Navigation service tests
- Theme service tests
- Invite system tests

## Notes

The test project uses .NET 9.0 while the main application uses .NET 8.0. This is intentional to leverage the latest testing features while maintaining backward compatibility.