using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Conference.Admin.Share;
using System.Text.Json;

namespace Conference.Web.Admin.Services
{
    public class ConferenceAdminService : IConferenceAdminService
    {
        private readonly HttpClient client;

        public ConferenceAdminService(HttpClient client)
        {
            this.client = client;
        }

        public async Task<ConferenceReadModel> Get(string slug, string accessCode)
        {
            return await JsonSerializer.DeserializeAsync<ConferenceReadModel>(
                await client.GetStreamAsync($"api/conference/?slug={slug}&accessCode={accessCode}"),
                new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
        }

        public async Task<bool> Publish(string slug, string accessCode)
        {
            var modelJson =
                new StringContent(
                    JsonSerializer.Serialize(new PublishInputModel() {Slug = slug, AccessCode = accessCode}),
                    Encoding.UTF8, "application/json");
            return (await client.PutAsync($"api/conference/publish", modelJson)).IsSuccessStatusCode;
        }

        public async Task<bool> UnPublish(string slug, string accessCode)
        {
            var modelJson =
                new StringContent(
                    JsonSerializer.Serialize(new PublishInputModel() {Slug = slug, AccessCode = accessCode}),
                    Encoding.UTF8, "application/json");
            return (await client.PutAsync($"api/conference/unpublish", modelJson)).IsSuccessStatusCode;
        }

        public async Task<(string, string)> Locate(string email, string accessCode)
        {
            return await JsonSerializer.DeserializeAsync<(string, string)>(
                await client.GetStreamAsync($"api/conference/?slug={email}&accessCode={accessCode}"),
                new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
        }

        public async Task<string> CreateConference(ConferenceCreateInputModel inputModel)
        {
            var modelJson =
                new StringContent(
                    JsonSerializer.Serialize(inputModel),
                    Encoding.UTF8, "application/json");
            var responseMessage = await client.PostAsync($"api/conference/create", modelJson);

            if (responseMessage.IsSuccessStatusCode)
            {
                return await responseMessage.Content.ReadAsStringAsync();
            }

            return string.Empty;
        }
    }
}