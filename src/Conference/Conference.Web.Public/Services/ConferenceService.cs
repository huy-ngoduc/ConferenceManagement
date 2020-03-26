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

        public async Task<IList<Registration.ReadModel.Conference>> GetPublishedConferences()
        {
            return await JsonSerializer.DeserializeAsync<List<Registration.ReadModel.Conference>>(
                await client.GetStreamAsync("conference/published"),
                new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
        }
        
        public async Task<Registration.ReadModel.ConferenceDetails> GetConference(string conferenceCode)
        {
            return await JsonSerializer.DeserializeAsync<Registration.ReadModel.ConferenceDetails>(
                await client.GetStreamAsync($"conference/{conferenceCode}"),
                new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
        }
    }
}