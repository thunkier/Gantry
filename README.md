# Gantry

Gantry is a modern, cross-platform API client built with .NET and Avalonia UI. It provides a powerful and efficient interface for testing and organizing HTTP requests, managing environments, and collaborating on API collections with built-in version control.

## Features

- **Cross-Platform**: Runs seamlessly on Windows, macOS, and Linux
- **Collection Management**: Organize requests into collections and folders with intuitive drag-and-drop support
- **Environment Variables**: Manage variables for different environments (Dev, Staging, Prod)
- **Import/Export**: 
  - Import existing Postman collections (v2.1)
  - Export collections to OpenAPI 3.0, TypeSpec, Bruno, or JSON Schema formats
- **Source Control**: Integrated Git support for versioning collections and team collaboration
- **Node Editor**: Visual workflow builder for creating automated API test flows and request chains
- **File-Based Storage**: Collections stored as simple file system structures for easy versioning and sharing

## Why Bundle-as-Directory?

Gantry takes a unique approach to storing API requests using a **directory-based bundle format**. Each request is stored as a `.req` folder containing multiple files, rather than a single monolithic JSON file. This design brings several powerful advantages:

### Git-Friendly Collaboration
- **Clean Diffs**: Changes to URLs, headers, body, or scripts show up as isolated file changes, making code reviews meaningful
- **No Merge Conflicts**: Team members can simultaneously edit different aspects of a request (one person updating the body while another refines tests) without conflicts
- **Meaningful History**: Git blame and history work at the granular level—see exactly when a test script was added or a header was modified

### Developer Experience
- **IDE Integration**: Script files (`.js`) get full IntelliSense, syntax highlighting, and linting support in your favorite editor
- **Separate Concerns**: Request configuration (`meta.toml`), payload (`body.json`), scripts (`pre-script.js`, `test.js`), and documentation (`readme.md`) are cleanly separated
- **Text Editor Friendly**: Edit any component with specialized tools—use a Markdown editor for docs, your IDE for scripts, and any text editor for configs

### Documentation & Maintenance
- **Documentation as Code**: Each request bundle includes a `readme.md` for inline documentation that lives alongside the request
- **Clean Bundles**: Empty files are automatically removed—no clutter from unused scripts or documentation
- **Reusability**: Copy entire `.req` folders between collections or share common request patterns as templates

### Example Bundle Structure
```
GetUser.req/
├── meta.toml          # URL, method, headers, params, auth
├── body.json          # Request payload (if applicable)
├── pre-script.js      # Pre-request script
├── test.js            # Response tests/assertions
└── readme.md          # Endpoint documentation
```

This approach ensures your API collections are not just executable requests, but **maintainable, collaborative, and version-controlled projects**.

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022, JetBrains Rider, or VS Code

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/thinkier/Gantry.git
   cd Gantry
   ```

2. Open `Gantry.sln` in your preferred IDE

3. Build and run the `Gantry.Desktop` project:
   ```bash
   dotnet run --project src/Gantry.Desktop/Gantry.Desktop.csproj
   ```

## Architecture

Gantry follows a clean, modular architecture:

- **Gantry.Core**: Domain models and business logic
- **Gantry.Infrastructure**: Data persistence, serialization, and external services
- **Gantry.UI**: Avalonia-based UI components organized by feature
- **Gantry.Desktop**: Desktop application entry point

## Roadmap

### In Progress
- **UI Polish**: Refining the user interface for a more unique, intuitive experience
- **Node Editor Enhancements**: 
  - Persist node workflows to workspace for version control
  - Automated request execution and response processing
  - Full API specification generation from flows
  - Enhanced visual editing capabilities

### Planned
- HTTP/2 and HTTP/3 support
- GraphQL support
- WebSocket testing
- CLI for automation and CI/CD integration
- Plugin system for extensibility

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Copyright

Copyright (c) 2025 Hunter Coupe DeVillez (Thinkier).
