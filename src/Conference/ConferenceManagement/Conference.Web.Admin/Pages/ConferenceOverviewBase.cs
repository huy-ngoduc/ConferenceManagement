using System;
using System.Threading.Tasks;
using Conference.Admin.Share;
using Conference.Web.Admin.Services;
using Conference.Web.Admin.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;

namespace Conference.Web.Admin.Pages
{
    public class ConferenceOverviewBase : ComponentBase
    {
        [Inject] public IConferenceAdminService ConferenceAdminService { get; set; }
        [Inject] public ProtectedSessionStorage SessionStorage { get; set; }
        [Inject] public ILogger<ConferenceOverviewBase> _logger { get; set; }
        [Parameter] public string Slug { get; set; }

        public string AccessCode { get; set; }
        public ConferenceReadModel Conference { get; set; } = new ConferenceReadModel();

        public string Message { get; set; }
        public bool HasError { get; set; }
        public string PublishButtonText { get; set; }

        public bool IsPublished { get; set; }

        protected override async Task OnInitializedAsync()
        {
            AccessCode = await SessionStorage.GetAsync<string>(LocalStorageKey.AccessCode);
            Conference = await ConferenceAdminService.Get(Slug, AccessCode);
            IsPublished = Conference.IsPublished;

            Message = await SessionStorage.GetAsync<string>(LocalStorageKey.Notification);
            await SessionStorage.DeleteAsync(LocalStorageKey.Notification);
            PublishButtonText = GetPublishButtonText(Conference.IsPublished);

            HasError = false;
        }

        private string GetPublishButtonText(bool isPublished)
        {
            return isPublished ? "Unpublish" : "Publish";
        }

        protected async Task Publish()
        {

            HasError = false;
            Message = string.Empty;
            IsPublished = await ConferenceAdminService.Publish(Slug, AccessCode);

            Message = $"Conference[{Slug}] was published { (IsPublished ? "successful" : "unsuccessful") }";
            PublishButtonText = GetPublishButtonText(IsPublished);
        }

        protected async Task UnPublish()
        {
            HasError = false;
            Message = string.Empty;
            IsPublished = !(await ConferenceAdminService.UnPublish(Slug, AccessCode));
            Message = $"Conference[{Slug}] was unpublished { (IsPublished ? "unsuccessful" : "successful") }";
            PublishButtonText = GetPublishButtonText(IsPublished);
        }

        protected async Task HandlePublishEvent()
        {
            if (IsPublished)
            {
                await UnPublish();
            }
            else
            {
                await Publish();
            }
        }
    }
}