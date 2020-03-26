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

using System.Threading.Tasks;
using MassTransit;

namespace Registration.Handlers
{
    using Infrastructure.EventSourcing;
    using Registration.Commands;

    /// <summary>
    /// Handles commands issued to the seats availability aggregate.
    /// </summary>
    public class SeatsAvailabilityHandler :
        IConsumer<MakeSeatReservation>,
        IConsumer<CancelSeatReservation>,
        IConsumer<CommitSeatReservation>,
        IConsumer<AddSeats>,
        IConsumer<RemoveSeats>
    {
        private readonly IEventSourcedRepository<SeatsAvailability> repository;

        public SeatsAvailabilityHandler(IEventSourcedRepository<SeatsAvailability> repository)
        {
            this.repository = repository;
        }

        public async Task Consume(ConsumeContext<MakeSeatReservation> consumeContext)
        {
            var availability = await this.repository.Get(consumeContext.Message.ConferenceId);
            availability.MakeReservation(consumeContext.Message.ReservationId, consumeContext.Message.Seats);
            await this.repository.Save(availability, consumeContext.Message.Id.ToString());
        }

        public async Task Consume(ConsumeContext<CancelSeatReservation> consumeContext)
        {
            var availability = await this.repository.Get(consumeContext.Message.ConferenceId);
            availability.CancelReservation(consumeContext.Message.ReservationId);
            await this.repository.Save(availability, consumeContext.Message.Id.ToString());
        }

        public async Task Consume(ConsumeContext<CommitSeatReservation> consumeContext)
        {
            var availability = await this.repository.Get(consumeContext.Message.ConferenceId);
            availability.CommitReservation(consumeContext.Message.ReservationId);
            await this.repository.Save(availability, consumeContext.Message.Id.ToString());
        }

        // Commands created from events from the conference BC

        public async Task Consume(ConsumeContext<AddSeats> consumeContext)
        {
            var availability = await this.repository.Find(consumeContext.Message.ConferenceId) ??
                               new SeatsAvailability(consumeContext.Message.ConferenceId);

            availability.AddSeats(consumeContext.Message.SeatType, consumeContext.Message.Quantity);
            await this.repository.Save(availability, consumeContext.Message.Id.ToString());
        }

        public async Task Consume(ConsumeContext<RemoveSeats> consumeContext)
        {
            var availability = await this.repository.Find(consumeContext.Message.ConferenceId) ??
                               new SeatsAvailability(consumeContext.Message.ConferenceId);

            availability.RemoveSeats(consumeContext.Message.SeatType, consumeContext.Message.Quantity);
            await this.repository.Save(availability, consumeContext.Message.Id.ToString());
        }
    }
}