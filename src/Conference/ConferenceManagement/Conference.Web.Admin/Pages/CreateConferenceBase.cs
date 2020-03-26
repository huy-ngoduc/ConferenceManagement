using System;
using System.Threading.Tasks;
using Conference.Admin.Share;
using Conference.Web.Admin.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.ProtectedBrowserStorage;

namespace Conference.Web.Admin.Pages
{
    public class CreateConferenceBase : ComponentBase
    {
        [Inject] public IConferenceAdminService ConferenceAdminService { get; set; }
        [Inject] public ProtectedLocalStorage LocalStorage { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }

        public ConferenceCreateInputModel Conference { get; set; } = new ConferenceCreateInputModel()
        {
            Slug = "blazor",
            TwitterSearch = "blazor",
            Name = "blazor",
            Description = "The blazor course, everything regarding to blazor",
            OwnerName = "huy",
            OwnerEmail = "huy@gmail.com",
            Location = "Hanoi",
            IsPublished = false,
            Tagline = "blazor, workshop",
            StartDate = DateTime.Now.AddDays(7),
            EndDate = DateTime.Now.AddDays(14)
        };

        protected async Task CreateValidConference()
        {
            var accessCode = await ConferenceAdminService.CreateConference(Conference);
            await LocalStorage.SetAsync("accessCode", accessCode);
            await LocalStorage.SetAsync("notification", $"Conference [{Conference.Slug}] was created successfully");
            NavigationManager.NavigateTo($"/conference/{Conference.Slug}");
        }
    }
}