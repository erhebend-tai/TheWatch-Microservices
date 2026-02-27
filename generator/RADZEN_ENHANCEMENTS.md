# Radzen Blazor Generator Enhancements

## Overview

The MicroGen generator has been enhanced with improved Radzen Blazor component scaffolding. These enhancements provide richer UI components, better validation, data export capabilities, and analytics dashboards out-of-the-box.

## New Features

### 1. Metrics Dashboard (Metrics.razor)

A comprehensive metrics dashboard is now generated for each service, featuring:

- **Line Charts** - Request volume over time
- **Area Charts** - Response time trends
- **Donut Charts** - Status code distribution
- **KPI Cards** - Total requests, average response time, error rate
- **Real-time Updates** - Sample data with refresh capability

**Usage:**
```csharp
// Navigate to /metrics in your dashboard
// Metrics are auto-generated with sample data
// Replace sample data with actual metrics from your monitoring service
```

### 2. Advanced Filter Dialog (AdvancedFilterDialog.razor)

A reusable dialog component for complex filtering scenarios:

- Full-text search across all fields
- Date range filtering
- Status dropdown
- Sort order selection
- Clear and Apply actions

**Usage:**
```razor
<RadzenButton Text="Filters" Icon="filter_list" 
              Click="@OpenAdvancedFilters" />

@code {
    private async Task OpenAdvancedFilters()
    {
        var result = await DialogService.OpenAsync<AdvancedFilterDialog>(
            "Advanced Filters",
            null,
            new DialogOptions { Width = "700px" });

        if (result != null)
        {
            // Apply filters
        }
    }
}
```

### 3. Export Service (ExportService.cs)

Data export functionality for CSV, JSON, and Excel formats:

**Features:**
- CSV export with proper field escaping
- JSON export with pretty formatting
- Excel export (placeholder for future library integration)
- Browser download via JavaScript interop

**Usage:**
```csharp
[Inject] private ExportService ExportService { get; set; }

private async Task ExportData()
{
    await ExportService.ExportToCsvAsync(_items, "data.csv");
    // or
    await ExportService.ExportToJsonAsync(_items, "data.json");
    // or
    await ExportService.ExportToExcelAsync(_items, "data.xlsx");
}
```

**Required JavaScript:**
Add this to your `wwwroot/js/site.js` or `_Host.cshtml`:

```javascript
window.downloadFile = function(filename, mimeType, base64Data) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = `data:${mimeType};base64,${base64Data}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
```

### 4. Enhanced List Pages

List pages now include:

- **Export Button** - RadzenSplitButton with CSV/JSON/Excel options
- **Filter Button** - Opens advanced filter dialog
- **Better Layout** - Improved button organization and spacing

### 5. Enhanced Form Validation

Form dialogs now include comprehensive validation:

- **Field-specific Validators:**
  - `RadzenRequiredValidator` - Required fields
  - `RadzenEmailValidator` - Email format validation
  - `RadzenLengthValidator` - Password length requirements
  - Name attribute on all form fields for validator binding

- **Form-level Features:**
  - `ValidationSummary` - Shows all validation errors
  - Submit state management - Prevents double submissions
  - Error handling with try-catch
  - NotificationService integration for success/error messages

**Example Generated Form:**
```razor
<RadzenFormField Text="Email" Variant="Variant.Outlined">
    <RadzenTextBox @bind-Value="@Model.Email" Name="Email" />
    <RadzenRequiredValidator Component="Email" Text="Email is required" />
    <RadzenEmailValidator Component="Email" Text="Please enter a valid email" />
</RadzenFormField>

<RadzenFormField Text="Password" Variant="Variant.Outlined">
    <RadzenPassword @bind-Value="@Model.Password" Name="Password" />
    <RadzenRequiredValidator Component="Password" Text="Password is required" />
    <RadzenLengthValidator Component="Password" Min="8" 
                           Text="Password must be at least 8 characters" />
</RadzenFormField>
```

## Configuration

All Radzen features are controlled by the `RadzenUI` feature flag in `microgen.json`:

```json
{
  "features": {
    "radzenUI": true,
    "blazorDashboard": true
  }
}
```

## Generated Files

For each service, the following files are generated:

```
{Service}.Dashboard/
├── Pages/
│   ├── {Entity}List.razor          # Enhanced with export & filters
│   ├── {Entity}List.razor.cs       # Enhanced code-behind
│   ├── {Entity}Form.razor          # Enhanced with validation
│   ├── {Entity}Form.razor.cs       # Enhanced with error handling
│   ├── {Entity}Detail.razor        # Detail view
│   ├── {Entity}Detail.razor.cs     # Detail code-behind
│   ├── Metrics.razor               # NEW: Metrics dashboard
│   └── Metrics.razor.cs            # NEW: Metrics code-behind
├── Components/
│   ├── DeleteConfirmDialog.razor
│   └── AdvancedFilterDialog.razor  # NEW: Advanced filters
├── Services/
│   ├── {Entity}DataService.cs
│   └── ExportService.cs            # NEW: Export utilities
└── Layout/
    └── EntityNavMenu.razor
