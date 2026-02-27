# Blazor Pages Generator - Implementation Guide

## Overview

This guide provides detailed information about the newly created Blazor Pages Generator and the 500 generated pages using Radzen Blazor components.

## Generated Assets

### 1. Generator Script

**File**: `scripts/generate-blazor-pages.py`

**Features**:
- 1,300+ lines of production-ready Python code
- Parses TODO.md for page specifications
- Generates Blazor pages with Radzen components
- Supports web and mobile (MAUI) variants
- Intelligent duplicate handling
- Component mapping from generic to Radzen

**Usage**:
```bash
# Generate 500 pages (default behavior used)
python scripts/generate-blazor-pages.py --limit 500 --output-dir generated-blazor-pages

# Generate all pages
python scripts/generate-blazor-pages.py --output-dir generated-blazor-pages

# Generate for specific domain
python scripts/generate-blazor-pages.py --domain emergency --output-dir generated-blazor-pages

# Generate web-only pages
python scripts/generate-blazor-pages.py --web-only --output-dir generated-blazor-pages

# Generate mobile-only pages
python scripts/generate-blazor-pages.py --mobile-only --output-dir generated-blazor-pages
```

### 2. Generated Pages

**Output Directory**: `generated-blazor-pages/`

**Statistics**:
- **500 web pages** (Blazor Server/WebAssembly)
- **500 mobile pages** (MAUI Blazor)
- **2,002 total files**
  - 1,000 .razor files
  - 1,000 .razor.cs code-behind files
  - 2 MainLayout.razor files

**Domain Breakdown**:
- ADMIN: 51 pages
- AUTH: 51 pages
- EMERGENCY: 58 pages
- INFRASTRUCTURE: 53 pages
- DISASTER: 48 pages
- LOCATION: 45 pages
- DATABASE: 35 pages
- PLATFORM: 34 pages
- EVIDENCE: 27 pages
- MESSAGING: 15 pages
- NOTIFICATIONS: 15 pages
- LOGISTICS: 15 pages
- MEDICAL: 13 pages
- COMMUNITY: 9 pages
- _TESTING: 8 pages
- DISPATCH: 7 pages
- CACHING: 6 pages
- LEGAL: 6 pages
- LINT: 4 pages

**Page Type Breakdown**:
- **Create Pages**: 208 (41.6%)
- **Detail Pages**: 120 (24.0%)
- **List Pages**: 118 (23.6%)
- **Delete Pages**: 28 (5.6%)
- **Edit Pages**: 26 (5.2%)

## Page Templates

### 1. List Pages (118 pages)

**Features**:
- RadzenDataGrid with sorting, filtering, paging
- Search functionality with RadzenTextBox
- Create/Edit/Delete action buttons
- Loading states with RadzenProgressBarCircular
- Empty state handling

**Example**: `EMERGENCY/IncidentsListPage.razor`

**Components Used**:
- `RadzenDataGrid<T>` - Main data table
- `RadzenTextBox` - Search input
- `RadzenButton` - Actions
- `RadzenCard` - Content container
- `RadzenProgressBarCircular` - Loading indicator

**Code Structure**:
```csharp
public partial class IncidentsListPage
{
    private RadzenDataGrid<object>? grid;
    private IEnumerable<object> items;
    private bool isLoading = true;
    private string searchText = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync() { /* ... */ }
    private async Task OnSearch() { /* ... */ }
    private void OnView(object item) { /* ... */ }
    private void OnEdit(object item) { /* ... */ }
    private async Task OnDelete(object item) { /* ... */ }
}
```

### 2. Detail Pages (120 pages)

**Features**:
- RadzenCard for content display
- Breadcrumb navigation
- Action buttons (Edit, Delete, Back)
- Loading and error states
- Responsive layout

**Example**: `AUTH/UsersDetailPage.razor`

**Components Used**:
- `RadzenCard` - Content containers
- `RadzenBreadCrumb` - Navigation
- `RadzenButton` - Actions
- `RadzenText` - Typography
- `RadzenStack` - Layout

### 3. Create/Edit Pages (234 pages)

**Features**:
- RadzenTemplateForm with validation
- RadzenFieldset for field grouping
- RadzenRequiredValidator for validation
- Cancel and Submit buttons
- Loading states during save

