using Microsoft.AspNetCore.Components;
using Radzen;

namespace LOCATION.Web.Pages;

public partial class MapsDetailPage
{
    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Inject]
    public DialogService DialogService { get; set; } = default!;

    [Inject]
    public NotificationService NotificationService { get; set; } = default!;

    private object? item;
    private bool isLoading = true;

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
            // item = await getMapStyleService.GetByIdAsync(Id);
            await Task.Delay(500); // Simulate API call
            item = new object(); // Replace with actual data
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to load details: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void OnEdit()
    {
        Navigation.NavigateTo($"//maps/styles/{styleid}/{Id}/edit");
    }

    private async Task OnDelete()
    {
        var confirmed = await DialogService.Confirm(
            "Are you sure you want to delete this item? This action cannot be undone.",
            "Confirm Delete",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" }
        );

        if (confirmed == true)
        {
            try
            {
                // TODO: Call delete API
                NotificationService.Notify(NotificationSeverity.Success, "Success", "Item deleted successfully");
                Navigation.NavigateTo("//maps/styles/{styleid}");
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to delete: {ex.Message}");
            }
        }
    }
}