```

## Radzen Components Used

### Charts
- `RadzenChart` - Base chart component
- `RadzenLineSeries` - Line charts
- `RadzenAreaSeries` - Area charts
- `RadzenDonutSeries` - Donut charts
- `RadzenCategoryAxis` / `RadzenValueAxis` - Chart axes
- `RadzenLegend` - Chart legend

### Data Display
- `RadzenDataGrid<T>` - Data grids with sorting, filtering, paging
- `RadzenCard` - Card containers
- `RadzenBadge` - Status badges
- `RadzenText` - Typography

### Forms & Input
- `RadzenFormField` - Form field wrapper
- `RadzenTextBox` - Text input
- `RadzenPassword` - Password input
- `RadzenNumeric<T>` - Numeric input
- `RadzenDatePicker` - Date/time picker
- `RadzenSwitch` - Boolean toggle
- `RadzenDropDown<T>` - Dropdown select
- `RadzenTextArea` - Multi-line text

### Validation
- `RadzenRequiredValidator` - Required field validation
- `RadzenEmailValidator` - Email format validation
- `RadzenLengthValidator` - Length validation
- `ValidationSummary` - Form-level error summary

### Buttons & Actions
- `RadzenButton` - Standard button
- `RadzenSplitButton` - Button with dropdown menu
- `RadzenSplitButtonItem` - Menu item

### Layout
- `RadzenStack` - Flexbox container
- `RadzenRow` / `RadzenColumn` - Grid layout

### Dialogs
- `DialogService` - Service for opening dialogs
- `NotificationService` - Toast notifications
- `TooltipService` - Tooltips

## Migration from Previous Version

If you have existing generated code, the new features are additive and non-breaking:

1. **New Components** - Metrics, AdvancedFilterDialog, ExportService are new additions
2. **Enhanced Components** - List pages and forms have additional features but maintain the same structure
3. **Service Registration** - ExportService is automatically registered in Program.cs

To regenerate with new features:
```bash
cd generator
dotnet run --project src/MicroGen.Cli -- generate --input ../auth --output ../generated
```

## Examples

### Complete List Page with Export & Filters

```razor
@page "/users"

<PageTitle>Users — Auth Service</PageTitle>

<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.SpaceBetween">
    <RadzenText TextStyle="TextStyle.H4">Users</RadzenText>
    <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem">
        <RadzenSplitButton Text="Export" Icon="download" ButtonStyle="ButtonStyle.Light"
                           Click="@(() => ExportData("csv"))">
            <ChildContent>
                <RadzenSplitButtonItem Text="Export to CSV" Value="csv" Icon="table_chart" />
                <RadzenSplitButtonItem Text="Export to JSON" Value="json" Icon="code" />
                <RadzenSplitButtonItem Text="Export to Excel" Value="excel" Icon="description" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenButton Text="Filters" Icon="filter_list" Click="@OpenAdvancedFilters" />
        <RadzenButton Text="Create" Icon="add" ButtonStyle="ButtonStyle.Primary" Click="@OpenCreateDialog" />
    </RadzenStack>
</RadzenStack>

<RadzenDataGrid Data="@_items" TItem="User" AllowSorting="true" AllowFiltering="true" AllowPaging="true">
    <!-- Columns auto-generated based on schema -->
</RadzenDataGrid>
```

## Future Enhancements

Planned improvements for future releases:

- RadzenScheduler for calendar/schedule views
- RadzenGantt for timeline/project tracking
- RadzenTree for hierarchical data
- RadzenAutoComplete for typeahead search
- RadzenHtmlEditor for rich text editing
- Multi-step wizard forms with RadzenSteps
- Draggable dashboard layouts
- Theme customization scaffolding
- Dark mode support
- MAUI Blazor integration

## Support

For issues or questions:
1. Check the MicroGen documentation in the generator directory
2. Review the Generator.Prompt.md in the root directory for architecture details
3. Open an issue in the repository

## Contributing

To add new Radzen component templates:

1. Edit `generator/src/MicroGen.Generator/Generators/RadzenPageGenerator.cs`
2. Add a new generator method (e.g., `GenerateMyComponentAsync`)
3. Add corresponding Scriban templates to the `Templates` class
4. Register the generator in the main `GenerateAsync` method
5. Build and test: `dotnet build MicroGen.slnx`

## License

Same license as the parent project.
