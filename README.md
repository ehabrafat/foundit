# FounditAi

FounditAi is an ASP.NET Core Web API built with .NET 8, PostgreSQL, Entity Framework Core, and pgvector.

## Requirements

- .NET 8 SDK (the repository pins the 8.0 SDK family in `global.json`)
- PostgreSQL
- PostgreSQL with the [pgvector](https://github.com/pgvector/pgvector) extension available
- Git

The API uses Gemini for text embeddings and SMTP for email. Configure those services to use embedding and email features.

## 1. Get the source and restore packages

```cmd
git clone https://github.com/ehabrafat/foundit.git
cd foundit
dotnet restore FounditAi.sln
```

## 2. Create a PostgreSQL database

Create a database named `foundit` (or choose another name) and ensure your PostgreSQL installation supports pgvector. The migrations enable the `vector` extension; the PostgreSQL user used by the connection string must be allowed to create it, or an administrator must enable it first.

## 3. Configure local settings

Use .NET User Secrets to keep machine-specific credentials out of source control. Run from the repository root:

```cmd
dotnet user-secrets init --project API
```

Set the connection string to match your PostgreSQL host, port, database, username, and password:

```cmd
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=foundit;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD" --project API
```

Set a unique JWT signing secret (use a long random value):

```cmd
dotnet user-secrets set "Jwt:Secret" "YOUR_LONG_RANDOM_SECRET" --project API
```

For Gemini-powered embeddings, set your Gemini API key:

```cmd
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY" --project API
```

For email features, configure your SMTP provider details:

```cmd
dotnet user-secrets set "Smtp:Host" "YOUR_SMTP_HOST" --project API
dotnet user-secrets set "Smtp:Port" "587" --project API
dotnet user-secrets set "Smtp:Username" "YOUR_SMTP_USERNAME" --project API
dotnet user-secrets set "Smtp:Password" "YOUR_SMTP_PASSWORD" --project API
dotnet user-secrets set "Smtp:From" "YOUR_VERIFIED_SENDER_ADDRESS" --project API
```

User Secrets are for local development only. In production, provide these values through a secure environment or secret manager.

## 4. Apply database migrations

Install the EF Core command line tool if it is not already available:

```cmd
dotnet tool install --global dotnet-ef --version 8.0.0
```

Apply the migrations from the repository root:

```cmd
dotnet ef database update --project Infrastructure --startup-project API
```

The migrations create the schema and add the pgvector column/index.

## 5. Run the API

```cmd
dotnet run --project API
```

In Development mode, Swagger UI is available at the `/swagger` path shown in the console output (typically `https://localhost:<port>/swagger`).

## Configuration reference

| Setting | Required for | Example / notes |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | Database access | PostgreSQL connection string |
| `Jwt:Secret` | API startup and JWT authentication | Use a long random value |
| `Gemini:ApiKey` | Gemini embedding features | Gemini API key |
| `Gemini:EmbeddingModel` | Optional | Defaults to `gemini-embedding-001` |
| `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:From` | Email features | Values from your SMTP provider |
