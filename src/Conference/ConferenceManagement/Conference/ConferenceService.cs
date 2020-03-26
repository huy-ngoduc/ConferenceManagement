// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================


namespace Conference
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using MassTransit;
    using Microsoft.EntityFrameworkCore;
    using Polly;
    using Polly.Contrib.WaitAndRetry;
    using Polly.Retry;

    /// <summary>
    /// Transaction-script style domain service that manages 
    /// the interaction between the MVC controller and the 
    /// ORM persistence, as well as the publishing of integration 
    /// events.
    /// </summary>
    public class ConferenceService
    {
        private readonly ConferenceContext context;
        private readonly IBus bus;
        private readonly AsyncRetryPolicy retryPolicy;

        public ConferenceService(IBus bus, ConferenceContext conferenceContext)
        {
            // NOTE: the database storage cannot be transactional consistent with the 
            // event bus, so there is a chance that the conference state is saved 
            // to the database but the events are not published. The recommended 
            // mechanism to solve this lack of transaction support is to persist 
            // failed events to a table in the same database as the conference, in a 
            // queue that is retried until successful delivery of events is 
            // guaranteed. This mechanism has been implemented for the AzureEventSourcedRepository
            // and that implementation can be used as a guide to implement it here too.

            context = conferenceContext;
            this.bus = bus;

            var delay = Backoff.ConstantBackoff(TimeSpan.FromMilliseconds(200), retryCount: 5, fastFirst: true);

            this.retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(delay, (exception, i, span) =>
                {
                    Trace.TraceWarning(
                        $"An error occurred in attempt number {i} to access the database in ConferenceService: {exception.Message}");
                });
        }

        public async Task CreateConference(ConferenceInfo conference)
        {
            var existingSlug = await this.retryPolicy.ExecuteAsync(async () =>
                await context.Conferences
                    .Where(c => c.Slug == conference.Slug)
                    .Select(c => c.Slug)
                    .AnyAsync());

            if (existingSlug)
                throw new DuplicateNameException("The chosen conference slug is already taken.");

            // Conference publishing is explicit. 
            if (conference.IsPublished)
                conference.IsPublished = false;

            context.Conferences.Add(conference);
            await this.retryPolicy.ExecuteAsync(async () => await context.SaveChangesAsync());
            await this.PublishConferenceEvent<ConferenceCreated>(conference);
        }
//
//        public async Task CreateSeat(Guid conferenceId, SeatType seat)
//        {
//            var conference =
//                await this.retryPolicy.ExecuteAsync(async () => await context.Conferences.FindAsync(conferenceId));
//            if (conference == null)
//                throw new Exception(); //TODO: ObjectNotFoundException
//
//            conference.Seats.Add(seat);
//            await this.retryPolicy.ExecuteAsync(async () => await context.SaveChangesAsync());
//
//            // Don't publish new seats if the conference was never published 
//            // (and therefore is not published either).
//            if (conference.WasEverPublished)
//                await this.PublishSeatCreated(conferenceId, seat);
//        }

        public async Task<ConferenceInfo> FindConference(string slug)
        {
            return await this.retryPolicy.ExecuteAsync(async () =>await context.Conferences.FirstOrDefaultAsync(x => x.Slug == slug));
        }

        public async Task<ConferenceInfo> FindConference(string email, string accessCode)
        {
            return await this.retryPolicy.ExecuteAsync(async() =>
                await context.Conferences.FirstOrDefaultAsync(x => x.OwnerEmail == email && x.AccessCode == accessCode));
        }
//
//        public async Task<IEnumerable<SeatType>> FindSeatTypes(Guid conferenceId)
//        {
//            return await this.retryPolicy.ExecuteAsync(async () => await
//                       context.Conferences
//                           .Include(x => x.Seats)
//                           .Where(x => x.Id == conferenceId)
//                           .Select(x => x.Seats)
//                           .FirstOrDefaultAsync()) ??
//                   Enumerable.Empty<SeatType>();
//        }
//
//        public async Task<SeatType> FindSeatType(Guid seatTypeId)
//        {
//            return await this.retryPolicy.ExecuteAsync(async () => await context.Seats.FindAsync(seatTypeId));
//        }
//
//        public async Task<IEnumerable<Order>> FindOrders(Guid conferenceId)
//        {
//            return await this.retryPolicy.ExecuteAsync(async () =>await context.Orders.Include("Seats.SeatInfo")
//                .Where(x => x.ConferenceId == conferenceId)
//                .ToListAsync());
//        }

        public async Task UpdateConference(ConferenceInfo conference)
        {
            var existing = await this.retryPolicy.ExecuteAsync(async () =>await context.Conferences.FindAsync(conference.Id));
            if (existing == null)
                throw new Exception(); //ObjectNotFoundException

            context.Entry(existing).CurrentValues.SetValues(conference);
            await this.retryPolicy.ExecuteAsync(async () =>await context.SaveChangesAsync());

            await this.PublishConferenceEvent<ConferenceUpdated>(conference);
        }

//        public async Task UpdateSeat(Guid conferenceId, SeatType seat)
//        {
//            var existing = await this.retryPolicy.ExecuteAsync(async () =>await context.Seats.FindAsync(seat.Id));
//            if (existing == null)
//                throw new Exception(); //ObjectNotFoundException
//
//            context.Entry(existing).CurrentValues.SetValues(seat);
//            await this.retryPolicy.ExecuteAsync(async () =>await context.SaveChangesAsync());
//
//            // Don't publish seat updates if the conference was never published 
//            // (and therefore is not published either).
//            if (await this.retryPolicy.ExecuteAsync(async () =>await 
//                context.Conferences.Where(x => x.Id == conferenceId).Select(x => x.WasEverPublished)
//                    .FirstOrDefaultAsync()))
//            {
//                await this.bus.Publish(new SeatUpdated
//                {
//                    ConferenceId = conferenceId,
//                    SourceId = seat.Id,
//                    Name = seat.Name,
//                    Description = seat.Description,
//                    Price = seat.Price,
//                    Quantity = seat.Quantity,
//                });
//            }
//        }

        public async Task Publish(Guid conferenceId)
        {
            await this.UpdatePublished(conferenceId, true);
        }

        public async Task Unpublish(Guid conferenceId)
        {
            await this.UpdatePublished(conferenceId, false);
        }

        private async Task UpdatePublished(Guid conferenceId, bool isPublished)
        {
            var conference = await this.retryPolicy.ExecuteAsync(async () =>await context.Conferences.FindAsync(conferenceId));
            if (conference == null)
                throw new Exception(); //TODO: ObjectNotFoundException

            conference.IsPublished = isPublished;
            if (isPublished && !conference.WasEverPublished)
            {
                // This flags prevents any further seat type deletions.
                conference.WasEverPublished = true;
                await this.retryPolicy.ExecuteAsync(async () =>await context.SaveChangesAsync());
//
//                // We always publish events *after* saving to store.
//                // Publish all seats that were created before.
//                foreach (var seat in conference.Seats)
//                {
//                    await PublishSeatCreated(conference.Id, seat);
//                }
            }
            else
            {
                await this.retryPolicy.ExecuteAsync(async () =>await context.SaveChangesAsync());
            }

            if (isPublished)
                await this.bus.Publish(new ConferencePublished {SourceId = conferenceId});
            else
                await this.bus.Publish(new ConferenceUnpublished {SourceId = conferenceId});
        }
//
//        public async Task DeleteSeat(Guid id)
//        {
//            var seat = await this.retryPolicy.ExecuteAsync(async () =>await context.Seats.FindAsync(id));
//            if (seat == null)
//                throw new Exception(); //TODO: ObjectNotFoundException
//
//            var wasPublished = await this.retryPolicy.ExecuteAsync(async () =>await context.Conferences
//                .Where(x => x.Seats.Any(s => s.Id == id))
//                .Select(x => x.WasEverPublished)
//                .FirstOrDefaultAsync());
//
//            if (wasPublished)
//                throw new InvalidOperationException(
//                    "Can't delete seats from a conference that has been published at least once.");
//
//            context.Seats.Remove(seat);
//            await this.retryPolicy.ExecuteAsync(async () =>await context.SaveChangesAsync());
//        }

        private async Task PublishConferenceEvent<T>(ConferenceInfo conference)
            where T : ConferenceEvent, new()
        {
            await this.retryPolicy.ExecuteAsync(async () => await this.bus.Publish(new T()
            {
                SourceId = conference.Id,
                Owner = new Owner
                {
                    Name = conference.OwnerName,
                    Email = conference.OwnerEmail,
                },
                Name = conference.Name,
                Description = conference.Description,
                Location = conference.Location,
                Slug = conference.Slug,
                Tagline = conference.Tagline,
                TwitterSearch = conference.TwitterSearch,
                StartDate = conference.StartDate,
                EndDate = conference.EndDate,
            }));
        }
//
//        private async Task PublishSeatCreated(Guid conferenceId, SeatType seat)
//        {
//            await this.retryPolicy.ExecuteAsync(async () => await this.bus.Publish(new SeatCreated
//            {
//                ConferenceId = conferenceId,
//                SourceId = seat.Id,
//                Name = seat.Name,
//                Description = seat.Description,
//                Price = seat.Price,
//                Quantity = seat.Quantity,
//            }));
//        }
    }
}