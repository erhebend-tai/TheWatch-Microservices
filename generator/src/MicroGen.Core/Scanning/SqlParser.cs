using System.Text.RegularExpressions;
using MicroGen.Core.Helpers;
using MicroGen.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace MicroGen.Core.Scanning;

/// <summary>
/// Parses SQL Server DDL scripts into <see cref="ServiceDescriptor"/> objects.
/// Uses TSql170Parser (ScriptDom) for proper AST-based parsing.
/// </summary>
public sealed partial class SqlParser : ISourceParser
{
    private readonly ILogger<SqlParser> _logger;

    public SqlParser(ILogger<SqlParser> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> SupportedExtensions => [".sql"];

    public bool CanParse(string pathOrUri) =>
        Path.GetExtension(pathOrUri).Equals(".sql", StringComparison.OrdinalIgnoreCase);

    public async Task<ServiceDescriptor> ParseAsync(string pathOrUri, string domainName, CancellationToken ct = default)
    {
        _logger.LogDebug("Parsing SQL file {File}", pathOrUri);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var sqlText = await File.ReadAllTextAsync(pathOrUri, ct).ConfigureAwait(false);

        var parser = new TSql170Parser(initialQuotedIdentifiers: true);
        var fragment = parser.Parse(new StringReader(sqlText), out var errors);

        if (errors is { Count: > 0 })
        {
            foreach (var err in errors)
                _logger.LogWarning("SQL parse error in {File} line {Line}: {Message}",
                    pathOrUri, err.Line, err.Message);
        }

        var visitor = new SqlVisitor();
        fragment?.Accept(visitor);

        var serviceName = visitor.DatabaseName
            ?? NamingHelper.ServiceNameFromFile(pathOrUri);

        var (title, description) = ExtractLeadingComment(sqlText);

        var schemas = new List<SchemaDescriptor>();
        var operations = new List<OperationDescriptor>();
        var tags = new List<TagDescriptor>();

        // Process each discovered table
        foreach (var table in visitor.Tables)
        {
            ct.ThrowIfCancellationRequested();

            var schemaName = NamingHelper.ToPascalCase(table.TableName);
            var tag = new TagDescriptor
            {
                Name = schemaName,
                Description = $"CRUD operations for {table.TableName}"
            };
            tags.Add(tag);

            // Entity schema (all columns)
            var entitySchema = BuildEntitySchema(table);
            schemas.Add(entitySchema);

            // Create request schema (exclude PK, computed, rowversion)
            var createSchema = BuildCreateRequestSchema(table);
            schemas.Add(createSchema);

            // Update request schema (same exclusions)
            var updateSchema = BuildUpdateRequestSchema(table);
            schemas.Add(updateSchema);

            // Check constraints → enums
            foreach (var enumSchema in BuildEnumSchemas(table))
                schemas.Add(enumSchema);

            // 5 CRUD operations per table
            operations.AddRange(BuildCrudOperations(table, schemaName));
        }

        // Process stored procedures
        foreach (var proc in visitor.Procedures)
        {
            ct.ThrowIfCancellationRequested();
            operations.Add(BuildProcedureOperation(proc));
        }

        sw.Stop();

        var descriptor = new ServiceDescriptor
        {
            DomainName = domainName,
            ServiceName = serviceName,
            SpecFilePath = pathOrUri,
            Title = title ?? serviceName,
            Description = description ?? string.Empty,
            Version = "1.0.0",
            Tags = tags,
            Operations = operations,
            Schemas = schemas,
            SecuritySchemes = ["BearerAuth"]
        };

        _logger.LogInformation(
            "Parsed SQL {File} in {Elapsed}ms: {Tables} tables, {Ops} operations, {Schemas} schemas",
            Path.GetFileName(pathOrUri), sw.ElapsedMilliseconds,
            visitor.Tables.Count, operations.Count, schemas.Count);

        return descriptor;
    }

    private static SchemaDescriptor BuildEntitySchema(TableInfo table)
    {
        var properties = table.Columns.Select(col =>
        {
            var (type, format) = SqlTypeMapping.MapToOpenApi(col.SqlType);
            var maxLen = SqlTypeMapping.ExtractMaxLength(col.SqlType);

            return new Models.PropertyDescriptor
            {
                Name = NamingHelper.ToCamelCase(col.Name),
                Type = type,
                Format = format,
                Description = col.IsPrimaryKey ? "Primary key" :
                              col.IsComputed ? "Computed column" :
                              col.IsRowVersion ? "Row version (concurrency token)" : string.Empty,
                Required = !col.IsNullable && !col.HasDefault,
                IsNullable = col.IsNullable,
                RefSchemaName = col.ForeignKeyTable is not null
                    ? NamingHelper.ToPascalCase(col.ForeignKeyTable) : null,
                MaxLength = maxLen
            };
        }).ToList();

        return new SchemaDescriptor
        {
            Name = NamingHelper.ToPascalCase(table.TableName),
            Description = $"Entity representing the {table.SchemaName}.{table.TableName} table",
            Type = "object",
            Properties = properties,
            RequiredProperties = properties.Where(p => p.Required).Select(p => p.Name).ToList()
        };
    }

    private static SchemaDescriptor BuildCreateRequestSchema(TableInfo table)
    {
        var properties = table.Columns
            .Where(c => !c.IsPrimaryKey && !c.IsComputed && !c.IsRowVersion)
            .Select(col =>
            {
                var (type, format) = SqlTypeMapping.MapToOpenApi(col.SqlType);
                var maxLen = SqlTypeMapping.ExtractMaxLength(col.SqlType);

                return new Models.PropertyDescriptor
                {
                    Name = NamingHelper.ToCamelCase(col.Name),
                    Type = type,
                    Format = format,
                    Required = !col.IsNullable && !col.HasDefault,
                    IsNullable = col.IsNullable,
                    RefSchemaName = col.ForeignKeyTable is not null
                        ? NamingHelper.ToPascalCase(col.ForeignKeyTable) : null,
                    MaxLength = maxLen
                };
            }).ToList();

        return new SchemaDescriptor
        {
            Name = $"Create{NamingHelper.ToPascalCase(table.TableName)}Request",
            Description = $"Request body for creating a {table.TableName}",
            Type = "object",
            Properties = properties,
            RequiredProperties = properties.Where(p => p.Required).Select(p => p.Name).ToList()
        };
    }

    private static SchemaDescriptor BuildUpdateRequestSchema(TableInfo table)
    {
        var properties = table.Columns
            .Where(c => !c.IsPrimaryKey && !c.IsComputed && !c.IsRowVersion)
            .Select(col =>
            {
                var (type, format) = SqlTypeMapping.MapToOpenApi(col.SqlType);
                var maxLen = SqlTypeMapping.ExtractMaxLength(col.SqlType);

                return new Models.PropertyDescriptor
                {
                    Name = NamingHelper.ToCamelCase(col.Name),
                    Type = type,
                    Format = format,
                    Required = false, // All optional for update
                    IsNullable = true,
                    RefSchemaName = col.ForeignKeyTable is not null
                        ? NamingHelper.ToPascalCase(col.ForeignKeyTable) : null,
                    MaxLength = maxLen
                };
            }).ToList();

        return new SchemaDescriptor
        {
            Name = $"Update{NamingHelper.ToPascalCase(table.TableName)}Request",
            Description = $"Request body for updating a {table.TableName}",
            Type = "object",
            Properties = properties
        };
    }

    private static List<SchemaDescriptor> BuildEnumSchemas(TableInfo table)
    {
        var enums = new List<SchemaDescriptor>();

        foreach (var col in table.Columns)
        {
            if (col.CheckConstraintValues is { Count: > 0 })
            {
                enums.Add(new SchemaDescriptor
                {
                    Name = $"{NamingHelper.ToPascalCase(table.TableName)}{NamingHelper.ToPascalCase(col.Name)}",
                    Description = $"Allowed values for {table.TableName}.{col.Name}",
                    Type = "string",
                    IsEnum = true,
                    EnumValues = col.CheckConstraintValues
                });
            }
        }

        return enums;
    }

    private static List<OperationDescriptor> BuildCrudOperations(TableInfo table, string tag)
    {
        var tablePascal = NamingHelper.ToPascalCase(table.TableName);
        var tableCamel = NamingHelper.ToCamelCase(table.TableName);
        var tableKebab = NamingHelper.ToKebabCase(table.TableName);
        var basePath = $"/api/{tableKebab}";

        var pkType = "string";
        var pkFormat = "uuid";
        if (table.PrimaryKeyColumn is not null)
        {
            var (t, f) = SqlTypeMapping.MapToOpenApi(table.PrimaryKeyColumn.SqlType);
            pkType = t;
            pkFormat = f ?? string.Empty;
        }

        return
        [
            // List
            new OperationDescriptor
            {
                OperationId = $"list{tablePascal}",
                HttpMethod = "GET",
                Path = basePath,
                Summary = $"List all {table.TableName} records",
                Tag = tag,
                QueryParameters =
                [
                    new ParameterDescriptor { Name = "page", Location = "query", Type = "integer", Format = "int32", DefaultValue = "1" },
                    new ParameterDescriptor { Name = "pageSize", Location = "query", Type = "integer", Format = "int32", DefaultValue = "20" }
                ],
                Responses = new Dictionary<string, ResponseDescriptor>
                {
                    ["200"] = new() { StatusCode = "200", Description = $"List of {table.TableName} records" }
                }
            },
            // Get by ID
            new OperationDescriptor
            {
                OperationId = $"get{tablePascal}",
                HttpMethod = "GET",
                Path = $"{basePath}/{{id}}",
                Summary = $"Get a {table.TableName} by ID",
                Tag = tag,
                PathParameters =
                [
                    new ParameterDescriptor { Name = "id", Location = "path", Type = pkType, Format = pkFormat, Required = true }
                ],
                Responses = new Dictionary<string, ResponseDescriptor>
                {
                    ["200"] = new() { StatusCode = "200", Description = $"The {table.TableName} record" },
                    ["404"] = new() { StatusCode = "404", Description = "Not found" }
                }
            },
            // Create
            new OperationDescriptor
            {
                OperationId = $"create{tablePascal}",
                HttpMethod = "POST",
                Path = basePath,
                Summary = $"Create a new {table.TableName}",
                Tag = tag,
                RequestBody = new SchemaDescriptor { Name = $"Create{tablePascal}Request" },
                Responses = new Dictionary<string, ResponseDescriptor>
                {
                    ["201"] = new() { StatusCode = "201", Description = $"Created {table.TableName}" },
                    ["400"] = new() { StatusCode = "400", Description = "Validation error" }
                }
            },
            // Update
            new OperationDescriptor
            {
                OperationId = $"update{tablePascal}",
                HttpMethod = "PUT",
                Path = $"{basePath}/{{id}}",
                Summary = $"Update an existing {table.TableName}",
                Tag = tag,
                PathParameters =
                [
                    new ParameterDescriptor { Name = "id", Location = "path", Type = pkType, Format = pkFormat, Required = true }
                ],
                RequestBody = new SchemaDescriptor { Name = $"Update{tablePascal}Request" },
                Responses = new Dictionary<string, ResponseDescriptor>
                {
                    ["200"] = new() { StatusCode = "200", Description = $"Updated {table.TableName}" },
                    ["404"] = new() { StatusCode = "404", Description = "Not found" }
                }
            },
            // Delete
            new OperationDescriptor
            {
                OperationId = $"delete{tablePascal}",
                HttpMethod = "DELETE",
                Path = $"{basePath}/{{id}}",
                Summary = $"Delete a {table.TableName}",
                Tag = tag,
                PathParameters =
                [
                    new ParameterDescriptor { Name = "id", Location = "path", Type = pkType, Format = pkFormat, Required = true }
                ],
                Responses = new Dictionary<string, ResponseDescriptor>
                {
                    ["200"] = new() { StatusCode = "200", Description = $"Deleted {table.TableName}" },
                    ["404"] = new() { StatusCode = "404", Description = "Not found" }
                }
            }
        ];
    }

    private static OperationDescriptor BuildProcedureOperation(ProcedureInfo proc)
    {
        var httpMethod = InferHttpMethodFromName(proc.Name);
        var pascalName = NamingHelper.ToPascalCase(proc.Name);
        var kebabName = NamingHelper.ToKebabCase(proc.Name);

        var parameters = proc.Parameters.Select(p =>
        {
            var (type, format) = SqlTypeMapping.MapToOpenApi(p.SqlType);
            return new ParameterDescriptor
            {
                Name = NamingHelper.ToCamelCase(p.Name.TrimStart('@')),
                Location = httpMethod == "GET" ? "query" : "query",
                Type = type,
                Format = format ?? string.Empty,
                Required = !p.HasDefault
            };
        }).ToList();

        SchemaDescriptor? requestBody = null;
        if (httpMethod is "POST" or "PUT" or "PATCH" && parameters.Count > 0)
        {
            requestBody = new SchemaDescriptor
            {
                Name = $"{pascalName}Request",
                Type = "object",
                Properties = parameters.Select(p => new Models.PropertyDescriptor
                {
                    Name = p.Name,
                    Type = p.Type,
                    Format = p.Format,
                    Required = p.Required
                }).ToList()
            };
            parameters = [];
        }

        return new OperationDescriptor
        {
            OperationId = NamingHelper.ToCamelCase(proc.Name),
            HttpMethod = httpMethod,
            Path = $"/api/procedures/{kebabName}",
            Summary = $"Execute stored procedure {proc.SchemaName}.{proc.Name}",
            Tag = "Procedures",
            QueryParameters = parameters,
            RequestBody = requestBody,
            Responses = new Dictionary<string, ResponseDescriptor>
            {
                ["200"] = new() { StatusCode = "200", Description = "Procedure result" }
            }
        };
    }

    private static string InferHttpMethodFromName(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.StartsWith("get", StringComparison.Ordinal) || lower.StartsWith("list", StringComparison.Ordinal) || lower.StartsWith("find", StringComparison.Ordinal) ||
            lower.StartsWith("search", StringComparison.Ordinal) || lower.StartsWith("select", StringComparison.Ordinal) || lower.StartsWith("read", StringComparison.Ordinal))
            return "GET";
        if (lower.StartsWith("delete", StringComparison.Ordinal) || lower.StartsWith("remove", StringComparison.Ordinal) || lower.StartsWith("drop", StringComparison.Ordinal))
            return "DELETE";
        if (lower.StartsWith("update", StringComparison.Ordinal) || lower.StartsWith("modify", StringComparison.Ordinal) || lower.StartsWith("set", StringComparison.Ordinal) ||
            lower.StartsWith("edit", StringComparison.Ordinal) || lower.StartsWith("change", StringComparison.Ordinal))
            return "PUT";
        return "POST"; // create, insert, usp_, sp_, etc.
    }

