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
    using Infrastructure.EventSourcing;
    using Registration.Commands;
    using Registration.Events;

    public class SeatAssignmentsHandler :
        IConsumer<OrderConfirmed>,
        IConsumer<UnassignSeat>,
        IConsumer<AssignSeat>
    {
        private readonly IEventSourcedRepository<Order> ordersRepository;
        private readonly IEventSourcedRepository<SeatAssignments> assignmentsRepository;

        public SeatAssignmentsHandler(IEventSourcedRepository<Order> ordersRepository, IEventSourcedRepository<SeatAssignments> assignmentsRepository)
        {
            this.ordersRepository = ordersRepository;
            this.assignmentsRepository = assignmentsRepository;
        }

        public async Task Consume(ConsumeContext<OrderConfirmed> consumeContext)
        {
            var order = await this.ordersRepository.Get(consumeContext.Message.SourceId);
            var assignments = order.CreateSeatAssignments();
            await assignmentsRepository.Save(assignments, null);
        }

        public async Task Consume(ConsumeContext<AssignSeat> consumeContext)
        {
            var assignments = await this.assignmentsRepository.Get(consumeContext.Message.SeatAssignmentsId);
            assignments.AssignSeat(consumeContext.Message.Position, consumeContext.Message.Attendee);
            await assignmentsRepository.Save(assignments, consumeContext.Message.Id.ToString());
        }

        public async Task Consume(ConsumeContext<UnassignSeat> consumeContext)
        {
            var assignments =await this.assignmentsRepository.Get(consumeContext.Message.SeatAssignmentsId);
            assignments.Unassign(consumeContext.Message.Position);
            await assignmentsRepository.Save(assignments, consumeContext.Message.Id.ToString());
        }
    }
}
