# Test app for tree editing

Implementation for the tree editor test application.

## Start with one command

```bash
docker compose up --build
```

Then open:

- UI: http://localhost:4200
- API Swagger: http://localhost:5001/swagger

Stop everything with `Ctrl+C`. To remove containers and the database volume:

```bash
docker compose down -v
```

## Stack

- **API:** ASP.NET Core 8, EF Core 8, PostgreSQL 16
- **UI:** Angular 18, standalone components, Signals
- **Infrastructure:** Docker Compose
- **DB model:** adjacency list (`parent_id`)

### Database schema

The database uses a simple adjacency-list table:

```text
TreeNodes
---------
id         uuid PK
parent_id  uuid NULL FK -> TreeNodes.id
value      varchar(500)
```

The parent relationship is immutable for existing nodes. The API does not expose `parentId` in update commands, so a client cannot move an existing node to another parent.

## Launching without Docker

### API

Requires .NET 8 SDK and PostgreSQL.

```bash
cd TreeEditorWithLocalCache.Api
dotnet restore
dotnet run
```

Set `ConnectionStrings__ApplicationDb` to your PostgreSQL connection string if necessary.

### UI

Requires Node.js 20+.

```bash
cd TreeEditorWithLocalCache.Ui
npm install --force
npm start
```

The Angular development server proxies `/api` to `http://localhost:5001`.
