You are a Staff .NET Engineer helping me build a production-quality side project for Senior Software Engineer interviews.

Project context:

- .NET SDK: 10.0.x
- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core
- Podman
- Clean Architecture
- Future integrations: Azure Blob Storage, Azure Key Vault, Azure AI Foundry, Service-to-Service Authentication, Background Processing.

Current solution structure:

Document.Api
Document.Application
Document.Contracts
Document.Domain
Document.Infrastructure
Document.Processing

Task:

Prepare the Infrastructure project for Entity Framework Core.

Requirements:

1. Verify that project references follow Clean Architecture principles.
   Expected dependencies:

   - Document.Domain
     (no dependencies)

   - Document.Application
     references Document.Domain and Document.Contracts

   - Document.Infrastructure
     references Document.Application, Document.Domain and Document.Contracts

   - Document.Api
     references Document.Application, Document.Infrastructure and Document.Contracts

   - Document.Processing
     references Document.Application, Document.Infrastructure and Document.Contracts

2. Add the required NuGet packages to Document.Infrastructure:

   - Microsoft.EntityFrameworkCore
   - Microsoft.EntityFrameworkCore.Design
   - Npgsql.EntityFrameworkCore.PostgreSQL

Use the latest stable 10.x versions compatible with .NET 10.

3. Verify whether dotnet-ef is installed.

If not installed, provide the command to install it globally.

4. Explain WHY every package is needed.

5. Explain WHY these project references follow Clean Architecture.

Do NOT generate DbContext yet.

Do NOT generate entities yet.

Do NOT create migrations yet.

Return:

- project reference diagram
- package list
- commands
- explanation
- recommended git commit message