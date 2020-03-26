using Registration.ReadModel;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Conference.Web.Public.Services
{
    public class ConferenceService
    {
        private readonly HttpClient client;

        public ConferenceService(HttpClient client)
        {
            this.client = client;
        }

        public async Task<IList<ConferenceAlias>> GetPublishedConferences()
        {
            return await JsonSerializer.DeserializeAsync<List<ConferenceAlias>>(
                await client.GetStreamAsync("conference/published"),
                new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
        }
        
        public async Task<ConferenceDetails> GetConference(string conferenceCode)
        {
            return await JsonSerializer.DeserializeAsync<ConferenceDetails>(
                await client.GetStreamAsync($"conference/details/{conferenceCode}"),
                new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
        }
    }
}