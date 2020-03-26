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

 namespace Registration.Handlers
{
    using System.Linq;
    using Infrastructure.EventSourcing;
    using Registration.Commands;

    // Note: ConfirmOrderPayment was renamed to this from V1. Make sure there are no commands pending for processing when this is deployed,
    // otherwise the ConfirmOrderPayment commands will not be processed.
    public class OrderCommandHandler :
        IConsumer<RegisterToConference>,
        IConsumer<MarkSeatsAsReserved>,
        IConsumer<RejectOrder>,
        IConsumer<AssignRegistrantDetails>,
        IConsumer<ConfirmOrder>
    {
        private readonly IEventSourcedRepository<Order> repository;
        private readonly IPricingService pricingService;

        public OrderCommandHandler(IEventSourcedRepository<Order> repository, IPricingService pricingService)
        {
            this.repository = repository;
            this.pricingService = pricingService;
        }

        public async Task Consume(ConsumeContext<RegisterToConference> commandContext)
        {
            var items = commandContext.Message.Seats.Select(t => new OrderItem(t.SeatType, t.Quantity)).ToList();
            var order = await repository.Find(commandContext.Message.OrderId);
            if (order == null)
            {
                order = new Order(commandContext.Message.OrderId, commandContext.Message.ConferenceId, items, pricingService);
            }
            else
            {
                order.UpdateSeats(items, pricingService);
            }

            await repository.Save(order, commandContext.Message.Id.ToString());
        }

        public async Task Consume(ConsumeContext<MarkSeatsAsReserved> commandContext)
        {
            var order = await repository.Get(commandContext.Message.OrderId);
            order.MarkAsReserved(this.pricingService, commandContext.Message.Expiration, commandContext.Message.Seats);
            await repository.Save(order, commandContext.Message.Id.ToString());
        }

        public async Task Consume(ConsumeContext<RejectOrder> commandContext)
        {
            var order = await repository.Find(commandContext.Message.OrderId);
            // Explicitly idempotent. 
            if (order != null)
            {
                order.Expire();
                await repository.Save(order, commandContext.Message.Id.ToString());
            }
        }

        public async Task Consume(ConsumeContext<AssignRegistrantDetails> commandContext)
        {
            var order = await repository.Get(commandContext.Message.OrderId);
            order.AssignRegistrant(commandContext.Message.FirstName, commandContext.Message.LastName, commandContext.Message.Email);
            await repository.Save(order, commandContext.Message.Id.ToString());
        }

        public async Task Consume(ConsumeContext<ConfirmOrder> commandContext)
        {
            var order = await repository.Get(commandContext.Message.OrderId);
            order.Confirm();
            await repository.Save(order, commandContext.Message.Id.ToString());
        }
    }
}