**Example**: `EMERGENCY/IncidentsCreatePage.razor`

**Components Used**:
- `RadzenTemplateForm<T>` - Form container
- `RadzenFieldset` - Field groups
- `RadzenTextBox` - Text inputs
- `RadzenTextArea` - Multi-line text
- `RadzenButton` - Form actions
- `RadzenRequiredValidator` - Validation

**Code Structure**:
```csharp
public partial class IncidentsCreatePage
{
    private FormModel model = new();
    private bool isSaving = false;

    private async Task OnSubmit()
    {
        isSaving = true;
        try
        {
            // TODO: Call API service
            NotificationService.Notify(...);
            Navigation.NavigateTo("/incidents");
        }
        finally
        {
            isSaving = false;
        }
    }
}
```

### 4. Dashboard Pages (Coming Soon)

Template ready for dashboard pages with:
- Metric cards with RadzenCard
- Charts with RadzenChart (Line, Donut, Bar)
- Recent activity with RadzenDataList
- Real-time updates

## Radzen Component Mapping

The generator intelligently maps generic component names to specific Radzen components:

| Generic Component | Radzen Component | Usage |
|-------------------|------------------|-------|
| DataTable | RadzenDataGrid | Lists, tables |
| SearchFilter | RadzenTextBox | Search inputs |
| Pagination | RadzenPager | Table pagination |
| LoadingSpinner | RadzenProgressBarCircular | Loading states |
| Form | RadzenTemplateForm | Forms |
| FormValidation | RadzenRequiredValidator | Validation |
| SubmitButton | RadzenButton | Form submit |
| DetailView | RadzenCard | Content display |
| DeleteButton | RadzenButton | Delete actions |
| ConfirmDialog | RadzenDialog | Confirmations |
| Chart | RadzenChart | Data visualization |
| Dashboard | RadzenStack | Layout |
| NotificationBadge | RadzenBadge | Notifications |
| AlertList | RadzenDataList | Alert lists |

## Integration Guide

### Step 1: Install Radzen.Blazor

```bash
cd YourBlazorProject
dotnet add package Radzen.Blazor
```

### Step 2: Configure Services

In `Program.cs`:

```csharp
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add Radzen services
builder.Services.AddRadzenComponents();

// Add other services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

var app = builder.Build();
app.Run();
```

### Step 3: Add Radzen CSS and JS

In `App.razor` or `_Host.cshtml`:

```html
<head>
    <!-- Radzen CSS -->
    <link rel="stylesheet" href="_content/Radzen.Blazor/css/material-base.css">
    
    <!-- Optional: Choose different theme -->
    <!-- <link rel="stylesheet" href="_content/Radzen.Blazor/css/standard-base.css"> -->
    <!-- <link rel="stylesheet" href="_content/Radzen.Blazor/css/fluent-base.css"> -->
</head>

<body>
    <!-- Your app content -->
    
    <!-- Radzen JS -->
    <script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
</body>
```

### Step 4: Add Using Directives

In `_Imports.razor`:

```razor
@using Radzen
@using Radzen.Blazor
```

### Step 5: Copy Generated Pages

```bash
# Copy web pages
cp -r generated-blazor-pages/Web/Pages/* YourBlazorProject/Pages/

# Copy mobile pages
cp -r generated-blazor-pages/Mobile/Pages/* YourMauiBlazorProject/Pages/

# Copy layout
cp generated-blazor-pages/Web/Shared/MainLayout.razor YourBlazorProject/Shared/
```

### Step 6: Implement API Services

Each page has `// TODO:` comments indicating where to add API calls. Example:

```csharp
// TODO: Call API service
// items = await incidentService.GetAllAsync();
await Task.Delay(500); // Simulate API call
```

Replace these with actual service calls:

```csharp
// Create API service interface
public interface IIncidentService
{
    Task<IEnumerable<Incident>> GetAllAsync();
    Task<Incident> GetByIdAsync(string id);
    Task<Incident> CreateAsync(CreateIncidentRequest request);
    Task UpdateAsync(string id, UpdateIncidentRequest request);
    Task DeleteAsync(string id);
}

// Inject in page
[Inject]
public IIncidentService IncidentService { get; set; } = default!;

// Use in code
private async Task LoadDataAsync()
{
    isLoading = true;
    try
    {
        items = await IncidentService.GetAllAsync();
    }
    catch (Exception ex)
    {
        NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
    }
    finally
    {
        isLoading = false;
    }
}
```

