# Event Board - Quick Start Guide

## Overview

The Event Board is a full-stack application with:
- **Frontend**: React + Vite with Bootstrap
- **Backend**: ASP.NET Core Web API with SQLite

## Prerequisites

- Node.js (v16+)
- .NET 8 SDK
- SQLite (included with .NET)

## Setup Instructions

### 1. Start the Backend API

```bash
cd /Users/mac/React_vite/EventBoard.Api

# First time: restore packages
dotnet restore

# Run the API
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 2. Create Test Data (Optional)

In a new terminal, create a test user:

```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com"}'
```

Response:
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john@example.com"
}
```

Then create test events:

```bash
curl -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "title":"Tech Conference 2026",
    "description":"Join us for keynote speeches and workshops",
    "date":"2026-09-15T09:00:00Z",
    "userId":1
  }'
```

### 3. Start the Frontend Development Server

In a new terminal:

```bash
cd /Users/mac/React_vite/event-board-frontend

# First time: install dependencies (already done)
npm install

# Start dev server
npm run dev
```

Expected output:
```
  VITE v8.1.1  ready in 123 ms

  ➜  Local:   http://localhost:5173/
```

### 4. Open in Browser

Navigate to: **http://localhost:5173**

## Features

### Frontend
- ✅ Responsive Bootstrap layout
- ✅ Event list with pagination cards
- ✅ Event detail view
- ✅ Loading spinners
- ✅ Error handling
- ✅ React Router navigation

### Backend
- ✅ User management (CRUD)
- ✅ Event management (CRUD)
- ✅ One-to-many relationship (User → Events)
- ✅ Cascade delete
- ✅ SQLite database
- ✅ Swagger API documentation

## API Endpoints

### Users
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Events
- `GET /api/events` - Get all events
- `GET /api/events/{id}` - Get event by ID
- `GET /api/events/user/{userId}` - Get user's events
- `POST /api/events` - Create event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event

## File Locations

```
/Users/mac/React_vite/
├── EventBoard.Api/                 # Backend ASP.NET Core
│   ├── Controllers/
│   │   ├── UsersController.cs
│   │   └── EventsController.cs
│   ├── Models/
│   │   ├── User.cs
│   │   └── Event.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Migrations/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── EventBoard.Api.csproj
│   └── EventBoard.db                # SQLite database
│
└── event-board-frontend/            # React + Vite
    ├── src/
    │   ├── components/
    │   │   ├── EventList.jsx
    │   │   └── EventDetail.jsx
    │   ├── App.jsx
    │   ├── App.css
    │   └── main.jsx
    ├── .env
    ├── vite.config.js
    ├── package.json
    └── index.html
```

## Troubleshooting

### Frontend can't connect to API
- ✅ Check backend is running on port 5000
- ✅ Verify proxy in `vite.config.js` is correct
- ✅ Check `.env` file has correct `VITE_API_BASE_URL`

### Backend errors
- ✅ Check port 5000 is available
- ✅ Delete `EventBoard.db` and restart to reset database
- ✅ Run `dotnet restore` if packages are missing

### Port already in use
```bash
# Find process on port 5000
lsof -i :5000

# Kill process
kill -9 <PID>
```

## Development Commands

### Frontend
```bash
npm run dev      # Start dev server
npm run build    # Build for production
npm run preview  # Preview production build
npm run lint     # Run linter
```

### Backend
```bash
dotnet run                          # Run API
dotnet build                        # Build project
dotnet ef database update           # Apply migrations
dotnet ef migrations add <Name>     # Create migration
```

## Environment Variables

### Frontend (.env)
```
VITE_API_BASE_URL=/api
```

### Backend (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=EventBoard.db"
  }
}
```

## Next Steps

1. ✅ Start both backend and frontend
2. ✅ Create test users and events via API
3. ✅ View events in list view
4. ✅ Click to see event details
5. ✅ Customize styling in `App.css`
6. ✅ Add more features as needed

## Support

For issues or questions about the implementation, check:
- [IMPLEMENTATION_VERIFICATION.md](./IMPLEMENTATION_VERIFICATION.md) - Detailed feature checklist
- Backend README in EventBoard.Api folder
- Frontend components in src/components/
