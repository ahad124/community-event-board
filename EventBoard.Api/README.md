# EventBoard.Api

ASP.NET Core Web API for the Event Board application targeting .NET 8.

## Project Structure

- **Models/**: Entity classes (User, Event) with validation
- **Data/**: Entity Framework Core context (AppDbContext)
- **Controllers/**: API endpoints (UsersController)
- **Migrations/**: Database schema migrations

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB or full installation)

## Features

### User Management API
- GET /api/users - Get all users
- GET /api/users/{id} - Get user by ID
- POST /api/users - Create new user
- PUT /api/users/{id} - Update user
- DELETE /api/users/{id} - Delete user

## Entity Relationships

- **One-to-Many**: User → Events
  - One User can have many Events
  - Cascade delete enabled (deleting a user also deletes their events)
  - Email is unique for each user

## Getting Started

1. Install dependencies:
```bash
dotnet restore
```

2. Apply migrations:
```bash
dotnet ef database update
```

3. Run the application:
```bash
dotnet run
```

4. Access Swagger UI at https://localhost:5001/swagger

## Database Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EventBoardDb;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

## NuGet Packages

- **Microsoft.EntityFrameworkCore**: 8.0.0
- **Microsoft.EntityFrameworkCore.SqlServer**: 8.0.0
- **Microsoft.EntityFrameworkCore.Tools**: 8.0.0
- **Swashbuckle.AspNetCore**: 6.4.0

## API Documentation

Swagger documentation is available at `/swagger` endpoint when the application is running in development mode.

## Error Handling

All endpoints return appropriate HTTP status codes:
- 200 OK - Successful GET
- 201 Created - Successful POST
- 204 No Content - Successful DELETE
- 400 Bad Request - Invalid input
- 404 Not Found - Resource not found
- 409 Conflict - Duplicate email or conflict
