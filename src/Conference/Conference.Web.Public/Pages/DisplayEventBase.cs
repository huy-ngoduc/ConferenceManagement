using System.Collections.Generic;
using System.Threading.Tasks;
using Conference.Web.Public.Services;
using Microsoft.AspNetCore.Components;

namespace Conference.Web.Public.Pages
{
    public class DisplayEventBase : ComponentBase
    {
        [Parameter] public string ConferenceCode { get; set; }
        [Inject] public ConferenceService ConferenceService { get; set; }
        public Registration.ReadModel.ConferenceDetails Conference { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Conference = await ConferenceService.GetConference(ConferenceCode);
        }
    }
}