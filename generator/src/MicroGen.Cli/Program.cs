using MicroGen.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("microgen");
    config.SetApplicationVersion("1.0.0");

    config.AddCommand<ScanCommand>("scan")
        .WithDescription("Scan workspace for OpenAPI specs and show what would be generated")
        .WithExample("scan", "--input", "./openapis")
        .WithExample("scan", "--input", "./openapis", "--domain", "emergency");

    config.AddCommand<GenerateCommand>("generate")
        .WithDescription("Generate microservice solutions from OpenAPI specs")
        .WithExample("generate", "--input", "./openapis", "--output", "./generated")
        .WithExample("generate", "--input", "./openapis", "--output", "./generated", "--domain", "emergency")
        .WithExample("generate", "--input", "./openapis", "--output", "./generated", "--dry-run");

    config.AddCommand<ParseCommand>("parse")
        .WithDescription("Parse a single source file (SQL, CSV, GraphQL, HTML, OpenAPI) and display its structure")
        .WithExample("parse", "--file", "./schema.sql")
        .WithExample("parse", "--file", "./data.csv", "--domain", "analytics")
        .WithExample("parse", "--url", "https://example.com/swagger");

    config.AddCommand<ValidateCommand>("validate")
        .WithDescription("Validate OpenAPI specs without generating code")
        .WithExample("validate", "--input", "./openapis");

    config.AddCommand<InitCommand>("init")
        .WithDescription("Create a default microgen.json configuration file")
        .WithExample("init")
        .WithExample("init", "--output", "./my-project");
});

return await app.RunAsync(args);
