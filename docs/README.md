# TaskManager

A minimal .NET 8 sample with **clear layers** in one solution:
- `TaskManager.Domain` (Entities/DTOs)
- `TaskManager.DAL` (EF Core, repository)
- `TaskManager.BLL` (business logic)
- `TaskManager.Web` (Razor Pages UI + Web API + Swagger)

**Database:** SQLite (file `tasks.db`). You can switch to SQL Server by changing the connection string in `TaskManager.Web/appsettings.json` and adding the `Microsoft.EntityFrameworkCore.SqlServer` package to `TaskManager.Web` and `TaskManager.DAL`.

## Configration Secrets

Please, use [Secret Manager](https://learn.microsoft.com/en-gb/aspnet/core/security/app-secrets?view=aspnetcore-9.0&tabs=windows) to set your secrets:
```bash
dotnet user-secrets set "AzureOpenAI:Endpoint"  "..."
dotnet user-secrets set "AzureOpenAI:DeploymentName"  "..."
dotnet user-secrets set "AzureOpenAI:ApiKey"  "..."
dotnet user-secrets set "AzureOpenAIEmbeddings:Endpoint"  "..."
dotnet user-secrets set "AzureOpenAIEmbeddings:DeploymentName"  "..."
dotnet user-secrets set "AzureOpenAIEmbeddings:ApiKey"  "..."
```

## Run

```bash
cd TaskManager/TaskManager.Web
dotnet run
```

Open:
- UI: https://localhost:7199 or http://localhost:5199
- API (Swagger): `/swagger`

## Notes
- DB is created on first run (`EnsureCreated()`).
- UI calls the API on the **same origin** (`/api/tasks`). Frontend and backend are clearly separated (Razor Pages vs Controllers), while sharing a single host for simplicity.