    private static (string? Title, string? Description) ExtractLeadingComment(string sqlText)
    {
        // Extract title/description from leading -- or /* */ comment blocks
        var lines = sqlText.Split('\n');
        var commentLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("--", StringComparison.Ordinal))
                commentLines.Add(line[2..].Trim());
            else if (line.StartsWith("/*", StringComparison.Ordinal))
                continue; // skip delimiter
            else if (line.StartsWith('*') && !line.StartsWith("*/", StringComparison.Ordinal))
                commentLines.Add(line[1..].Trim());
            else if (line.StartsWith("*/", StringComparison.Ordinal))
                break;
            else if (string.IsNullOrWhiteSpace(line))
                continue;
            else
                break;
        }

        if (commentLines.Count == 0) return (null, null);

        var title = commentLines[0];
        var desc = commentLines.Count > 1
            ? string.Join(" ", commentLines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
            : null;
        return (title, desc);
    }

    // ─── TSql AST Visitor ─────────────────────────────────────────────────

    private sealed class SqlVisitor : TSqlFragmentVisitor
    {
        public string? DatabaseName { get; private set; }
        public List<TableInfo> Tables { get; } = [];
        public List<ProcedureInfo> Procedures { get; } = [];

        private readonly Dictionary<string, TableInfo> _tableMap = new(StringComparer.OrdinalIgnoreCase);

        public override void Visit(UseStatement node)
        {
            DatabaseName = node.DatabaseName?.Value;
        }

        public override void Visit(CreateTableStatement node)
        {
            var schemaName = node.SchemaObjectName.SchemaIdentifier?.Value ?? "dbo";
            var tableName = node.SchemaObjectName.BaseIdentifier.Value;

            var table = new TableInfo
            {
                SchemaName = schemaName,
                TableName = tableName
            };

            // Process columns
            foreach (var colDef in node.Definition.ColumnDefinitions)
            {
                var col = new ColumnInfo
                {
                    Name = colDef.ColumnIdentifier.Value,
                    SqlType = GetDataTypeName(colDef.DataType),
                    IsNullable = IsColumnNullable(colDef),
                    HasDefault = colDef.DefaultConstraint is not null,
                    IsComputed = colDef.ComputedColumnExpression is not null,
                    IsRowVersion = IsRowVersionColumn(colDef)
                };

                // Check for IDENTITY
                if (colDef.IdentityOptions is not null)
                    col = col with { HasDefault = true };

                // Check for PRIMARY KEY inline constraint
                foreach (var constraint in colDef.Constraints)
                {
                    if (constraint is UniqueConstraintDefinition { IsPrimaryKey: true })
                        col = col with { IsPrimaryKey = true };
                }

                // Check for CHECK constraints with IN lists
                foreach (var constraint in colDef.Constraints)
                {
                    if (constraint is CheckConstraintDefinition checkConstraint)
                    {
                        var values = ExtractInListValues(checkConstraint.CheckCondition);
                        if (values.Count > 0)
                            col = col with { CheckConstraintValues = values };
                    }
                }

                table.Columns.Add(col);
            }

            // Process table-level constraints
            foreach (var constraint in node.Definition.TableConstraints)
            {
                if (constraint is UniqueConstraintDefinition { IsPrimaryKey: true } pkConstraint)
                {
                    foreach (var pkCol in pkConstraint.Columns)
                    {
                        var colName = pkCol.Column.MultiPartIdentifier.Identifiers.Last().Value;
                        var existing = table.Columns.FindIndex(c =>
                            c.Name.Equals(colName, StringComparison.OrdinalIgnoreCase));
                        if (existing >= 0)
                            table.Columns[existing] = table.Columns[existing] with { IsPrimaryKey = true };
                    }
                }
                else if (constraint is ForeignKeyConstraintDefinition fkConstraint)
                {
                    var refTable = fkConstraint.ReferenceTableName.BaseIdentifier.Value;
                    foreach (var fkCol in fkConstraint.Columns)
                    {
                        var colName = fkCol.Value;
                        var existing = table.Columns.FindIndex(c =>
                            c.Name.Equals(colName, StringComparison.OrdinalIgnoreCase));
                        if (existing >= 0)
                            table.Columns[existing] = table.Columns[existing] with { ForeignKeyTable = refTable };
                    }
                }
                else if (constraint is CheckConstraintDefinition tableCheck)
                {
                    // Try to match column from expression text
                    var exprText = GetFragmentText(tableCheck.CheckCondition);
                    var values = ExtractInListValues(tableCheck.CheckCondition);
                    if (values.Count > 0)
                    {
                        // Try to find the column name referenced
                        var colRef = FindColumnReference(tableCheck.CheckCondition);
                        if (colRef is not null)
                        {
                            var existing = table.Columns.FindIndex(c =>
                                c.Name.Equals(colRef, StringComparison.OrdinalIgnoreCase));
                            if (existing >= 0)
                                table.Columns[existing] = table.Columns[existing] with { CheckConstraintValues = values };
                        }
                    }
                }
            }

            table.PrimaryKeyColumn = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);

            _tableMap[tableName] = table;
            Tables.Add(table);
        }

        public override void Visit(CreateProcedureStatement node)
        {
            var schemaName = node.ProcedureReference.Name.SchemaIdentifier?.Value ?? "dbo";
            var procName = node.ProcedureReference.Name.BaseIdentifier.Value;

            var proc = new ProcedureInfo
            {
                SchemaName = schemaName,
                Name = procName
            };

            foreach (var param in node.Parameters)
            {
                proc.Parameters.Add(new ProcedureParameterInfo
                {
                    Name = param.VariableName.Value,
                    SqlType = GetDataTypeName(param.DataType),
                    HasDefault = param.Value is not null
                });
            }

            Procedures.Add(proc);
        }

        private static string GetDataTypeName(DataTypeReference? dataType)
        {
            if (dataType is null) return "NVARCHAR(MAX)";

            var name = dataType.Name?.BaseIdentifier?.Value ?? "NVARCHAR";
            if (dataType is ParameterizedDataTypeReference pdt && pdt.Parameters.Count > 0)
            {
                var args = string.Join(",", pdt.Parameters.Select(p => p.Value));
                return $"{name}({args})";
            }
            return name;
        }

        private static bool IsColumnNullable(ColumnDefinition colDef)
        {
            // Default is nullable unless NOT NULL constraint is specified
            foreach (var constraint in colDef.Constraints)
            {
                if (constraint is NullableConstraintDefinition nullConstraint)
                    return nullConstraint.Nullable;
            }
            return true; // SQL Server default
        }

        private static bool IsRowVersionColumn(ColumnDefinition colDef)
        {
            var typeName = GetDataTypeName(colDef.DataType).ToLowerInvariant();
            return typeName is "timestamp" or "rowversion";
        }

        private static List<string> ExtractInListValues(BooleanExpression? expr)
        {
            if (expr is InPredicate inPred)
            {
                return inPred.Values
                    .OfType<StringLiteral>()
                    .Select(s => s.Value)
                    .ToList();
            }
            return [];
        }

        private static string? FindColumnReference(BooleanExpression? expr)
        {
            if (expr is InPredicate inPred && inPred.Expression is ColumnReferenceExpression colRef)
            {
                return colRef.MultiPartIdentifier.Identifiers.Last().Value;
            }
            return null;
        }

        private static string GetFragmentText(TSqlFragment fragment)
        {
            // Reconstruct text from token stream
            var sb = new System.Text.StringBuilder();
            for (var i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
            {
                sb.Append(fragment.ScriptTokenStream[i].Text);
            }
            return sb.ToString();
        }
    }

    // ─── Internal Records ─────────────────────────────────────────────────

    internal sealed record TableInfo
    {
        public required string SchemaName { get; init; }
        public required string TableName { get; init; }
        public List<ColumnInfo> Columns { get; init; } = [];
        public ColumnInfo? PrimaryKeyColumn { get; set; }
    }

    internal sealed record ColumnInfo
    {
        public required string Name { get; init; }
        public required string SqlType { get; init; }
        public bool IsNullable { get; init; }
        public bool IsPrimaryKey { get; init; }
        public bool HasDefault { get; init; }
        public bool IsComputed { get; init; }
        public bool IsRowVersion { get; init; }
        public string? ForeignKeyTable { get; init; }
        public List<string>? CheckConstraintValues { get; init; }
    }

    internal sealed record ProcedureInfo
    {
        public required string SchemaName { get; init; }
        public required string Name { get; init; }
        public List<ProcedureParameterInfo> Parameters { get; init; } = [];
    }

    internal sealed record ProcedureParameterInfo
    {
        public required string Name { get; init; }
        public required string SqlType { get; init; }
        public bool HasDefault { get; init; }
    }
}