## Customization Guide

### Changing Themes

Radzen supports multiple themes. In your layout:

```html
<!-- Material Design (default) -->
<link rel="stylesheet" href="_content/Radzen.Blazor/css/material-base.css">

<!-- Standard -->
<link rel="stylesheet" href="_content/Radzen.Blazor/css/standard-base.css">

<!-- Fluent -->
<link rel="stylesheet" href="_content/Radzen.Blazor/css/fluent-base.css">

<!-- Dark theme variants -->
<link rel="stylesheet" href="_content/Radzen.Blazor/css/material-dark-base.css">
```

### Customizing DataGrid

```razor
<RadzenDataGrid @ref="grid" 
                Data="@items" 
                TItem="Incident"
                AllowFiltering="true" 
                FilterMode="FilterMode.Advanced"
                AllowSorting="true" 
                AllowPaging="true" 
                PageSize="20"
                ShowPagingSummary="true"
                PagerHorizontalAlign="HorizontalAlign.Center">
    <Columns>
        <RadzenDataGridColumn TItem="Incident" Property="Id" Title="ID" Width="80px" />
        <RadzenDataGridColumn TItem="Incident" Property="Title" Title="Title" />
        <RadzenDataGridColumn TItem="Incident" Property="Priority" Title="Priority">
            <Template Context="incident">
                <RadzenBadge 
                    BadgeStyle="@GetPriorityBadgeStyle(incident.Priority)" 
                    Text="@incident.Priority.ToString()" />
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>
```

### Adding Authentication

Wrap routes with `AuthorizeView`:

```razor
<AuthorizeView>
    <Authorized>
        <!-- Page content -->
    </Authorized>
    <NotAuthorized>
        <RadzenCard>
            <RadzenText>You are not authorized to view this page.</RadzenText>
            <RadzenButton Text="Login" Click="@(() => Navigation.NavigateTo("/login"))" />
        </RadzenCard>
    </NotAuthorized>
</AuthorizeView>
```

### Customizing Forms

```razor
<RadzenTemplateForm Data="@model" Submit="@OnSubmit">
    <RadzenStack Gap="1rem">
        <!-- Text field with icon -->
        <RadzenFormField Text="Email:" Variant="Variant.Outlined">
            <Start>
                <RadzenIcon Icon="email" />
            </Start>
            <ChildContent>
                <RadzenTextBox @bind-Value="@model.Email" Name="Email" />
            </ChildContent>
            <Helper>
                <RadzenText TextStyle="TextStyle.Caption">We'll never share your email.</RadzenText>
            </Helper>
        </RadzenFormField>
        <RadzenEmailValidator Component="Email" Text="Invalid email" />
        
        <!-- Date picker -->
        <RadzenFormField Text="Date:" Variant="Variant.Outlined">
            <RadzenDatePicker @bind-Value="@model.Date" Name="Date" ShowTime="true" />
        </RadzenFormField>
        
        <!-- Dropdown -->
        <RadzenFormField Text="Priority:" Variant="Variant.Outlined">
            <RadzenDropDown @bind-Value="@model.Priority" 
                           Data="@priorities" 
                           TextProperty="Name" 
                           ValueProperty="Value" />
        </RadzenFormField>
    </RadzenStack>
</RadzenTemplateForm>
```

## Mobile (MAUI Blazor) Specifics

The mobile pages use the same components but are optimized for mobile layouts:

### MauiProgram.cs

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        });

    builder.Services.AddMauiBlazorWebView();
    
    // Add Radzen services
    builder.Services.AddRadzenComponents();
    builder.Services.AddScoped<DialogService>();
    builder.Services.AddScoped<NotificationService>();

    return builder.Build();
}
```

### wwwroot/index.html

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover" />
    <title>TheWatch Mobile</title>
    <link rel="stylesheet" href="_content/Radzen.Blazor/css/material-base.css">
    <link href="css/app.css" rel="stylesheet" />
</head>
<body>
    <div class="status-bar-safe-area"></div>
    <div id="app">Loading...</div>
    <script src="_framework/blazor.webview.js" autostart="false"></script>
    <script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
</body>
</html>
```

