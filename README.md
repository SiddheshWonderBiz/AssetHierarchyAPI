# Asset Hierarchy API

A robust RESTful API for managing hierarchical asset structures with real-time signal monitoring, authentication, and data persistence. This project provides a complete backend solution for asset management systems with support for complex hierarchies, signal tracking, and role-based access control.

## Overview

The Asset Hierarchy API enables organizations to:
- Organize assets in hierarchical tree structures (parent-child relationships)
- Monitor and track signals/metrics associated with assets
- Manage asset logs and audit trails
- Authenticate users via OAuth2 (Google, GitHub) or JWT
- Track asset calculations and derived metrics
- Receive real-time notifications via WebSocket

## Features

✅ **Hierarchical Asset Management** - Create, read, update, and delete assets organized in tree structures  
✅ **Signal Monitoring** - Track and manage signals/metrics for assets  
✅ **Real-time Notifications** - WebSocket-based real-time updates via SignalR  
✅ **Multi-format Storage** - Support for JSON and XML file formats  
✅ **Database Persistence** - SQL Server with Entity Framework Core  
✅ **Authentication & Authorization** - OAuth2 (Google, GitHub) and JWT token support  
✅ **Comprehensive Logging** - Serilog with file and console outputs  
✅ **Rate Limiting** - Request throttling middleware  
✅ **Error Handling** - Centralized error handling middleware  
✅ **Docker Support** - Containerized deployment  
✅ **API Documentation** - Swagger/OpenAPI integration  

## Tech Stack

### Backend (This Repository)

**Language & Framework:**
- **C# 12+** - Language
- **ASP.NET Core 8+** - Web API framework

**Database & ORM:**
- **SQL Server** - Relational database
- **Entity Framework Core 8+** - ORM for database operations
- **EF Core Migrations** - Database versioning

**Authentication & Security:**
- **JWT (JSON Web Tokens)** - Token-based authentication
- **Google OAuth2** - Social authentication
- **GitHub OAuth2** - Social authentication
- **Serilog** - Structured logging

**Real-time Communication:**
- **SignalR** - Real-time WebSocket communication for notifications

**API & Documentation:**
- **Swagger/OpenAPI** - API documentation and exploration

**Middleware & Cross-cutting:**
- **CORS** - Cross-Origin Resource Sharing for frontend integration
- **Custom Rate Limiting** - Request throttling
- **Custom Error Handling** - Centralized exception handling

**DevOps & Deployment:**
- **Docker** - Containerization
- **Docker Compose** - Multi-container orchestration

---

### Frontend (Separate Repository)

