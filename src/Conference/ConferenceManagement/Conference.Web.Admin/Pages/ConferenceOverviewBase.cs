using System;
using System.Threading.Tasks;
using Conference.Admin.Share;
using Conference.Web.Admin.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;

namespace Conference.Web.Admin.Pages
{
    public class ConferenceOverviewBase : ComponentBase
    {
        [Inject] public IConferenceAdminService ConferenceAdminService { get; set; }
        [Inject] public ProtectedLocalStorage LocalStorage { get; set; }
        [Inject] public ILogger<ConferenceOverviewBase> _logger { get; set; }
        [Parameter] public string Slug { get; set; }
        public string AccessCode { get; set; }
        public ConferenceReadModel Conference { get; set; }

        public string Message { get; set; }
        public bool HasError { get; set; }
        
        protected override async Task OnInitializedAsync()
        {
            Conference = await ConferenceAdminService.Get(Slug, AccessCode);
            Message = await LocalStorage.GetAsync<string>("notification");
            HasError = false;
        }

        protected async Task Publish()
        {
            try
            {
                HasError = false;
                Message = string.Empty;
                await ConferenceAdminService.Publish(Slug, AccessCode);
                Message = $"Conference[{Slug}] was published successful";
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Debug, e, e.Message);
                HasError = true;
                Message = $"Conference[{Slug}] was published unsuccessful";
            }
        }
        
        protected async Task UnPublish()
        {
            try
            {
                HasError = false;
                Message = string.Empty;
                await ConferenceAdminService.Publish(Slug, AccessCode);
                Message = $"Conference[{Slug}] was unpublished successful";
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Debug, e, e.Message);
                HasError = true;
                Message = $"Conference[{Slug}] was unpublished unsuccessful";
            }
        }
    }
}