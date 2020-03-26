using System;
using System.Data;
using System.Threading.Tasks;
using AutoMapper;
using Conference.Admin.Share;
using Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Conference.Admin.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConferenceController : ControllerBase
    {
        private readonly ConferenceService _service;
        private readonly ILogger<ConferenceController> _logger;
        private readonly IMapper _mapper;

        public ConferenceController(ConferenceService service, ILogger<ConferenceController> logger, IMapper mapper)
        {
            _service = service;
            _logger = logger;
            _mapper = mapper;
        }

        #region Conference Details

        [HttpGet("{slug}")]
        public async Task<IActionResult> Get(string slug, [FromHeader]string accessCode)
        {
            var conference = await _service.FindConference(slug);
            if (conference == null)
            {
                return NotFound();
            }

            // check access
            if (accessCode == null || !string.Equals(accessCode, conference.AccessCode, StringComparison.Ordinal))
            {
                return Unauthorized("Invalid access code.");
            }

            return Ok(_mapper.Map<ConferenceReadModel>(conference));
        }
        
        [HttpGet("locate/{email}")]
        public async Task<IActionResult> Locate(string email, [FromHeader]string accessCode)
        {
            var conference = await this._service.FindConference(email, accessCode);
            if (conference == null)
            {
                return NotFound("Could not locate a conference with the provided email and access code.");
            }

            return Ok(new ConferenceIdentity{Slug = conference.Slug, AccessCode = conference.AccessCode});
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ConferenceCreateInputModel inputModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var conference = _mapper.Map<ConferenceInfo>(inputModel);
            try
            {
                conference.Id = GuidUtil.NewSequentialId();
                await this._service.CreateConference(conference);

                return Ok(conference.AccessCode);
            }
            catch (DuplicateNameException e)
            {
                ModelState.AddModelError("Slug", e.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] ConferenceEditInputModel inputModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var conference = await _service.FindConference(inputModel.Slug);
            if (conference == null)
            {
                return NotFound();
            }

            // check access
            if (inputModel.AccessCode == null ||
                !string.Equals(inputModel.AccessCode, conference.AccessCode, StringComparison.Ordinal))
            {
                return Unauthorized("Invalid access code.");
            }

            var edited = _mapper.Map(inputModel, conference);
            await _service.UpdateConference(edited);

            return Ok();
        }

        [HttpPut]
        [Microsoft.AspNetCore.Mvc.Route("publish")]
        public async Task<IActionResult> Publish(ConferenceIdentity identity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var conference = await this._service.FindConference(identity.Slug);
            if (conference == null)
            {
                return NotFound();
            }

            // check access
            if (string.IsNullOrWhiteSpace(identity.AccessCode) ||
                !string.Equals(identity.AccessCode, conference.AccessCode, StringComparison.Ordinal))
            {
                return Unauthorized("Invalid access code.");
            }

            await _service.Publish(conference.Id);

            return Ok();
        }

        [HttpPut]
        [Microsoft.AspNetCore.Mvc.Route("unpublish/")]
        public async Task<IActionResult> Unpublish(ConferenceIdentity identity)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var conference = await this._service.FindConference(identity.Slug);
            if (conference == null)
            {
                return NotFound();
            }

            // check access
            if (string.IsNullOrWhiteSpace(identity.AccessCode) ||
                !string.Equals(identity.AccessCode, conference.AccessCode, StringComparison.Ordinal))
            {
                return Unauthorized("Invalid access code.");
            }

            await _service.Unpublish(conference.Id);

            return Ok();
        }

        #endregion

        #region Seat Types

//
//        public ViewResult Seats()
//        {
//            return View();
//        }
//
//        public ActionResult SeatGrid()
//        {
//            if (this.Conference == null)
//            {
//                return HttpNotFound();
//            }
//
//            return PartialView(this._service.FindSeatTypes(this.Conference.Id));
//        }
//
//        public ActionResult SeatRow(Guid id)
//        {
//            return PartialView("SeatGrid", new SeatType[] { this._service.FindSeatType(id) });
//        }
//
//        public ActionResult CreateSeat()
//        {
//            return PartialView("EditSeat");
//        }
//
//        [HttpPost]
//        public ActionResult CreateSeat(SeatType seat)
//        {
//            if (this.Conference == null)
//            {
//                return HttpNotFound();
//            }
//
//            if (ModelState.IsValid)
//            {
//                seat.Id = GuidUtil.NewSequentialId();
//                this._service.CreateSeat(this.Conference.Id, seat);
//
//                return PartialView("SeatGrid", new SeatType[] { seat });
//            }
//
//            return PartialView("EditSeat", seat);
//        }
//
//        public ActionResult EditSeat(Guid id)
//        {
//            if (this.Conference == null)
//            {
//                return HttpNotFound();
//            }
//
//            return PartialView(this._service.FindSeatType(id));
//        }
//
//        [HttpPost]
//        public ActionResult EditSeat(SeatType seat)
//        {
//            if (this.Conference == null)
//            {
//                return HttpNotFound();
//            }
//
//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    this._service.UpdateSeat(this.Conference.Id, seat);
//                }
//                catch (ObjectNotFoundException)
//                {
//                    return HttpNotFound();
//                }
//
//                return PartialView("SeatGrid", new SeatType[] { seat });
//            }
//
//            return PartialView(seat);
//        }
//
//        [HttpPost]
//        public void DeleteSeat(Guid id)
//        {
//            this._service.DeleteSeat(id);
//        }

        #endregion

        #region Orders

//
//        public ViewResult Orders()
//        {
//            var orders = this._service.FindOrders(this.Conference.Id);
//
//            return View(orders);
//        }

        #endregion
    }
}