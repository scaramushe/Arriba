# Arriba

A .NET web application that wraps the Aruba Instant On portal, providing a simplified interface to manage access points and radio controls.

## Features

- **Authentication**: Login with your Aruba Instant On credentials
- **Access Point Management**: View all access points in your sites
- **Radio Control**: Enable/disable radios and view their status with toggle buttons
- **Browser Caching**: Credentials and tokens are cached in localStorage for persistent sessions
- **Responsive UI**: Works on desktop and mobile devices

## Architecture

```
Arriba/
├── src/
│   ├── Arriba.Core/           # Core library
│   │   ├── Models/            # Data models
│   │   └── Services/          # API client and business logic
│   └── Arriba.Web/            # ASP.NET Core Web API
│       ├── Controllers/       # API endpoints
│       └── wwwroot/           # Frontend assets (HTML, CSS, JS)
└── tests/
    └── Arriba.Tests/          # Unit and integration tests
```

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An Aruba Instant On account with access to your sites

## Getting Started

### Running Locally

1. Clone the repository:
   ```bash
   git clone https://github.com/your-repo/arriba.git
   cd arriba
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project src/Arriba.Web
   ```

4. Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

### Running Tests

```bash
dotnet test
```

### Using Docker

1. Build the Docker image:
   ```bash
   docker build -t arriba .
   ```

2. Run the container:
   ```bash
   docker run -p 8080:8080 arriba
   ```

3. Access the application at `http://localhost:8080`

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login with email/password |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | Logout and clear session |

### Sites

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/sites` | Get all sites |
| GET | `/api/sites/{siteId}` | Get site details with devices |
| GET | `/api/sites/{siteId}/devices` | Get all devices for a site |
| GET | `/api/sites/{siteId}/devices/{deviceId}` | Get device with radios |

### Radios

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/sites/{siteId}/devices/{deviceId}/radios` | Get all radios |
| POST | `/api/sites/{siteId}/devices/{deviceId}/radios/{radioId}/toggle` | Toggle radio on/off |
| PATCH | `/api/sites/{siteId}/devices/{deviceId}/radios/{radioId}` | Update radio settings |

## Configuration

The application uses the following configuration in `appsettings.json`:

```json
{
  "Aruba": {
    "BaseUrl": "https://nb.portal.arubainstanton.com/api",
    "AuthUrl": "https://sso.arubainstanton.com",
    "DefaultSiteId": "your-site-id"
  }
}
```

## Security Considerations

- Tokens are stored in the browser's localStorage (or sessionStorage if "Remember me" is unchecked)
- The application proxies requests to the Aruba API, keeping credentials secure
- HTTPS is recommended for production deployments
- CORS is configured to allow requests from any origin (configure appropriately for production)

## Browser Caching

The application implements aggressive caching for better performance:

- **Static assets** (JS, CSS): Cached for 1 year with immutable flag
- **HTML files**: Cached for 1 hour
- **Auth tokens**: Stored in localStorage with automatic refresh before expiry

## License

MIT
