using System;
using System.Threading.Tasks;
using Conference.Admin.Share;

namespace Conference.Web.Admin.Services
{
    public interface IConferenceAdminService
    {
        Task<ConferenceReadModel> Get(string slug, string accessCode);
        Task<bool> Publish(string slug, string accessCode);
        Task<bool> UnPublish(string slug, string accessCode);
        Task<ValueTuple<string, string>> Locate(string email, string accessCode);
        Task<string> CreateConference(ConferenceCreateInputModel inputModel);
    }
}