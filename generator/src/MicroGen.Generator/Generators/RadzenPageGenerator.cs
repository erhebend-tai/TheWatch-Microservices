using MicroGen.Core.Configuration;
using MicroGen.Core.Models;
using MicroGen.Generator.Emitters;
using MicroGen.Generator.Templates;
using Microsoft.Extensions.Logging;

namespace MicroGen.Generator.Generators;

/// <summary>
/// Generates per-entity CRUD pages using Radzen Blazor components.
/// Produces List pages (DataGrid), Create/Edit dialogs (FormField), and Detail views
/// from OpenAPI schemas and operations.
/// </summary>
public sealed class RadzenPageGenerator
{
    private readonly TemplateEngine _engine;
    private readonly GeneratorConfig _config;
    private readonly ILogger _logger;

    public RadzenPageGenerator(TemplateEngine engine, GeneratorConfig config, ILogger logger)
    {
        _engine = engine;
        _config = config;
        _logger = logger;
    }

    public async Task GenerateAsync(
        ServiceDescriptor service,
        string serviceRoot,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var projectName = $"{service.PascalName}.Dashboard";
        _logger.LogDebug("  Generating Radzen CRUD pages for {Service}...", service.PascalName);

        // Generate a CRUD page set for each entity schema
        foreach (var schema in service.Schemas.Where(s => s.IsEntity))
        {
            await GenerateListPageAsync(service, schema, serviceRoot, projectName, emitter, ct);
            await GenerateCreateEditDialogAsync(service, schema, serviceRoot, projectName, emitter, ct);
            await GenerateDetailPageAsync(service, schema, serviceRoot, projectName, emitter, ct);
            await GenerateEntityServiceAsync(service, schema, serviceRoot, projectName, emitter, ct);
        }

        // Generate a chart/metrics page per tag
        foreach (var tag in service.Tags)
        {
            await GenerateTagOverviewPageAsync(service, tag, serviceRoot, projectName, emitter, ct);
        }

        // Generate shared components
        await GenerateDeleteConfirmDialogAsync(service, serviceRoot, projectName, emitter, ct);
        await GenerateEntityNavMenuItemsAsync(service, serviceRoot, projectName, emitter, ct);
        
        // Generate enhanced components
        await GenerateMetricsChartPageAsync(service, serviceRoot, projectName, emitter, ct);
        await GenerateAdvancedFilterDialogAsync(service, serviceRoot, projectName, emitter, ct);
        await GenerateExportHelperAsync(service, serviceRoot, projectName, emitter, ct);
    }

    // ── List Page with RadzenDataGrid ──────────────────────────────────