**Repository:** [asset-hierarchy-frontend](https://github.com/Siddhesh1123/asset-hierarchy-frontend)

**Tech Stack:**
- **React 18+** - UI framework
- **JavaScript (ES6+)** - Programming language
- **Tailwind CSS** - Utility-first CSS framework
- **Vite** - Build tool and development server

---

## Project Structure

```
AssetHierarchyAPI/
├── Application/              # Application layer - DTOs and services
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Interfaces/           # Service contracts
│   ├── Services/             # Business logic (HierarchyService, etc.)
│   └── Mapping/              # Object mapping logic
├── Domain/                   # Domain layer - Core models
│   └── Models/               # AssetNode, Signal, User, AssetLog, SignalValue
├── Infrastructure/           # Infrastructure layer - Data access & EF Core
│   ├── Data/                 # DbContext, migrations, database files
│   ├── Repositories/         # Data access patterns
│   ├── Services/             # Infrastructure services
│   ├── Hubs/                 # SignalR hubs for real-time communication
│   └── Extensions/           # Dependency injection extensions
├── AssetHierarchyAPI/        # API layer - Controllers and middleware
│   ├── Controllers/          # HTTP endpoints
│   ├── Middleware/           # Custom middleware
│   └── Properties/           # Launch settings
├── Dockerfile                # Container configuration
└── docker-compose.yaml       # Multi-container orchestration
```

## Architecture

The project follows a **Clean Architecture** pattern with clear separation of concerns:

- **API Layer** (`AssetHierarchyAPI/`) - HTTP endpoints, middleware, and request handling
- **Application Layer** (`Application/`) - Business logic, DTOs, and service interfaces
- **Domain Layer** (`Domain/`) - Core models and business rules
- **Infrastructure Layer** (`Infrastructure/`) - Database access, repositories, and data persistence

## Getting Started

### Prerequisites

- .NET 8 SDK or later
- SQL Server (local or Docker)
- Docker & Docker Compose (optional, for containerized deployment)

### Local Development Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/asset-hierarchy-api.git
   cd AssetHierarchyAPI
   ```

2. **Configure the database connection:**
   - Update `appsettings.Development.json` with your SQL Server connection string:
   ```json
   {
     "ConnectionStrings": {
       "Connect": "Server=YOUR_SERVER;Database=AssetsDB;User Id=sa;Password=YOUR_PASSWORD;"
     }
   }
   ```

3. **Apply database migrations:**
   ```bash
   dotnet ef database update -p Infrastructure
   ```

4. **Run the API:**
   ```bash
   cd AssetHierarchyAPI
   dotnet run
   ```

5. **Access the API:**
   - Swagger UI: `http://localhost:7079/swagger`
   - API Base URL: `http://localhost:7079/api`

### Docker Setup

1. **Build and run with Docker Compose:**
   ```bash
   docker-compose up --build
   ```

2. **Access the containerized API:**
   - Swagger UI: `http://localhost:7079/swagger`
   - Database runs on port `1433`

## Configuration

### Environment Variables & Settings

Configure the following in `appsettings.json`:

```json
{
  "StorageType": "DB",           // "DB" or "FILE"
  "FileType": "JSON",            // "JSON" or "XML"
  "ConnectionStrings": {
    "Connect": "YOUR_CONNECTION_STRING"
  },
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "GitHub": { "ClientId": "...", "ClientSecret": "..." }
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY",
    "Issuer": "yourapp",
    "Audience": "yourappUsers"
  }
}
```

## API Endpoints

### Core Endpoints

**Hierarchy Management:**
- `GET /api/hierarchy` - Get all assets
- `GET /api/hierarchy/{id}` - Get asset by ID
- `POST /api/hierarchy` - Create new asset
- `PUT /api/hierarchy/{id}` - Update asset
- `DELETE /api/hierarchy/{id}` - Delete asset

**Signals:**
- `GET /api/signals` - Get all signals
- `GET /api/signals/{assetId}` - Get signals for an asset
- `POST /api/signals` - Create new signal
- `PUT /api/signals/{id}` - Update signal

**Authentication:**
- `POST /api/auth/login` - Login with credentials
- `POST /api/auth/google` - Google OAuth callback
- `POST /api/auth/github` - GitHub OAuth callback

**Calculations:**
- `GET /api/calculations/{assetId}` - Get calculated metrics

See Swagger UI for complete API documentation.

## Real-time Features

The API uses **SignalR** for real-time WebSocket communication:

- **Connection Endpoint:** `/notificationHub`
- **Features:** Real-time asset updates, signal changes, and notifications

## Logging

Logs are configured using **Serilog** and output to:
- **Console** - Real-time console output
- **Files** - Daily rolling log files in `Logs/` directory with format: `log-YYYY-MM-DD.txt`

## Frontend Integration

Connect your React frontend by:

1. **Set CORS origin** in `Program.cs`:
   ```csharp
   policy.WithOrigins("http://localhost:5173")  // Vite dev server
   ```

2. **Frontend repository:** [asset-hierarchy-frontend](https://github.com/Siddhesh1123/asset-hierarchy-frontend)
   - Built with React, JavaScript, and Tailwind CSS
   - Communicates with this API for data and real-time updates

## Development Workflow

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName -p Infrastructure
```

Apply migrations:
```bash
dotnet ef database update -p Infrastructure
```

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Security Considerations

- ✅ JWT tokens for stateless authentication
- ✅ OAuth2 for social login security
- ✅ SQL Server for encrypted data storage
- ✅ CORS configured for specific frontend domains
- ✅ Rate limiting to prevent abuse
- ✅ Structured error handling without exposing sensitive details

## Performance Features

- Rate limiting middleware prevents API abuse
- Entity Framework Core with lazy loading for efficient queries
- SignalR for optimized real-time communication
- Docker containerization for scalable deployment

## Troubleshooting

**Issue: Database connection fails**
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Ensure database credentials are correct

**Issue: CORS errors**
- Verify frontend origin matches CORS policy in `Program.cs`
- Check that credentials are allowed in CORS configuration

**Issue: SignalR connection fails**
- Ensure `/notificationHub` endpoint is accessible
- Check WebSocket connections are not blocked by firewalls

## License

This project is licensed under the MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Contact & Support

For issues, questions, or suggestions, please open an issue on GitHub or contact the development team.

---

**Related Projects:**
- 🎨 **Frontend:** [asset-hierarchy-frontend](https://github.com/Siddhesh1123/asset-hierarchy-frontend) - React + Tailwind CSS UI

---

*Last Updated: May 2026*
