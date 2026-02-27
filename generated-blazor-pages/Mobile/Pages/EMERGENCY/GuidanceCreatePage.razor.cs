using Microsoft.AspNetCore.Components;
using Radzen;

namespace EMERGENCY.Web.Pages;

public partial class GuidanceCreatePage
{
    
    

    [Inject]
    public NotificationService NotificationService { get; set; } = default!;

    private FormModel model = new();
    private bool isLoading = false;
    private bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        // New item - no data to load
    }

    
    
        
        
        
            
            
            
        
        
        
            
        
        
        
            
        
    

    private async Task OnSubmit()
    {
        isSaving = true;
        try
        {
            // TODO: Call API service
            // await service.CreateAsync(model);
            await Task.Delay(500); // Simulate API call
            
            NotificationService.Notify(
                NotificationSeverity.Success, 
                "Success", 
                $"Item created successfully"
            );
            
            Navigation.NavigateTo("//guidance/select");
        }
        catch (Exception ex)
        {
            NotificationService.Notify(
                NotificationSeverity.Error, 
                "Error", 
                $"Failed to create: {ex.Message}"
            );
        }
        finally
        {
            isSaving = false;
        }
    }

    private void OnCancel()
    {
        Navigation.NavigateTo("//guidance/select");
    }

    // TODO: Replace with actual model class
    private class FormModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