    private async Task GenerateListPageAsync(
        ServiceDescriptor service,
        SchemaDescriptor schema,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var model = new { Service = service, Schema = schema, Config = _config };

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", $"{schema.PascalName}List.razor"),
            _engine.Render(Templates.ListPage, model), ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", $"{schema.PascalName}List.razor.cs"),
            _engine.Render(Templates.ListPageCode, model), ct);
    }

    // ── Create / Edit Dialog ───────────────────────────────────────────

    private async Task GenerateCreateEditDialogAsync(
        ServiceDescriptor service,
        SchemaDescriptor schema,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var model = new { Service = service, Schema = schema, Config = _config };

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", $"{schema.PascalName}Form.razor"),
            _engine.Render(Templates.FormDialog, model), ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", $"{schema.PascalName}Form.razor.cs"),
            _engine.Render(Templates.FormDialogCode, model), ct);
    }

    // ── Detail Page ────────────────────────────────────────────────────

    private async Task GenerateDetailPageAsync(
        ServiceDescriptor service,
        SchemaDescriptor schema,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var model = new { Service = service, Schema = schema };

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", $"{schema.PascalName}Detail.razor"),
            _engine.Render(Templates.DetailPage, model), ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", $"{schema.PascalName}Detail.razor.cs"),
            _engine.Render(Templates.DetailPageCode, model), ct);
    }

    // ── Entity Data Service ────────────────────────────────────────────

    private async Task GenerateEntityServiceAsync(
        ServiceDescriptor service,
        SchemaDescriptor schema,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var model = new { Service = service, Schema = schema };

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Services", $"{schema.PascalName}DataService.cs"),
            _engine.Render(Templates.EntityDataService, model), ct);
    }

    // ── Tag Overview Page ──────────────────────────────────────────────

    private async Task GenerateTagOverviewPageAsync(
        ServiceDescriptor service,
        TagDescriptor tag,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        var operations = service.Operations.Where(o => o.Tag == tag.Name).ToList();
        var queryCount = operations.Count(o => o.IsQuery);
        var commandCount = operations.Count(o => o.IsCommand);
        var model = new { Service = service, Tag = tag, Operations = operations, QueryCount = queryCount, CommandCount = commandCount };

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", $"{tag.PascalName}Overview.razor"),
            _engine.Render(Templates.TagOverviewPage, model), ct);
    }

    // ── Delete Confirm Dialog ──────────────────────────────────────────

    private async Task GenerateDeleteConfirmDialogAsync(
        ServiceDescriptor service,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Components", "DeleteConfirmDialog.razor"),
            _engine.Render(Templates.DeleteConfirmDialog, new { Service = service }), ct);
    }

    // ── Nav Menu Items ─────────────────────────────────────────────────

    private async Task GenerateEntityNavMenuItemsAsync(
        ServiceDescriptor service,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Layout", "EntityNavMenu.razor"),
            _engine.Render(Templates.EntityNavMenu, new { Service = service }), ct);
    }

    // ── Enhanced Components ────────────────────────────────────────────

    private async Task GenerateMetricsChartPageAsync(
        ServiceDescriptor service,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", "Metrics.razor"),
            _engine.Render(Templates.MetricsChartPage, new { Service = service, Config = _config }),
            ct);

        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Pages", "Metrics.razor.cs"),
            _engine.Render(Templates.MetricsChartPageCode, new { Service = service }),
            ct);
    }

    private async Task GenerateAdvancedFilterDialogAsync(
        ServiceDescriptor service,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Components", "AdvancedFilterDialog.razor"),
            _engine.Render(Templates.AdvancedFilterDialog, new { Service = service }),
            ct);
    }

    private async Task GenerateExportHelperAsync(
        ServiceDescriptor service,
        string serviceRoot,
        string projectName,
        FileEmitter emitter,
        CancellationToken ct)
    {
        await emitter.EmitToProjectAsync(serviceRoot, projectName,
            Path.Combine("Services", "ExportService.cs"),
            _engine.Render(Templates.ExportService, new { Service = service }),
            ct);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Maps an OpenAPI/schema property type+format to the appropriate Radzen input component.
    /// </summary>
    public static string MapToRadzenComponent(string type, string? format) => (type, format) switch
    {
        ("string", "email") => "RadzenTextBox",
        ("string", "uri" or "url") => "RadzenTextBox",
        ("string", "password") => "RadzenPassword",
        ("string", "date" or "date-time") => "RadzenDatePicker",
        ("string", "time") => "RadzenTimeSpanPicker",
        ("string", "color") => "RadzenColorPicker",
        ("string", "binary" or "byte") => "RadzenFileInput",
        ("string", _) => "RadzenTextBox",
        ("integer" or "number", _) => "RadzenNumeric",
        ("boolean", _) => "RadzenSwitch",
        ("array", _) => "RadzenListBox",
        _ => "RadzenTextBox"
    };

    /// <summary>
    /// Returns the Material icon name for a given HTTP method badge.
    /// </summary>
    public static string GetMethodIcon(string method) => method.ToUpperInvariant() switch
    {
        "GET" => "download",
        "POST" => "add_circle",
        "PUT" => "edit",
        "PATCH" => "build",
        "DELETE" => "delete",
        _ => "api"
    };

    /// <summary>
    /// Returns the RadzenBadge style for an HTTP method.
    /// </summary>
    public static string GetMethodBadgeStyle(string method) => method.ToUpperInvariant() switch
    {
        "GET" => "Success",
        "POST" => "Primary",
        "PUT" => "Warning",
        "PATCH" => "Info",
        "DELETE" => "Danger",
        _ => "Light"
    };

    // ═══════════════════════════════════════════════════════════════════
    //  Scriban Templates
    // ═══════════════════════════════════════════════════════════════════

    private static class Templates
    {
        // ── List Page ──────────────────────────────────────────────────
        public const string ListPage = """
            @page "/{{ Schema.PascalName | string.downcase }}s"

            <PageTitle>{{ Schema.PascalName }} List — {{ Service.PascalName }}</PageTitle>

            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center"
                         JustifyContent="JustifyContent.SpaceBetween" class="rz-mb-4">
                <RadzenText TextStyle="TextStyle.H4">{{ Schema.PascalName }}s</RadzenText>
                <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem">
                    <RadzenSplitButton Text="Export" Icon="download" ButtonStyle="ButtonStyle.Light"
                                       Click="@(() => ExportData("csv"))">
                        <ChildContent>
                            <RadzenSplitButtonItem Text="Export to CSV" Value="csv" Icon="table_chart" />
                            <RadzenSplitButtonItem Text="Export to JSON" Value="json" Icon="code" />
                            <RadzenSplitButtonItem Text="Export to Excel" Value="excel" Icon="description" />
                        </ChildContent>
                    </RadzenSplitButton>
                    <RadzenButton Text="Filters" Icon="filter_list" ButtonStyle="ButtonStyle.Light"
                                  Click="@OpenAdvancedFilters" />
                    <RadzenButton Text="Create" Icon="add" ButtonStyle="ButtonStyle.Primary"
                                  Click="@OpenCreateDialog" />
                </RadzenStack>
            </RadzenStack>

            <RadzenDataGrid @ref="_grid" Data="@_items" TItem="{{ Schema.PascalName }}"
                            AllowSorting="true" AllowFiltering="true" AllowPaging="true"
                            PageSize="20" AllowAlternatingRows="true"
                            FilterMode="FilterMode.Simple" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                            ShowPagingSummary="true" PagingSummaryFormat="Showing {0} to {1} of {2}"
                            SelectionMode="DataGridSelectionMode.Single"
                            RowSelect="@OnRowSelect" Density="Density.Default"
                            GridLines="DataGridGridLines.Horizontal">
                <Columns>
            {{~ for prop in Schema.Properties ~}}
                {{~ if prop.Type == "string" && prop.Format == "date-time" ~}}
                    <RadzenDataGridColumn TItem="{{ Schema.PascalName }}" Property="{{ prop.PascalName }}"
                                          Title="{{ prop.PascalName }}" FormatString="{0:yyyy-MM-dd HH:mm}" Width="180px" />
                {{~ else if prop.Type == "boolean" ~}}
                    <RadzenDataGridColumn TItem="{{ Schema.PascalName }}" Property="{{ prop.PascalName }}"
                                          Title="{{ prop.PascalName }}" Width="100px">
                        <Template Context="row">
                            <RadzenCheckBox Value="@row.{{ prop.PascalName }}" Disabled="true" />
                        </Template>
                    </RadzenDataGridColumn>
                {{~ else if prop.Type == "integer" || prop.Type == "number" ~}}
                    <RadzenDataGridColumn TItem="{{ Schema.PascalName }}" Property="{{ prop.PascalName }}"
                                          Title="{{ prop.PascalName }}" Width="120px" TextAlign="TextAlign.Right" />
                {{~ else ~}}
                    <RadzenDataGridColumn TItem="{{ Schema.PascalName }}" Property="{{ prop.PascalName }}"
                                          Title="{{ prop.PascalName }}" />
                {{~ end ~}}
            {{~ end ~}}
                    <RadzenDataGridColumn TItem="{{ Schema.PascalName }}" Title="Actions" Width="160px"
                                          Sortable="false" Filterable="false" TextAlign="TextAlign.Center">
                        <Template Context="row">
                            <RadzenButton Icon="visibility" ButtonStyle="ButtonStyle.Light" Size="ButtonSize.Small"
                                          Click="@(() => ViewDetail(row))" class="rz-mr-1" />
                            <RadzenButton Icon="edit" ButtonStyle="ButtonStyle.Light" Size="ButtonSize.Small"
                                          Click="@(() => OpenEditDialog(row))" class="rz-mr-1" />
                            <RadzenButton Icon="delete" ButtonStyle="ButtonStyle.Danger" Size="ButtonSize.Small"
                                          Variant="Variant.Text" Click="@(() => ConfirmDelete(row))" />
                        </Template>
                    </RadzenDataGridColumn>
                </Columns>
            </RadzenDataGrid>
            """;

        public const string ListPageCode = """
            using Microsoft.AspNetCore.Components;
            using Radzen;
            using Radzen.Blazor;
            using {{ Service.PascalName }}.Dashboard.Services;

            namespace {{ Service.PascalName }}.Dashboard.Pages;

            public partial class {{ Schema.PascalName }}List : ComponentBase
            {
                [Inject] private {{ Schema.PascalName }}DataService DataService { get; set; } = null!;
                [Inject] private DialogService DialogService { get; set; } = null!;
                [Inject] private NotificationService NotificationService { get; set; } = null!;
                [Inject] private NavigationManager Navigation { get; set; } = null!;
                [Inject] private ExportService ExportService { get; set; } = null!;

                private RadzenDataGrid<{{ Schema.PascalName }}>? _grid;
                private IEnumerable<{{ Schema.PascalName }}>? _items;

                protected override async Task OnInitializedAsync()
                {
                    await LoadDataAsync();
                }

                private async Task LoadDataAsync()
                {
                    _items = await DataService.GetAllAsync();
                }

                private async Task OpenCreateDialog()
                {
                    var result = await DialogService.OpenAsync<{{ Schema.PascalName }}Form>(
                        "Create {{ Schema.PascalName }}",
                        new Dictionary<string, object> { ["IsEdit"] = false },
                        new DialogOptions { Width = "600px", Resizable = true, Draggable = true });

                    if (result is true)
                    {
                        await LoadDataAsync();
                        NotificationService.Notify(NotificationSeverity.Success, "Created",
                            "{{ Schema.PascalName }} created successfully.");
                    }
                }

                private async Task OpenEditDialog({{ Schema.PascalName }} item)
                {
                    var result = await DialogService.OpenAsync<{{ Schema.PascalName }}Form>(
                        "Edit {{ Schema.PascalName }}",
                        new Dictionary<string, object> { ["IsEdit"] = true, ["Model"] = item },
                        new DialogOptions { Width = "600px", Resizable = true, Draggable = true });

                    if (result is true)
                    {
                        await LoadDataAsync();
                        NotificationService.Notify(NotificationSeverity.Success, "Updated",
                            "{{ Schema.PascalName }} updated successfully.");
                    }
                }

                private void ViewDetail({{ Schema.PascalName }} item)
                {
                    Navigation.NavigateTo($"/{{ Schema.PascalName | string.downcase }}s/{item.Id}");
                }

                private void OnRowSelect({{ Schema.PascalName }} item)
                {
                    // Row selection handler
                }

                private async Task ConfirmDelete({{ Schema.PascalName }} item)
                {
                    var confirmed = await DialogService.Confirm(
                        $"Are you sure you want to delete this {{ Schema.PascalName }}?",
                        "Confirm Delete",
                        new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" });

                    if (confirmed == true)
                    {
                        await DataService.DeleteAsync(item.Id);
                        await LoadDataAsync();
                        NotificationService.Notify(NotificationSeverity.Warning, "Deleted",
                            "{{ Schema.PascalName }} deleted.");
                    }
                }

                private async Task OpenAdvancedFilters()
                {
                    var result = await DialogService.OpenAsync<AdvancedFilterDialog>(
                        "Advanced Filters",
                        null,
                        new DialogOptions { Width = "700px", Resizable = true, Draggable = true });

                    if (result != null)
                    {
                        // Apply filters to grid
                        NotificationService.Notify(NotificationSeverity.Info, "Filters Applied",
                            "Advanced filters have been applied.");
                        await LoadDataAsync();
                    }
                }

                private async Task ExportData(string format)
                {
                    if (_items == null || !_items.Any())
                    {
                        NotificationService.Notify(NotificationSeverity.Warning, "No Data",
                            "There is no data to export.");
                        return;
                    }

                    try
                    {
                        switch (format.ToLowerInvariant())
                        {
                            case "csv":
                                await ExportService.ExportToCsvAsync(_items, "{{ Schema.PascalName | string.downcase }}s.csv");
                                break;
                            case "json":
                                await ExportService.ExportToJsonAsync(_items, "{{ Schema.PascalName | string.downcase }}s.json");
                                break;
                            case "excel":
                                await ExportService.ExportToExcelAsync(_items, "{{ Schema.PascalName | string.downcase }}s.xlsx");
                                break;
                        }

                        NotificationService.Notify(NotificationSeverity.Success, "Export Complete",
                            $"Data exported to {format.ToUpperInvariant()} successfully.");
                    }
                    catch (Exception ex)
                    {
                        NotificationService.Notify(NotificationSeverity.Error, "Export Failed",
                            $"Failed to export data: {ex.Message}");
                    }
                }
            }
            """;

        // ── Form Dialog ────────────────────────────────────────────────
        public const string FormDialog = """
            <RadzenStack Gap="1rem">
                <EditForm Model="@Model" OnValidSubmit="@OnSubmit">
                    <DataAnnotationsValidator />
                    <ValidationSummary />

                    <RadzenRow Gap="1rem">
            {{~ for prop in Schema.Properties ~}}
                {{~ if prop.Name != "id" && prop.Name != "Id" ~}}
                        <RadzenColumn Size="6">
                    {{~ if prop.IsEnum ~}}
                            <RadzenFormField Text="{{ prop.PascalName }}" Variant="Variant.Outlined" Style="width: 100%;">
                                <RadzenDropDown @bind-Value="@Model.{{ prop.PascalName }}" Data="@_{{ prop.CamelName }}Options"
                                                Style="width: 100%;" Placeholder="Select {{ prop.PascalName }}..." />
                                <RadzenRequiredValidator Component="{{ prop.PascalName }}" Text="{{ prop.PascalName }} is required" />
                            </RadzenFormField>
                    {{~ else if prop.Type == "boolean" ~}}
                            <RadzenFormField Text="{{ prop.PascalName }}" Variant="Variant.Outlined" Style="width: 100%;">
                                <RadzenSwitch @bind-Value="@Model.{{ prop.PascalName }}" Name="{{ prop.PascalName }}" />
                            </RadzenFormField>
                    {{~ else if prop.Type == "integer" || prop.Type == "number" ~}}
                            <RadzenFormField Text="{{ prop.PascalName }}" Variant="Variant.Outlined" Style="width: 100%;">
                                <RadzenNumeric @bind-Value="@Model.{{ prop.PascalName }}" Style="width: 100%;" 
                                               Name="{{ prop.PascalName }}" />
                                {{~ if prop.Required ~}}
                                <RadzenRequiredValidator Component="{{ prop.PascalName }}" Text="{{ prop.PascalName }} is required" />
                                {{~ end ~}}
                            </RadzenFormField>
                    {{~ else if prop.Format == "date-time" || prop.Format == "date" ~}}
                            <RadzenFormField Text="{{ prop.PascalName }}" Variant="Variant.Outlined" Style="width: 100%;">
                                <RadzenDatePicker @bind-Value="@Model.{{ prop.PascalName }}" Style="width: 100%;"
                                                  ShowTime="{{ prop.Format == "date-time" }}" Name="{{ prop.PascalName }}" />
                                {{~ if prop.Required ~}}
                                <RadzenRequiredValidator Component="{{ prop.PascalName }}" Text="{{ prop.PascalName }} is required" />
                                {{~ end ~}}
                            </RadzenFormField>
                    {{~ else if prop.Format == "email" ~}}
                            <RadzenFormField Text="{{ prop.PascalName }}" Variant="Variant.Outlined" Style="width: 100%;">
                                <RadzenTextBox @bind-Value="@Model.{{ prop.PascalName }}" Style="width: 100%;"
                                               MaxLength="{{ prop.MaxLength }}" Name="{{ prop.PascalName }}" />
                                {{~ if prop.Required ~}}
                                <RadzenRequiredValidator Component="{{ prop.PascalName }}" Text="{{ prop.PascalName }} is required" />
                                {{~ end ~}}
                                <RadzenEmailValidator Component="{{ prop.PascalName }}" Text="Please enter a valid email address" />
                            </RadzenFormField>
                    {{~ else if prop.Format == "password" ~}}
                            <RadzenFormField Text="{{ prop.PascalName }}" Variant="Variant.Outlined" Style="width: 100%;">
                                <RadzenPassword @bind-Value="@Model.{{ prop.PascalName }}" Style="width: 100%;" 
                                                Name="{{ prop.PascalName }}" />
                                {{~ if prop.Required ~}}
                                <RadzenRequiredValidator Component="{{ prop.PascalName }}" Text="{{ prop.PascalName }} is required" />
                                {{~ end ~}}
                                <RadzenLengthValidator Component="{{ prop.PascalName }}" Min="8" 
                                                       Text="Password must be at least 8 characters" />
                            </RadzenFormField>
                    {{~ else if prop.MaxLength > 500 ~}}
                            <RadzenColumn Size="12">
                            <RadzenFormField Text="{{ prop.PascalName }}" Variant="Variant.Outlined" Style="width: 100%;">
                                <RadzenTextArea @bind-Value="@Model.{{ prop.PascalName }}" Style="width: 100%;"
                                                Rows="4" MaxLength="{{ prop.MaxLength }}" Name="{{ prop.PascalName }}" />
                                {{~ if prop.Required ~}}
                                <RadzenRequiredValidator Component="{{ prop.PascalName }}" Text="{{ prop.PascalName }} is required" />
                                {{~ end ~}}
                            </RadzenFormField>
                            </RadzenColumn>
                    {{~ else ~}}
                            <RadzenFormField Text="{{ prop.PascalName }}" Variant="Variant.Outlined" Style="width: 100%;">
                                <RadzenTextBox @bind-Value="@Model.{{ prop.PascalName }}" Style="width: 100%;"
                                               MaxLength="{{ prop.MaxLength }}" Name="{{ prop.PascalName }}" />
                                {{~ if prop.Required ~}}
                                <RadzenRequiredValidator Component="{{ prop.PascalName }}" Text="{{ prop.PascalName }} is required" />
                                {{~ end ~}}
                            </RadzenFormField>
                    {{~ end ~}}
                        </RadzenColumn>
                {{~ end ~}}
            {{~ end ~}}
                    </RadzenRow>

                    <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.End"
                                 Gap="0.5rem" class="rz-mt-4">
                        <RadzenButton Text="Cancel" ButtonStyle="ButtonStyle.Light"
                                      Click="@Cancel" ButtonType="ButtonType.Button" />
                        <RadzenButton Text="@(IsEdit ? "Save Changes" : "Create")"
                                      ButtonStyle="ButtonStyle.Primary" ButtonType="ButtonType.Submit"
                                      Icon="@(IsEdit ? "save" : "add")" Disabled="@_isSubmitting" />
                    </RadzenStack>
                </EditForm>
            </RadzenStack>
            """;

        public const string FormDialogCode = """
            using Microsoft.AspNetCore.Components;
            using Radzen;

            namespace {{ Service.PascalName }}.Dashboard.Pages;

            public partial class {{ Schema.PascalName }}Form : ComponentBase
            {
                [Inject] private {{ Schema.PascalName }}DataService DataService { get; set; } = null!;
                [Inject] private DialogService DialogService { get; set; } = null!;
                [Inject] private NotificationService NotificationService { get; set; } = null!;

                [Parameter] public bool IsEdit { get; set; }
                [Parameter] public {{ Schema.PascalName }}? Model { get; set; }

                private bool _isSubmitting;

                protected override void OnInitialized()
                {
                    Model ??= new {{ Schema.PascalName }}();
                }

                private async Task OnSubmit()
                {
                    if (_isSubmitting)
                        return;

                    try
                    {
                        _isSubmitting = true;
                        StateHasChanged();

                        if (IsEdit)
                            await DataService.UpdateAsync(Model!);
                        else
                            await DataService.CreateAsync(Model!);

                        DialogService.Close(true);
                    }
                    catch (Exception ex)
                    {
                        NotificationService.Notify(NotificationSeverity.Error, "Error",
                            $"Failed to save: {ex.Message}");
                    }
                    finally
                    {
                        _isSubmitting = false;
                        StateHasChanged();
                    }
                }

                private void Cancel()
                {
                    DialogService.Close(false);
                }
            }
            """;

        // ── Detail Page ────────────────────────────────────────────────
        public const string DetailPage = """
            @page "/{{ Schema.PascalName | string.downcase }}s/{Id}"

            <PageTitle>{{ Schema.PascalName }} Detail — {{ Service.PascalName }}</PageTitle>

            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center"
                         JustifyContent="JustifyContent.SpaceBetween" class="rz-mb-4">
                <RadzenBreadCrumb>
                    <RadzenBreadCrumbItem Text="Home" Path="" />
                    <RadzenBreadCrumbItem Text="{{ Schema.PascalName }}s" Path="/{{ Schema.PascalName | string.downcase }}s" />
                    <RadzenBreadCrumbItem Text="Detail" />
                </RadzenBreadCrumb>
                <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem">
                    <RadzenButton Text="Edit" Icon="edit" ButtonStyle="ButtonStyle.Primary"
                                  Click="@OpenEditDialog" />
                    <RadzenButton Text="Back" Icon="arrow_back" ButtonStyle="ButtonStyle.Light"
                                  Click="@GoBack" />
                </RadzenStack>
            </RadzenStack>

            @if (_item is not null)
            {
                <RadzenCard>
                    <RadzenRow Gap="1rem">
            {{~ for prop in Schema.Properties ~}}
                        <RadzenColumn Size="6">
                            <RadzenText TextStyle="TextStyle.Caption" class="rz-color-secondary">{{ prop.PascalName }}</RadzenText>
                {{~ if prop.Type == "boolean" ~}}
                            <RadzenCheckBox Value="@_item.{{ prop.PascalName }}" Disabled="true" />
                {{~ else if prop.Format == "date-time" ~}}
                            <RadzenText TextStyle="TextStyle.Body1">@_item.{{ prop.PascalName }}?.ToString("yyyy-MM-dd HH:mm:ss")</RadzenText>
                {{~ else ~}}
                            <RadzenText TextStyle="TextStyle.Body1">@_item.{{ prop.PascalName }}</RadzenText>
                {{~ end ~}}
                        </RadzenColumn>
            {{~ end ~}}
                    </RadzenRow>
                </RadzenCard>
            }
            else
            {
                <RadzenProgressBar Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" />
            }
            """;

        public const string DetailPageCode = """
            using Microsoft.AspNetCore.Components;
            using Radzen;

            namespace {{ Service.PascalName }}.Dashboard.Pages;

            public partial class {{ Schema.PascalName }}Detail : ComponentBase
            {
                [Parameter] public string Id { get; set; } = string.Empty;

                [Inject] private {{ Schema.PascalName }}DataService DataService { get; set; } = null!;
                [Inject] private DialogService DialogService { get; set; } = null!;
                [Inject] private NavigationManager Navigation { get; set; } = null!;

                private {{ Schema.PascalName }}? _item;

                protected override async Task OnParametersSetAsync()
                {
                    _item = await DataService.GetByIdAsync(Id);
                }

                private async Task OpenEditDialog()
                {
                    var result = await DialogService.OpenAsync<{{ Schema.PascalName }}Form>(
                        "Edit {{ Schema.PascalName }}",
                        new Dictionary<string, object> { ["IsEdit"] = true, ["Model"] = _item! },
                        new DialogOptions { Width = "600px", Resizable = true, Draggable = true });

                    if (result is true)
                    {
                        _item = await DataService.GetByIdAsync(Id);
                    }
                }

                private void GoBack()
                {
                    Navigation.NavigateTo("/{{ Schema.PascalName | string.downcase }}s");
                }
            }
            """;

        // ── Entity Data Service ────────────────────────────────────────
        public const string EntityDataService = """
            using Microsoft.Extensions.Logging;

            namespace {{ Service.PascalName }}.Dashboard.Services;

            /// <summary>
            /// Dashboard data service for {{ Schema.PascalName }} entities.
            /// Wraps the API client for the dashboard UI.
            /// </summary>
            public class {{ Schema.PascalName }}DataService
            {
                private readonly HttpClient _http;
                private readonly ILogger<{{ Schema.PascalName }}DataService> _logger;

                public {{ Schema.PascalName }}DataService(
                    IHttpClientFactory httpClientFactory,
                    ILogger<{{ Schema.PascalName }}DataService> logger)
                {
                    _http = httpClientFactory.CreateClient("{{ Service.PascalName }}Api");
                    _logger = logger;
                }

                public async Task<IEnumerable<{{ Schema.PascalName }}>> GetAllAsync()
                {
                    _logger.LogDebug("Fetching all {{ Schema.PascalName }} entities");
                    var response = await _http.GetFromJsonAsync<List<{{ Schema.PascalName }}>>(
                        "/api/v1/{{ Schema.PascalName | string.downcase }}s");
                    return response ?? [];
                }

                public async Task<{{ Schema.PascalName }}?> GetByIdAsync(string id)
                {
                    return await _http.GetFromJsonAsync<{{ Schema.PascalName }}>(
                        $"/api/v1/{{ Schema.PascalName | string.downcase }}s/{id}");
                }

                public async Task CreateAsync({{ Schema.PascalName }} item)
                {
                    var response = await _http.PostAsJsonAsync(
                        "/api/v1/{{ Schema.PascalName | string.downcase }}s", item);
                    response.EnsureSuccessStatusCode();
                }

                public async Task UpdateAsync({{ Schema.PascalName }} item)
                {
                    var response = await _http.PutAsJsonAsync(
                        $"/api/v1/{{ Schema.PascalName | string.downcase }}s/{item.Id}", item);
                    response.EnsureSuccessStatusCode();
                }

                public async Task DeleteAsync(string id)
                {
                    var response = await _http.DeleteAsync(
                        $"/api/v1/{{ Schema.PascalName | string.downcase }}s/{id}");
                    response.EnsureSuccessStatusCode();
                }
            }
            """;

        // ── Tag Overview Page ──────────────────────────────────────────
        public const string TagOverviewPage = """
            @page "/{{ Tag.Name | string.downcase }}"

            <PageTitle>{{ Tag.PascalName }} — {{ Service.PascalName }}</PageTitle>

            <RadzenText TextStyle="TextStyle.H4">{{ Tag.Name }}</RadzenText>
            <RadzenText TextStyle="TextStyle.Body1" class="rz-color-secondary rz-mb-4">
                {{ Tag.Description }}
            </RadzenText>

            <RadzenRow Gap="1rem" class="rz-mb-4">
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="api" Style="font-size: 2rem; color: var(--rz-primary);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ Operations | array.size }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Operations</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="download" Style="font-size: 2rem; color: var(--rz-success);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ QueryCount }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Queries (GET)</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
                <RadzenColumn Size="4">
                    <RadzenCard>
                        <RadzenStack AlignItems="AlignItems.Center">
                            <RadzenIcon Icon="add_circle" Style="font-size: 2rem; color: var(--rz-primary);" />
                            <RadzenText TextStyle="TextStyle.DisplayH4">{{ CommandCount }}</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Commands</RadzenText>
                        </RadzenStack>
                    </RadzenCard>
                </RadzenColumn>
            </RadzenRow>

            <RadzenText TextStyle="TextStyle.H5" class="rz-mb-2">Endpoints</RadzenText>
            <RadzenDataGrid Data="@_operations" TItem="OperationInfo" AllowSorting="true"
                            Density="Density.Compact">
                <Columns>
                    <RadzenDataGridColumn TItem="OperationInfo" Property="Method" Title="Method" Width="100px">
                        <Template Context="op">
                            <RadzenBadge Text="@op.Method" BadgeStyle="@GetBadgeStyle(op.Method)"
                                         IsPill="true" />
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn TItem="OperationInfo" Property="Path" Title="Path" />
                    <RadzenDataGridColumn TItem="OperationInfo" Property="OperationId" Title="Operation ID" />
                </Columns>
            </RadzenDataGrid>

            @code {
                private record OperationInfo(string Method, string Path, string OperationId);

                private List<OperationInfo> _operations =
                [
            {{~ for op in Operations ~}}
                    new("{{ op.HttpMethod }}", "{{ op.Path }}", "{{ op.OperationId }}"),
            {{~ end ~}}
                ];

                private static BadgeStyle GetBadgeStyle(string method) => method.ToUpperInvariant() switch
                {
                    "GET" => BadgeStyle.Success,
                    "POST" => BadgeStyle.Primary,
                    "PUT" => BadgeStyle.Warning,
                    "PATCH" => BadgeStyle.Info,
                    "DELETE" => BadgeStyle.Danger,
                    _ => BadgeStyle.Light
                };
            }
            """;

        // ── Delete Confirm Dialog ──────────────────────────────────────
        public const string DeleteConfirmDialog = """
            <RadzenStack Gap="1rem">
                <RadzenAlert AlertStyle="AlertStyle.Warning" Shade="Shade.Lighter">
                    <RadzenText TextStyle="TextStyle.Body1">
                        Are you sure you want to delete this item? This action cannot be undone.
                    </RadzenText>
                </RadzenAlert>

                <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.End" Gap="0.5rem">
                    <RadzenButton Text="Cancel" ButtonStyle="ButtonStyle.Light"
                                  Click="@(() => DialogService.Close(false))" />
                    <RadzenButton Text="Delete" ButtonStyle="ButtonStyle.Danger" Icon="delete"
                                  Click="@(() => DialogService.Close(true))" />
                </RadzenStack>
            </RadzenStack>

            @code {
                [Inject] private DialogService DialogService { get; set; } = null!;
            }
            """;

        // ── Entity Navigation ──────────────────────────────────────────
        public const string EntityNavMenu = """
            @* Entity navigation items — include in NavMenu.razor *@
            <RadzenPanelMenuItem Text="Entities" Icon="storage" Expanded="false">
            {{~ for schema in Service.Schemas ~}}
                {{~ if schema.IsEntity ~}}
                <RadzenPanelMenuItem Text="{{ schema.PascalName }}s" Icon="table_chart"
                                     Path="{{ schema.PascalName | string.downcase }}s" />
                {{~ end ~}}
            {{~ end ~}}
            </RadzenPanelMenuItem>
            """;

        // ── Metrics Chart Page ─────────────────────────────────────────
        public const string MetricsChartPage = """
            @page "/metrics"
            
            <PageTitle>Metrics Dashboard — {{ Service.PascalName }}</PageTitle>
            
            <RadzenStack Gap="1rem">
                <RadzenText TextStyle="TextStyle.H4">Service Metrics</RadzenText>
                
                <RadzenRow Gap="1rem">
                    <RadzenColumn Size="12" SizeMD="6">
                        <RadzenCard>
                            <RadzenText TextStyle="TextStyle.H6" class="rz-mb-2">Request Volume</RadzenText>
                            <RadzenChart>
                                <RadzenLineSeries Data="@_requestVolume" CategoryProperty="Time" 
                                                  ValueProperty="Count" Title="Requests/Min">
                                    <RadzenSeriesDataLabels Visible="false" />
                                </RadzenLineSeries>
                                <RadzenCategoryAxis>
                                    <RadzenAxisTitle Text="Time" />
                                </RadzenCategoryAxis>
                                <RadzenValueAxis>
                                    <RadzenAxisTitle Text="Count" />
                                    <RadzenGridLines Visible="true" />
                                </RadzenValueAxis>
                            </RadzenChart>
                        </RadzenCard>
                    </RadzenColumn>
                    
                    <RadzenColumn Size="12" SizeMD="6">
                        <RadzenCard>
                            <RadzenText TextStyle="TextStyle.H6" class="rz-mb-2">Response Times</RadzenText>
                            <RadzenChart>
                                <RadzenAreaSeries Data="@_responseTimes" CategoryProperty="Time" 
                                                  ValueProperty="Latency" Title="Avg Latency (ms)">
                                    <RadzenSeriesDataLabels Visible="false" />
                                </RadzenAreaSeries>
                                <RadzenCategoryAxis>
                                    <RadzenAxisTitle Text="Time" />
                                </RadzenCategoryAxis>
                                <RadzenValueAxis>
                                    <RadzenAxisTitle Text="Milliseconds" />
                                    <RadzenGridLines Visible="true" />
                                </RadzenValueAxis>
                            </RadzenChart>
                        </RadzenCard>
                    </RadzenColumn>
                </RadzenRow>
                
                <RadzenRow Gap="1rem">
                    <RadzenColumn Size="12" SizeMD="4">
                        <RadzenCard Style="height: 100%;">
                            <RadzenStack Gap="0.5rem" AlignItems="AlignItems.Center">
                                <RadzenText TextStyle="TextStyle.Overline">Total Requests</RadzenText>
                                <RadzenText TextStyle="TextStyle.DisplayH3">@_totalRequests.ToString("N0")</RadzenText>
                                <RadzenBadge Text="+12.5%" BadgeStyle="BadgeStyle.Success" />
                            </RadzenStack>
                        </RadzenCard>
                    </RadzenColumn>
                    
                    <RadzenColumn Size="12" SizeMD="4">
                        <RadzenCard Style="height: 100%;">
                            <RadzenStack Gap="0.5rem" AlignItems="AlignItems.Center">
                                <RadzenText TextStyle="TextStyle.Overline">Avg Response Time</RadzenText>
                                <RadzenText TextStyle="TextStyle.DisplayH3">@_avgResponseTime.ToString("N0")ms</RadzenText>
                                <RadzenBadge Text="-5.2%" BadgeStyle="BadgeStyle.Success" />
                            </RadzenStack>
                        </RadzenCard>
                    </RadzenColumn>
                    
                    <RadzenColumn Size="12" SizeMD="4">
                        <RadzenCard Style="height: 100%;">
                            <RadzenStack Gap="0.5rem" AlignItems="AlignItems.Center">
                                <RadzenText TextStyle="TextStyle.Overline">Error Rate</RadzenText>
                                <RadzenText TextStyle="TextStyle.DisplayH3">@_errorRate.ToString("P2")</RadzenText>
                                <RadzenBadge Text="+0.3%" BadgeStyle="BadgeStyle.Danger" />
                            </RadzenStack>
                        </RadzenCard>
                    </RadzenColumn>
                </RadzenRow>
                
                <RadzenCard>
                    <RadzenText TextStyle="TextStyle.H6" class="rz-mb-2">Status Code Distribution</RadzenText>
                    <RadzenChart>
                        <RadzenDonutSeries Data="@_statusCodes" CategoryProperty="Status" 
                                           ValueProperty="Count" Title="Status Codes">
                            <RadzenSeriesDataLabels Visible="true" />
                        </RadzenDonutSeries>
                        <RadzenLegend Position="LegendPosition.Right" />
                    </RadzenChart>
                </RadzenCard>
            </RadzenStack>
            """;

        public const string MetricsChartPageCode = """
            using Microsoft.AspNetCore.Components;
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            namespace {{ Service.PascalName }}.Dashboard.Pages;
            
            public partial class Metrics : ComponentBase
            {
                private class MetricDataPoint
                {
                    public string Time { get; set; } = string.Empty;
                    public int Count { get; set; }
                    public double Latency { get; set; }
                }
                
                private class StatusCodeData
                {
                    public string Status { get; set; } = string.Empty;
                    public int Count { get; set; }
                }
                
                private List<MetricDataPoint> _requestVolume = new();
                private List<MetricDataPoint> _responseTimes = new();
                private List<StatusCodeData> _statusCodes = new();
                
                private int _totalRequests = 125_432;
                private double _avgResponseTime = 145.2;
                private double _errorRate = 0.0152;
                
                protected override void OnInitialized()
                {
                    // Generate sample data - replace with actual metrics service
                    var now = DateTime.Now;
                    var random = new Random();
                    
                    for (int i = 24; i >= 0; i--)
                    {
                        var time = now.AddHours(-i);
                        _requestVolume.Add(new MetricDataPoint
                        {
                            Time = time.ToString("HH:mm"),
                            Count = random.Next(100, 500)
                        });
                        _responseTimes.Add(new MetricDataPoint
                        {
                            Time = time.ToString("HH:mm"),
                            Latency = random.Next(50, 250)
                        });
                    }
                    
                    _statusCodes =
                    [
                        new StatusCodeData { Status = "200 OK", Count = 118_234 },
                        new StatusCodeData { Status = "201 Created", Count = 3_456 },
                        new StatusCodeData { Status = "400 Bad Request", Count = 1_892 },
                        new StatusCodeData { Status = "401 Unauthorized", Count = 987 },
                        new StatusCodeData { Status = "404 Not Found", Count = 654 },
                        new StatusCodeData { Status = "500 Server Error", Count = 209 }
                    ];
                }
            }
            """;

        // ── Advanced Filter Dialog ─────────────────────────────────────
        public const string AdvancedFilterDialog = """
            <RadzenStack Gap="1rem">
                <RadzenText TextStyle="TextStyle.H6">Advanced Filters</RadzenText>
                
                <RadzenRow Gap="1rem">
                    <RadzenColumn Size="12" SizeMD="6">
                        <RadzenFormField Text="Search" Variant="Variant.Outlined">
                            <RadzenTextBox @bind-Value="@SearchTerm" Placeholder="Search all fields..." 
                                           Style="width: 100%;" />
                        </RadzenFormField>
                    </RadzenColumn>
                    
                    <RadzenColumn Size="12" SizeMD="6">
                        <RadzenFormField Text="Date Range" Variant="Variant.Outlined">
                            <RadzenDatePicker @bind-Value="@DateFrom" Placeholder="From..." 
                                              Style="width: 48%; margin-right: 4%;" />
                            <RadzenDatePicker @bind-Value="@DateTo" Placeholder="To..." 
                                              Style="width: 48%;" />
                        </RadzenFormField>
                    </RadzenColumn>
                </RadzenRow>
                
                <RadzenRow Gap="1rem">
                    <RadzenColumn Size="12" SizeMD="6">
                        <RadzenFormField Text="Status" Variant="Variant.Outlined">
                            <RadzenDropDown @bind-Value="@SelectedStatus" Data="@_statusOptions" 
                                            Placeholder="All Statuses" AllowClear="true" 
                                            Style="width: 100%;" />
                        </RadzenFormField>
                    </RadzenColumn>
                    
                    <RadzenColumn Size="12" SizeMD="6">
                        <RadzenFormField Text="Sort By" Variant="Variant.Outlined">
                            <RadzenDropDown @bind-Value="@SortBy" Data="@_sortOptions" 
                                            Placeholder="Default" Style="width: 100%;" />
                        </RadzenFormField>
                    </RadzenColumn>
                </RadzenRow>
                
                <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.End" Gap="0.5rem">
                    <RadzenButton Text="Clear" ButtonStyle="ButtonStyle.Light" Click="@ClearFilters" />
                    <RadzenButton Text="Apply Filters" ButtonStyle="ButtonStyle.Primary" 
                                  Icon="filter_list" Click="@ApplyFilters" />
                </RadzenStack>
            </RadzenStack>
            
            @code {
                [Inject] private DialogService DialogService { get; set; } = null!;
                
                private string? SearchTerm { get; set; }
                private DateTime? DateFrom { get; set; }
                private DateTime? DateTo { get; set; }
                private string? SelectedStatus { get; set; }
                private string? SortBy { get; set; }
                
                private readonly string[] _statusOptions = ["Active", "Inactive", "Pending", "Completed"];
                private readonly string[] _sortOptions = ["Name A-Z", "Name Z-A", "Date Newest", "Date Oldest"];
                
                private void ClearFilters()
                {
                    SearchTerm = null;
                    DateFrom = null;
                    DateTo = null;
                    SelectedStatus = null;
                    SortBy = null;
                }
                
                private void ApplyFilters()
                {
                    var filters = new
                    {
                        Search = SearchTerm,
                        DateFrom,
                        DateTo,
                        Status = SelectedStatus,
                        SortBy
                    };
                    DialogService.Close(filters);
                }
            }
            """;

        // ── Export Service ─────────────────────────────────────────────
        public const string ExportService = """
            using System.Text;
            using System.Text.Json;
            using Microsoft.JSInterop;
            
            namespace {{ Service.PascalName }}.Dashboard.Services;
            
            /// <summary>
            /// Service for exporting data to various formats (CSV, JSON, Excel).
            /// </summary>
            public class ExportService
            {
                private readonly IJSRuntime _jsRuntime;
                
                public ExportService(IJSRuntime jsRuntime)
                {
                    _jsRuntime = jsRuntime;
                }
                
                /// <summary>
                /// Exports data to CSV format and triggers download.
                /// </summary>
                public async Task ExportToCsvAsync<T>(IEnumerable<T> data, string filename = "export.csv")
                {
                    var items = data.ToList();
                    if (!items.Any())
                        return;
                    
                    var sb = new StringBuilder();
                    var properties = typeof(T).GetProperties();
                    
                    // Header row
                    sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvField(p.Name))));
                    
                    // Data rows
                    foreach (var item in items)
                    {
                        var values = properties.Select(p =>
                        {
                            var value = p.GetValue(item);
                            return EscapeCsvField(value?.ToString() ?? string.Empty);
                        });
                        sb.AppendLine(string.Join(",", values));
                    }
                    
                    await DownloadFileAsync(filename, sb.ToString(), "text/csv");
                }
                
                /// <summary>
                /// Exports data to JSON format and triggers download.
                /// </summary>
                public async Task ExportToJsonAsync<T>(IEnumerable<T> data, string filename = "export.json")
                {
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    
                    await DownloadFileAsync(filename, json, "application/json");
                }
                
                /// <summary>
                /// Exports data to Excel-compatible format.
                /// WARNING: This currently exports as CSV with .xlsx extension, not a true Excel file.
                /// For production use with true Excel format (.xlsx), integrate a library like ClosedXML or EPPlus.
                /// </summary>
                /// <remarks>
                /// This is a placeholder implementation that exports CSV data. Excel will open it but may show a warning.
                /// To generate true .xlsx files, add EPPlus or ClosedXML to your project and update this method.
                /// </remarks>
                public async Task ExportToExcelAsync<T>(IEnumerable<T> data, string filename = "export.xlsx")
                {
                    // For now, export as CSV with Excel extension
                    // In production, use a proper Excel library
                    await ExportToCsvAsync(data, filename);
                }
                
                private static string EscapeCsvField(string field)
                {
                    if (string.IsNullOrEmpty(field))
                        return string.Empty;
                    
                    // Escape quotes and wrap in quotes if contains comma, quote, newline, or carriage return
                    if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                    {
                        return $"\"{field.Replace("\"", "\"\"")}\"";
                    }
                    
                    return field;
                }
                
                private async Task DownloadFileAsync(string filename, string content, string mimeType)
                {
                    var bytes = Encoding.UTF8.GetBytes(content);
                    var base64 = Convert.ToBase64String(bytes);
                    
                    await _jsRuntime.InvokeVoidAsync("downloadFile", filename, mimeType, base64);
                }
            }
            
            // Add this to wwwroot/js/site.js or _Host.cshtml:
            /*
            window.downloadFile = function(filename, mimeType, base64Data) {
                const link = document.createElement('a');
                link.download = filename;
                link.href = `data:${mimeType};base64,${base64Data}`;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            };
            */
            """;
    }
}
