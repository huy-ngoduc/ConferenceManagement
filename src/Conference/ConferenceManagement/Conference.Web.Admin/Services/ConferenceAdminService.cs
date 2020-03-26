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
            client.DefaultRequestHeaders.Add("accessCode", accessCode);
            return await JsonSerializer.DeserializeAsync<ConferenceReadModel>(
                await client.GetStreamAsync($"conference/{slug}"),
                new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
        }

        public async Task<bool> Publish(string slug, string accessCode)
        {
            var modelJson =
                new StringContent(
                    JsonSerializer.Serialize(new ConferenceIdentity { AccessCode = accessCode, Slug = slug}),
                    Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"conference/publish", modelJson);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnPublish(string slug, string accessCode)
        {
            var modelJson =
                new StringContent(
                    JsonSerializer.Serialize(new ConferenceIdentity { AccessCode = accessCode, Slug = slug }),
                    Encoding.UTF8, "application/json");
            return (await client.PutAsync($"conference/unpublish", modelJson)).IsSuccessStatusCode;
        }

        public async Task<ConferenceIdentity> Locate(string email, string accessCode)
        {
            client.DefaultRequestHeaders.Add("accessCode", accessCode);
            return await JsonSerializer.DeserializeAsync<ConferenceIdentity>(
                await client.GetStreamAsync($"conference/locate/{email}"),
                new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
        }

        public async Task<string> CreateConference(ConferenceCreateInputModel inputModel)
        {
            var modelJson =
                new StringContent(
                    JsonSerializer.Serialize(inputModel),
                    Encoding.UTF8, "application/json");
            var responseMessage = await client.PostAsync($"conference", modelJson);

            if (responseMessage.IsSuccessStatusCode)
            {
                return await responseMessage.Content.ReadAsStringAsync();
            }

            return string.Empty;
        }
    }
}