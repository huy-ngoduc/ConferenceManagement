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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Registration.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Conference;
    using Registration.Events;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class PricedOrderViewModelGenerator :
        IConsumer<OrderPlaced>,
        IConsumer<OrderTotalsCalculated>,
        IConsumer<OrderConfirmed>,
        IConsumer<OrderExpired>,
        IConsumer<SeatAssignmentsCreated>,
        IConsumer<SeatCreated>,
        IConsumer<SeatUpdated>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;
        private readonly IMemoryCache seatDescriptionsCache;

        public PricedOrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory, IMemoryCache seatDescriptionsCache )
        {
            this.contextFactory = contextFactory;
            this.seatDescriptionsCache = seatDescriptionsCache;
        }

        public async Task Consume(ConsumeContext<OrderPlaced> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = new PricedOrder
            {
                OrderId = consumeContext.Message.SourceId,
                ReservationExpirationDate = consumeContext.Message.ReservationAutoExpiration,
                OrderVersion = consumeContext.Message.Version
            };
            repository.Set<PricedOrder>().Add(dto);
            try
            {
                await repository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                Trace.TraceWarning(
                    "Ignoring OrderPlaced message with version {1} for order id {0}. This could be caused because the message was already handled and the PricedOrder entity was already created.",
                    dto.OrderId,
                    consumeContext.Message.Version);
            }
        }

        public async Task Consume(ConsumeContext<OrderTotalsCalculated> consumeContext)
        {
            var seatTypeIds = consumeContext.Message.Lines.OfType<SeatOrderLine>().Select(x => x.SeatType).Distinct()
                .ToArray();
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Set<PricedOrder>().Include(x => x.Lines)
                .FirstAsync(x => x.OrderId == consumeContext.Message.SourceId);
            if (!WasNotAlreadyHandled(dto, consumeContext.Message.Version))
            {
                // message already handled, skip.
                return;
            }

            var linesSet = repository.Set<PricedOrderLine>();
            foreach (var line in dto.Lines.ToList())
            {
                linesSet.Remove(line);
            }

            var seatTypeDescriptions = await GetSeatTypeDescriptions(seatTypeIds, repository);

            for (int i = 0; i < consumeContext.Message.Lines.Length; i++)
            {
                var orderLine = consumeContext.Message.Lines[i];
                var line = new PricedOrderLine
                {
                    LineTotal = orderLine.LineTotal,
                    Position = i,
                };

                if (orderLine is SeatOrderLine seatOrderLine)
                {
                    // should we update the view model to avoid loosing the SeatTypeId?
                    line.Description = seatTypeDescriptions.Where(x => x.SeatTypeId == seatOrderLine.SeatType)
                        .Select(x => x.Name).FirstOrDefault();
                    line.UnitPrice = seatOrderLine.UnitPrice;
                    line.Quantity = seatOrderLine.Quantity;
                }

                dto.Lines.Add(line);
            }

            dto.Total = consumeContext.Message.Total;
            dto.IsFreeOfCharge = consumeContext.Message.IsFreeOfCharge;
            dto.OrderVersion = consumeContext.Message.Version;

            await repository.SaveChangesAsync();
        }

        public async Task Consume(ConsumeContext<OrderConfirmed> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Find<PricedOrder>(consumeContext.Message.SourceId);
            if (WasNotAlreadyHandled(dto, consumeContext.Message.Version))
            {
                dto.ReservationExpirationDate = null;
                dto.OrderVersion = consumeContext.Message.Version;
                await repository.Save(dto);
            }
        }

        public async Task Consume(ConsumeContext<OrderExpired> consumeContext)
        {
            // No need to keep this priced order alive if it is expired.
            await using var repository = this.contextFactory.Invoke();
            var pricedOrder = new PricedOrder {OrderId = consumeContext.Message.SourceId};
            var set = repository.Set<PricedOrder>();
            set.Attach(pricedOrder);
            set.Remove(pricedOrder);
            try
            {
                await repository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                Trace.TraceWarning(
                    "Ignoring priced order expiration message with version {1} for order id {0}. This could be caused because the message was already handled and the entity was already deleted.",
                    pricedOrder.OrderId,
                    consumeContext.Message.Version);
            }
        }

        /// <summary>
        /// Saves the seat assignments correlation ID for further lookup.
        /// </summary>
        public async Task Consume(ConsumeContext<SeatAssignmentsCreated> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Find<PricedOrder>(consumeContext.Message.OrderId);
            dto.AssignmentsId = consumeContext.Message.SourceId;
            // Note: consumeContext.Message.Version does not correspond to order.Version.;
            await repository.SaveChangesAsync();
        }

        public async Task Consume(ConsumeContext<SeatCreated> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Find<PricedOrderLineSeatTypeDescription>(consumeContext.Message.SourceId);
            if (dto == null)
            {
                dto = new PricedOrderLineSeatTypeDescription {SeatTypeId = consumeContext.Message.SourceId};
                repository.Set<PricedOrderLineSeatTypeDescription>().Add(dto);
            }

            dto.Name = consumeContext.Message.Name;
            await repository.SaveChangesAsync();
        }

        public async Task Consume(ConsumeContext<SeatUpdated> consumeContext)
        {
            await using var context = this.contextFactory.Invoke();
            var dto = await context.Find<PricedOrderLineSeatTypeDescription>(consumeContext.Message.SourceId);
            if (dto == null)
            {
                dto = new PricedOrderLineSeatTypeDescription {SeatTypeId = consumeContext.Message.SourceId};
                context.Set<PricedOrderLineSeatTypeDescription>().Add(dto);
            }

            dto.Name = consumeContext.Message.Name;
            context.SaveChanges();
            this.seatDescriptionsCache.Set(dto.SeatTypeId.ToString(), dto, DateTimeOffset.UtcNow.AddMinutes(5));
        }

        private static bool WasNotAlreadyHandled(PricedOrder pricedOrder, int eventVersion)
        {
            // This assumes that events will be handled in order, but we might get the same message more than once.
            if (eventVersion > pricedOrder.OrderVersion)
            {
                return true;
            }
            else if (eventVersion == pricedOrder.OrderVersion)
            {
                Trace.TraceWarning(
                    "Ignoring duplicate priced order update message with version {1} for order id {0}",
                    pricedOrder.OrderId,
                    eventVersion);
                return false;
            }
            else
            {
                Trace.TraceWarning(
                    @"Ignoring an older order update message was received with with version {1} for order id {0}, last known version {2}.
This read model generator has an expectation that the EventBus will deliver messages for the same source in order. Nevertheless, this warning can be expected in a migration scenario.",
                    pricedOrder.OrderId,
                    eventVersion,
                    pricedOrder.OrderVersion);
                return false;
            }
        }

        private async Task<List<PricedOrderLineSeatTypeDescription>> GetSeatTypeDescriptions(IEnumerable<Guid> seatTypeIds,
            DbContext context)
        {
            var result = new List<PricedOrderLineSeatTypeDescription>();
            var notCached = new List<Guid>();

            foreach (var seatType in seatTypeIds)
            {
                var cached = (PricedOrderLineSeatTypeDescription) this.seatDescriptionsCache.Get(seatType.ToString());
                if (cached == null)
                {
                    notCached.Add(seatType);
                }
                else
                {
                    result.Add(cached);
                }
            }

            if (notCached.Count <= 0) return result;

            var notCachedArray = notCached.ToArray();
            var seatTypeDescriptions = await context.Set<PricedOrderLineSeatTypeDescription>()
                .Where(x => notCachedArray.Contains(x.SeatTypeId))
                .ToListAsync();

            foreach (var seatType in seatTypeDescriptions)
            {
                // even though we went got a fresh version we don't want to overwrite a fresher version set by the event handler for seat descriptions
                var desc = await this.seatDescriptionsCache
                               .GetOrCreate(
                                   seatType.SeatTypeId.ToString(),
                                   async (entry) =>
                                   {
                                       entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5);
                                       return await Task.FromResult(seatType);
                                   })
                           ?? seatType;

                result.Add(desc);
            }

            return result;
        }
    }
}