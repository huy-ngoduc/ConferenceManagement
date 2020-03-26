using System.Threading.Tasks;
using Conference.Web.Admin.Data;
using Conference.Web.Admin.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.ProtectedBrowserStorage;

namespace Conference.Web.Admin.Pages
{
    public class LocateConferenceBase : ComponentBase
    {
        [Parameter] public LocateInputModel LocateModel { get; set; }
        [Parameter] public ProtectedLocalStorage LocalStorage { get; set; }
        [Inject] public IConferenceAdminService ConferenceAdminService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        protected async Task LocateConference()
        {
            var (slug, accessCode) = await ConferenceAdminService.Locate(LocateModel.Email, LocateModel.AccessCode);
            await LocalStorage.SetAsync("AccessCode", accessCode);
            NavigationManager.NavigateTo($"conference/{slug}");
        }
    }
}