## Testing Generated Pages

### 1. Create Test Project

```bash
dotnet new nunit -o YourProject.Tests
cd YourProject.Tests
dotnet add package bUnit
dotnet add reference ../YourProject/YourProject.csproj
```

### 2. Test List Page

```csharp
using Bunit;
using Xunit;
using Radzen.Blazor;

public class IncidentsListPageTests : TestContext
{
    [Fact]
    public void RendersCorrectly()
    {
        // Arrange
        Services.AddScoped<DialogService>();
        Services.AddScoped<NotificationService>();
        
        // Act
        var cut = RenderComponent<IncidentsListPage>();
        
        // Assert
        cut.WaitForElement("h1"); // Wait for title
        Assert.Contains("Incidents", cut.Markup);
        cut.FindComponent<RadzenDataGrid<object>>();
    }
    
    [Fact]
    public async Task LoadsData()
    {
        // Arrange
        var mockService = new Mock<IIncidentService>();
        mockService.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new[] { new Incident { Id = "1", Title = "Test" } });
        
        Services.AddScoped<IIncidentService>(_ => mockService.Object);
        
        // Act
        var cut = RenderComponent<IncidentsListPage>();
        
        // Assert
        await cut.InvokeAsync(() => Task.CompletedTask); // Wait for render
        Assert.Contains("Test", cut.Markup);
    }
}
```

## Performance Optimization

### 1. Virtualization for Large Lists

```razor
<RadzenDataGrid Data="@items" 
                TItem="Incident"
                AllowPaging="true"
                AllowVirtualization="true"
                PageSize="50">
    <!-- Columns -->
</RadzenDataGrid>
```

### 2. Lazy Loading

```csharp
private async Task LoadDataAsync()
{
    if (!isInitialized)
    {
        isLoading = true;
        items = await service.GetPageAsync(page, pageSize);
        isLoading = false;
        isInitialized = true;
    }
}
```

### 3. Debounced Search

```csharp
private System.Timers.Timer? searchTimer;

private void OnSearchTextChanged(string value)
{
    searchText = value;
    searchTimer?.Stop();
    searchTimer = new System.Timers.Timer(300); // 300ms debounce
    searchTimer.Elapsed += async (s, e) => 
    {
        await InvokeAsync(async () =>
        {
            await LoadDataAsync();
            StateHasChanged();
        });
    };
    searchTimer.AutoReset = false;
    searchTimer.Start();
}
```

## Troubleshooting

### Common Issues

**Issue**: Pages don't render
- **Solution**: Ensure Radzen.Blazor is installed and CSS/JS are included

**Issue**: Components not found
- **Solution**: Add `@using Radzen.Blazor` to _Imports.razor

**Issue**: Validation not working
- **Solution**: Ensure form has `<RadzenTemplateForm>` and validators are inside

**Issue**: DataGrid empty
- **Solution**: Check that Data property is bound and LoadDataAsync is called

## Next Steps

1. ✅ **Review Generated Pages** - Browse the 500 generated pages in CATALOG.md
2. ✅ **Install Dependencies** - Add Radzen.Blazor NuGet package
3. ✅ **Copy Pages** - Copy pages to your project
4. ✅ **Implement Services** - Replace TODO comments with API calls
5. ✅ **Add Authentication** - Protect routes with AuthorizeView
6. ✅ **Customize Styling** - Adjust theme and colors
7. ✅ **Add Tests** - Write unit tests with bUnit
8. ✅ **Deploy** - Build and deploy your application

## Resources

- **Radzen Blazor Documentation**: https://blazor.radzen.com/docs/guides/getting-started.html
- **Radzen Component Demos**: https://blazor.radzen.com/
- **bUnit Testing**: https://bunit.dev/
- **MAUI Blazor**: https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/

## Support

For issues with the generated pages or generator script:
1. Check CATALOG.md for page inventory
2. Review this implementation guide
3. Examine the generated code for TODO comments
4. Refer to Radzen Blazor documentation

---

**Generated**: 2026-02-17
**Generator Version**: 1.0.0
**Total Pages**: 500 (1000 including mobile variants)
**Framework**: .NET 10, Blazor, Radzen.Blazor 5.x
