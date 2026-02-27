using Microsoft.AspNetCore.Components;
using Radzen;

namespace COMMUNITY.Web.Pages;

public partial class CommunityEditPage
{
    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Inject]
    public NotificationService NotificationService { get; set; } = default!;

    private FormModel model = new();
    private bool isLoading = true;
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        try
        {
            // TODO: Load existing data
            // model = await service.GetByIdAsync(Id);
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to load: {{ex.Message}}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnSubmit()
    {
        isSaving = true;
        try
        {
            // TODO: Call API service
            // await service.UpdateAsync(Id, model);
            await Task.Delay(500); // Simulate API call
            
            NotificationService.Notify(
                NotificationSeverity.Success, 
                "Success", 
                $"Item updated successfully"
            );
            
            Navigation.NavigateTo("//community/food/sites/{siteid}/status");
        }
        catch (Exception ex)
        {
            NotificationService.Notify(
                NotificationSeverity.Error, 
                "Error", 
                $"Failed to update: {ex.Message}"
            );
        }
        finally
        {
            isSaving = false;
        }
    }

    private void OnCancel()
    {
        Navigation.NavigateTo("//community/food/sites/{siteid}/status");
    }

    // TODO: Replace with actual model class
    private class FormModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
