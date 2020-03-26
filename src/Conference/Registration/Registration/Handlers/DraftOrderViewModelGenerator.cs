﻿// ==============================================================================================================
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
 using Microsoft.EntityFrameworkCore;

 namespace Registration.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Registration.Events;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class DraftOrderViewModelGenerator :
        IConsumer<OrderPlaced>, IConsumer<OrderUpdated>,
        IConsumer<OrderPartiallyReserved>, IConsumer<OrderReservationCompleted>,
        IConsumer<OrderRegistrantAssigned>,
        IConsumer<OrderConfirmed>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public DraftOrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task Consume(ConsumeContext<OrderPlaced> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = new DraftOrder(consumeContext.Message.SourceId, consumeContext.Message.ConferenceId, DraftOrder.States.PendingReservation, consumeContext.Message.Version)
            {
                AccessCode = consumeContext.Message.AccessCode,
            };
            dto.Lines.AddRange(consumeContext.Message.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

            await repository.Save(dto);
        }

        public async Task Consume(ConsumeContext<OrderRegistrantAssigned> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Find<DraftOrder>(consumeContext.Message.SourceId);
            if (WasNotAlreadyHandled(dto, consumeContext.Message.Version))
            {
                dto.RegistrantEmail = consumeContext.Message.Email;
                dto.OrderVersion = consumeContext.Message.Version;
                await repository.Save(dto);
            }
        }

        public async Task Consume(ConsumeContext<OrderUpdated> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Set<DraftOrder>().Include(o => o.Lines).FirstAsync(o => o.OrderId == consumeContext.Message.SourceId);
            if (WasNotAlreadyHandled(dto, consumeContext.Message.Version))
            {
                var linesSet = repository.Set<DraftOrderItem>();
                foreach (var line in dto.Lines.ToArray())
                {
                    linesSet.Remove(line);
                }

                dto.Lines.AddRange(consumeContext.Message.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

                dto.State = DraftOrder.States.PendingReservation;
                dto.OrderVersion = consumeContext.Message.Version;

                await repository.Save(dto);
            }
        }

        public async Task Consume(ConsumeContext<OrderPartiallyReserved> consumeContext)
        {
            await this.UpdateReserved(consumeContext.Message.SourceId, consumeContext.Message.ReservationExpiration, DraftOrder.States.PartiallyReserved,
                consumeContext.Message.Version, consumeContext.Message.Seats);
        }

        public async Task Consume(ConsumeContext<OrderReservationCompleted> consumeContext)
        {
            await this.UpdateReserved(consumeContext.Message.SourceId, consumeContext.Message.ReservationExpiration, 
                DraftOrder.States.ReservationCompleted, consumeContext.Message.Version, consumeContext.Message.Seats);
        }

        public async Task Consume(ConsumeContext<OrderConfirmed> context)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Find<DraftOrder>(context.Message.SourceId);
            if (WasNotAlreadyHandled(dto, context.Message.Version))
            {
                dto.State = DraftOrder.States.Confirmed;
                dto.OrderVersion = context.Message.Version;
                await repository.Save(dto);
            }
        }

        private async Task UpdateReserved(Guid orderId, DateTime reservationExpiration, DraftOrder.States state, int orderVersion, IEnumerable<SeatQuantity> seats)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = repository.Set<DraftOrder>().Include(x => x.Lines).First(x => x.OrderId == orderId);
            if (WasNotAlreadyHandled(dto, orderVersion))
            {
                foreach (var seat in seats)
                {
                    var item = dto.Lines.Single(x => x.SeatType == seat.SeatType);
                    item.ReservedSeats = seat.Quantity;
                }

                dto.State = state;
                dto.ReservationExpirationDate = reservationExpiration;

                dto.OrderVersion = orderVersion;

                await repository.Save(dto);
            }
        }

        private static bool WasNotAlreadyHandled(DraftOrder draftOrder, int eventVersion)
        {
            // This assumes that events will be handled in order, but we might get the same message more than once.
            if (eventVersion > draftOrder.OrderVersion)
            {
                return true;
            }
            else if (eventVersion == draftOrder.OrderVersion)
            {
                Trace.TraceWarning(
                    "Ignoring duplicate draft order update message with version {1} for order id {0}",
                    draftOrder.OrderId,
                    eventVersion);
                return false;
            }
            else
            {
                Trace.TraceWarning(
                    @"An older order update message was received with with version {1} for order id {0}, last known version {2}.
This read model generator has an expectation that the EventBus will deliver messages for the same source in order.",
                    draftOrder.OrderId,
                    eventVersion,
                    draftOrder.OrderVersion);
                return false;
            }
        }
    }
}
