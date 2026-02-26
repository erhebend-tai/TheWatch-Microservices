# Stream 1: SCHEMA — Stage 5B (TODO Items 15-21)

You are working in a git worktree of TheWatch microservices solution. Your task is to adapt SQL DDL from specification documents into EF Core entity configurations and create seed data.

## YOUR ASSIGNED TODO ITEMS

15. Map P0 UniversalMeasurementsDB tables from 00_Schema.sql to EF seed data
16. Adapt P2 DDL from Watch_Program_02_VoiceEmergency.md to EF entity configs
17. Adapt P3 DDL from Watch-MeshNetwork.md (19 tables) to EF entity configs
18. Adapt P4 DDL from Program4_Watch_Wearable.sql to EF entity configs
19. Adapt P5 DDL from Watch-AuthSecurity/ (4 parts) to EF entity configs
20. Adapt P8 DDL from Watch-DisasterRelief/ to EF entity configs (18 tables)
21. Create seed data scripts for development (test users, sample incidents, demo families)

## SPECIFICATION FILES (read these first)

- `C:/Users/erheb/OneDrive/SCRAPE/Common_Measurement_Infrastructure/00_Schema.sql` (P0 foundation)
- `C:/Users/erheb/OneDrive/SCRAPE/Common_Measurement_Infrastructure/Watch_Program_02_VoiceEmergency.md` (P2)
- `C:/Users/erheb/OneDrive/SCRAPE/Common_Measurement_Infrastructure/Watch-MeshNetwork.md` (P3, 19 tables)
- `C:/Users/erheb/OneDrive/SCRAPE/Common_Measurement_Infrastructure/Program4_Watch_Wearable.sql` (P4)
- `C:/Users/erheb/OneDrive/SCRAPE/Watch-AuthSecurity/` (P5, 4 parts)
- `C:/Users/erheb/OneDrive/SCRAPE/Watch-DisasterRelief/` (P8)

## FILES YOU MAY MODIFY (your exclusive scope)

- `TheWatch.P1.CoreGateway/Core/*.cs` — add new entity classes from P0 schema
- `TheWatch.P2.VoiceEmergency/Emergency/*.cs` — extend entity classes per DDL
- `TheWatch.P3.MeshNetwork/Mesh/*.cs` — extend entity classes for 19 tables
- `TheWatch.P4.Wearable/Devices/*.cs` — extend entity classes per DDL
- `TheWatch.P5.AuthSecurity/Auth/*.cs` — extend entity classes per DDL (models only)
- `TheWatch.P8.DisasterRelief/Relief/*.cs` — extend entity classes for 18 tables
- `docker/sql/init/` — create seed data SQL scripts
- Any new `SeedData/` directories within services

## FILES YOU MUST NOT TOUCH

- Any `Program.cs` file
- Any `Services/` directory
- Any `Middleware/` directory
- `TheWatch.Shared/` (any file)
- `TheWatch.Mobile/` (any file)
- `TheWatch.Dashboard/` (any file)
- `TheWatch.Aspire.AppHost/`
- `TheWatch.Generators/`
- `.github/`
- `*.csproj` files (do not add packages)

## EXISTING PATTERNS TO FOLLOW

Entities use this pattern (detected by Roslyn generator):
```csharp
public class EntityName
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // ... properties
}
```

Key rules:
- All entities MUST have `public Guid Id { get; set; }` (the DbContext generator detects this)
- Use string enums (the generator converts enums to string columns)
- Value objects without Guid Id become owned types (e.g., Location, GeoPoint)
- Sub-namespaces: P1=`.Core`, P2=`.Emergency`, P3=`.Mesh`, P4=`.Devices`, P5=`.Auth`, P8=`.Relief`
- Read existing entity files first to understand what's already defined before adding

## SEED DATA FORMAT

Create seed data as C# classes with static methods that return entity arrays:
```csharp
public static class SeedData
{
    public static UserProfile[] GetTestUsers() => new[]
    {
        new UserProfile { Id = Guid.Parse("..."), Name = "Test User 1", ... },
        // ...
    };
}
```

Also create matching SQL scripts in `docker/sql/init/` for direct database seeding.

## WHEN DONE

Commit all changes with message:
```
feat(schema): adapt DDL specs to EF entity configs and create seed data

Items 15-21: P0 seed data, P2/P3/P4/P5/P8 entity configs from spec DDL
```
