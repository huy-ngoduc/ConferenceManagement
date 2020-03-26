using System.Collections.Generic;
using System.Threading.Tasks;
using Conference.Web.Public.Services;
using Microsoft.AspNetCore.Components;

namespace Conference.Web.Public.Pages
{
    public class AllEventBase : ComponentBase
    {
        [Inject] public ConferenceService ConferenceService { get; set; }
        public IList<Registration.ReadModel.Conference> Conferences { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Conferences = await ConferenceService.GetPublishedConferences();
        }
    }
}