# Human Resource Information System (HRIS)

This project is an **HRIS (Human Resource Information System)** built using **ASP.NET MVC** and **Entity Framework Core**.

Default local setup:
- App host: `http://localhost:5080`
- Database: PostgreSQL (managed via pgAdmin 4)

---

## Project Setup Instructions

### 1. Clone the Repository
```bash
git clone https://github.com/KewnsR/3ACS_G2.git
cd 3ACS_G2
dotnet restore
```

### 2. Create Database in pgAdmin 4
1. Open pgAdmin 4 and connect to your local PostgreSQL server.
2. Create a database named `hris_db`.
3. Ensure you have a user with privileges to this database (default example uses user `postgres`).

### 3. Configure Connection String
Update `HumanRepProj/appsettings.Development.json` (and `HumanRepProj/appsettings.json` if needed):

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=hris_db;Username=postgres;Password=your_password"
}
```

### 4. Run the Project
```bash
dotnet run --project HumanRepProj
```

On startup, the app applies EF Core migrations using `Database.MigrateAsync()`.

