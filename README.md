# Integration Platform

A robust integration platform built with .NET 8 that handles third-party integrations via SFTP and email, with comprehensive monitoring and tracing capabilities.

## Features

- SFTP Integration with certificate-based authentication
- Email Integration (IMAP/SMTP)
- File Processing
- Distributed Tracing with OpenTelemetry
- Comprehensive Logging
- API Documentation with Swagger

## Architecture

The solution follows a modular architecture with the following components:

- **IntegrationPlatform.Api**: Main API project that exposes REST endpoints
- **IntegrationPlatform.SFTP**: SFTP integration service
- **IntegrationPlatform.Email**: Email integration service
- **IntegrationPlatform.Monitoring**: Monitoring and tracing service
- **IntegrationPlatform.Contracts**: Shared contracts and interfaces

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Docker (optional)

### Installation

1. Clone the repository
2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
3. Build the solution:
   ```bash
   dotnet build
   ```
4. Run the API project:
   ```bash
   cd src/IntegrationPlatform.Api
   dotnet run
   ```

### Configuration

The application uses the following configuration settings:

```json
{
  "Sftp": {
    "Host": "sftp.example.com",
    "Port": 22,
    "Username": "user",
    "CertificatePath": "path/to/certificate.pfx"
  },
  "Email": {
    "Host": "mail.example.com",
    "Port": 993,
    "Username": "user@example.com",
    "Password": "password"
  },
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317"
  }
}
```

## API Endpoints

### SFTP Integration

- `POST /api/integration/sftp/connect`: Connect to SFTP server
- `POST /api/integration/sftp/upload`: Upload file to SFTP server
- `POST /api/integration/sftp/download`: Download file from SFTP server
- `DELETE /api/integration/sftp/delete`: Delete file from SFTP server
- `GET /api/integration/sftp/list`: List files in SFTP directory

### Email Integration

- `POST /api/integration/email/connect`: Connect to email server
- `POST /api/integration/email/send`: Send email
- `GET /api/integration/email/unread`: Get unread emails
- `POST /api/integration/email/mark-read`: Mark email as read
- `POST /api/integration/email/download-attachment`: Download email attachment

## Monitoring

The platform uses OpenTelemetry for distributed tracing and monitoring. Metrics and traces can be viewed in your preferred observability platform (e.g., Jaeger, Prometheus, Grafana).

## Security

- Certificate-based authentication for SFTP
- Secure storage of credentials in Azure Key Vault
- HTTPS for API endpoints
- Input validation and sanitization

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 