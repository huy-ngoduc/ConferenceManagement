using System.Threading.Tasks;
using Conference.Web.Admin.Data;
using Conference.Web.Admin.Services;
using Conference.Web.Admin.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.ProtectedBrowserStorage;

namespace Conference.Web.Admin.Pages
{
    public class LocateConferenceBase : ComponentBase
    {
        [Parameter] public LocateInputModel LocateModel { get; set; } = new LocateInputModel();
        [Inject] public ProtectedSessionStorage SessionStorage { get; set; }
        [Inject] public IConferenceAdminService ConferenceAdminService { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        protected async Task LocateConference()
        {
            var conferenceIdentity = await ConferenceAdminService.Locate(LocateModel.Email, LocateModel.AccessCode);
            await SessionStorage.SetAsync(LocalStorageKey.AccessCode, conferenceIdentity.AccessCode);
            
            NavigationManager.NavigateTo($"conference/{conferenceIdentity.Slug}");
        }
    }
}