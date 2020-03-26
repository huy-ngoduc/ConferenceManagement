using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Registration.ReadModel;

namespace Conference.Public.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConferenceController : ControllerBase
    {
        private readonly ILogger<ConferenceController> _logger;
        private readonly IConferenceDao conferenceDao;

        public ConferenceController(ILogger<ConferenceController> logger, IConferenceDao conferenceDao)
        {
            _logger = logger;
            this.conferenceDao = conferenceDao;
        }

        [HttpGet("/published")]
        public async Task<IActionResult> GetPublishedConferences()
        {
            return Ok(await conferenceDao.GetPublishedConferences());
        }
        [HttpGet("{conferenceCode}")]
        public async Task<IActionResult> GetConferenceDetails(string conferenceCode)
        {
            return Ok(await conferenceDao.GetConferenceDetails(conferenceCode));
        }
    }
}