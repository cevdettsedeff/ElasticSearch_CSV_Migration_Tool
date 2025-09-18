# ElasticSearch and CSV to PostgreSQL Migration Tool

A modern WPF application for migrating ElasticSearch or CSV data to PostgreSQL databases with ElasticSearch support and clean architecture design.

## Features

- **CSV Import**: Import CSV files with automatic column detection and validation
- **PostgreSQL Integration**: Direct data migration to PostgreSQL databases
- **ElasticSearch Support**: Migrate data from ElasticSearch to PostgreSQL
- **Data Validation**: Built-in validation with FluentValidation
- **Batch Processing**: Configurable batch sizes for optimal performance
- **Parallel Processing**: Multi-threaded processing for large datasets
- **Real-time Progress**: Live progress tracking with ETA calculations
- **Dry Run Mode**: Test migrations without actual data transfer
- **Modern UI**: Clean, responsive WPF interface
- **Clean Architecture**: MVVM pattern with dependency injection

## Screenshots

![Main Interface](https://i.imgur.com/G2n24lL.png)

## Requirements

- **.NET 8.0** or later
- **Windows 10** or later
- **PostgreSQL 12+** database
- **ElasticSearch 7.x** (optional)

## Installation

### Option 1: Download Release
1. Download the latest release from [Releases](../../releases)
2. Extract the ZIP file
3. Run `ElasticSearchPostgreSQLMigrationTool.exe`

### Option 2: Build from Source
```bash
git clone https://github.com/yourusername/csv-postgresql-migration-tool.git
cd csv-postgresql-migration-tool
dotnet restore
dotnet build --configuration Release
dotnet run
```

## Configuration

### Database Connection
Update `appsettings.json` with your database settings:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=your_db;Username=postgres;Password=yourpassword;"
  },
  "Migration": {
    "BatchSize": 1000,
    "EnableParallelProcessing": false,
    "MaxDegreeOfParallelism": 4,
    "StopOnError": true,
    "IgnoreDuplicates": true,
    "DryRun": false,
    "TruncateBeforeMigration": false,
    "LogLevel": "Info"
  }
}
```

### Environment-Specific Configuration
- `appsettings.Development.json` - Development settings
- `appsettings.Production.json` - Production settings

Set environment variable:
```bash
set ASPNETCORE_ENVIRONMENT=Development
```

## Usage

### CSV Migration
1. **Select CSV File**: Click "Browse..." to select your CSV file
2. **Configure Settings**: 
   - Set batch size (recommended: 1000-5000)
   - Enable/disable parallel processing
   - Configure database connection
3. **Validate**: Click "Validate CSV" to check file format
4. **Migrate**: Click "Start Migration" to begin the process

### Supported CSV Format
The tool expects CSV files with the following columns:
- `_id` - Unique identifier
- `timestamp` - Date/time field
- `ip` - IP address
- `eventId` - Event identifier
- Additional fields as needed

### ElasticSearch Migration
Configure ElasticSearch settings in `appsettings.json`:

```json
{
  "Migration": {
    "ElasticSearchNodes": [
      "http://localhost:9200"
    ],
    "IndexName": "your-index-name",
    "ScrollTimeout": "10m",
    "RequestTimeoutMinutes": 5
  }
}
```

## Advanced Features

### Batch Processing
- Configurable batch sizes from 100 to 10,000 records
- Automatic memory management
- Progress tracking per batch

### Parallel Processing
- Multi-threaded processing for large datasets
- Configurable thread count
- Thread-safe operations

### Data Validation
- FluentValidation integration
- Custom validation rules
- Error reporting and logging

### Logging
The application provides comprehensive logging:
- Real-time log display in UI
- File-based logging
- Configurable log levels (Debug, Info, Warning, Error)

## Architecture

The application follows clean architecture principles:

```
├── Models/           # Data models and entities
├── Services/         # Business logic and data access
├── ViewModels/       # MVVM view models
├── Views/           # WPF user interface
├── Interfaces/      # Service contracts
├── Validators/      # Data validation rules
├── Enums/           # Enumeration types and constants
├── Resources/       # Images, icons, and resource files
└── Infrastructure/  # Dependency injection and configuration
```

### Key Technologies
- **WPF** - User interface framework
- **MVVM Toolkit** - Data binding and commands
- **FluentValidation** - Data validation
- **NEST** - ElasticSearch client
- **Npgsql** - PostgreSQL driver
- **Microsoft Extensions** - Dependency injection and configuration

## Troubleshooting

### Common Issues

**Connection Errors**
- Verify database connection string
- Check PostgreSQL service is running
- Ensure firewall allows connections

**CSV Format Issues**
- Check file encoding (UTF-8 recommended)
- Verify column headers match expected format
- Remove special characters from headers

**Performance Issues**
- Reduce batch size for large files
- Disable parallel processing for debugging
- Monitor system memory usage

**ElasticSearch Connection**
- Verify ElasticSearch is running
- Check node URLs in configuration
- Validate index name and permissions

### Log Files
Logs are stored in:
- Application UI (real-time)
- `logs/` directory (file-based)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Setup
```bash
git clone https://github.com/yourusername/csv-postgresql-migration-tool.git
cd csv-postgresql-migration-tool
dotnet restore
```

### Code Style
- Follow C# coding conventions
- Use MVVM pattern for UI components
- Implement proper error handling
- Add unit tests for new features

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

### Version 1.0.0 (Initial Release)
- CSV to PostgreSQL migration functionality
- ElasticSearch to PostgreSQL migration support
- Modern WPF interface with MVVM architecture
- Real-time progress tracking with ETA calculations
- Batch processing with configurable sizes
- Parallel processing support for large datasets
- Data validation with FluentValidation
- Dry run mode for testing migrations
- Comprehensive logging and error handling
- Clean architecture with dependency injection
- Multiple environment configuration support
- Duplicate detection and handling

---

**Made with ❤️ using .NET and WPF**
