using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ADMIN.Web.Pages;

public partial class AdminListPage
{
    private RadzenDataGrid<object>? grid;
    private IEnumerable<object> items = new List<object>();
    private bool isLoading = true;
    private string searchText = string.Empty;

    [Inject]
    public DialogService DialogService { get; set; } = default!;

    [Inject]
    public NotificationService NotificationService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        try
        {
            // TODO: Call API service
            // items = await listActiveProtocolsService.GetAllAsync();
            await Task.Delay(500); // Simulate API call
            items = new List<object>(); // Replace with actual data
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to load data: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnSearch()
    {
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            // TODO: Implement search
            await LoadDataAsync();
        }
    }

    private async Task OnClearSearch()
    {
        searchText = string.Empty;
        await LoadDataAsync();
    }

    private void OnView(object item)
    {
        // TODO: Navigate to detail page
        Navigation.NavigateTo($"//admin/protocols/{item.GetType().GetProperty("Id")?.GetValue(item)}");
    }

    private void OnEdit(object item)
    {
        // TODO: Navigate to edit page
        Navigation.NavigateTo($"//admin/protocols/{item.GetType().GetProperty("Id")?.GetValue(item)}/edit");
    }

    private async Task OnDelete(object item)
    {
        var confirmed = await DialogService.Confirm(
            "Are you sure you want to delete this item?",
            "Confirm Delete",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" }
        );

        if (confirmed == true)
        {
            try
            {
                // TODO: Call delete API
                NotificationService.Notify(NotificationSeverity.Success, "Success", "Item deleted successfully");
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to delete: {ex.Message}");
            }
        }
    }
}
