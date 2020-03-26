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
 using AutoMapper;

 namespace Registration.Handlers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using MassTransit;
    using Infrastructure.BlobStorage;
    using Infrastructure.Serialization;
    using Registration.Events;
    using Registration.ReadModel;
    using System.Collections.Generic;
    
    public class SeatAssignmentsViewModelGenerator :
        IConsumer<SeatAssignmentsCreated>,
        IConsumer<SeatAssigned>,
        IConsumer<SeatUnassigned>,
        IConsumer<SeatAssignmentUpdated>
    {
        private readonly IBlobStorage storage;
        private readonly ITextSerializer serializer;
        private readonly IConferenceDao conferenceDao;
        private readonly IMapper mapper;

        public SeatAssignmentsViewModelGenerator(
            IConferenceDao conferenceDao,
            IBlobStorage storage,
            ITextSerializer serializer,
            IMapper mapper)
        {
            this.conferenceDao = conferenceDao;
            this.storage = storage;
            this.serializer = serializer;
            this.mapper = mapper;
        }

        //TODO: register mapper to container
//        static SeatAssignmentsViewModelGenerator()
//        {
//            mapper.CreateMap<SeatAssigned, OrderSeat>();
//            mapper.CreateMap<SeatAssignmentUpdated, OrderSeat>();
//        }
        
        public static string GetSeatAssignmentsBlobId(Guid sourceId)
        {
            return "SeatAssignments-" + sourceId;
        }

        public async Task Consume(ConsumeContext<SeatAssignmentsCreated> consumeContext)
        {
            var seatTypes =
                (await this.conferenceDao.GetSeatTypeNames(consumeContext.Message.Seats.Select(x => x.SeatType)))
                .ToDictionary(x => x.Id, x => x.Name);

            var dto = new OrderSeats(consumeContext.Message.SourceId, consumeContext.Message.OrderId, 
                consumeContext.Message.Seats.Select(i =>
                new OrderSeat(i.Position, seatTypes.TryGetValue(i.SeatType))));
            
            await Save(dto);
        }

        public async Task Consume(ConsumeContext<SeatAssigned> consumeContext)
        {
            var dto = await Find(consumeContext.Message.SourceId);
            var seat = dto.Seats.First(x => x.Position == consumeContext.Message.Position);
            mapper.Map(consumeContext.Message, seat);
            await Save(dto);
        }

        public async Task Consume(ConsumeContext<SeatUnassigned> consumeContext)
        {
            var dto = await Find(consumeContext.Message.SourceId);
            var seat = dto.Seats.First(x => x.Position == consumeContext.Message.Position);
            seat.Attendee.Email = seat.Attendee.FirstName = seat.Attendee.LastName = null;
            await Save(dto);
        }

        public async Task Consume(ConsumeContext<SeatAssignmentUpdated> consumeContext)
        {
            var dto = await Find(consumeContext.Message.SourceId);
            var seat = dto.Seats.First(x => x.Position == consumeContext.Message.Position);
            mapper.Map(consumeContext.Message, seat);
            await Save(dto);
        }

        private async  Task<OrderSeats> Find(Guid id)
        {
            var dto = await this.storage.Find(GetSeatAssignmentsBlobId(id));
            if (dto == null)
                return null;

            await using var stream = new MemoryStream(dto);
            using var reader = new StreamReader(stream);
            return (OrderSeats)this.serializer.Deserialize(reader);
        }

        private async Task Save(OrderSeats dto)
        {
            await using var writer = new StringWriter();
            this.serializer.Serialize(writer, dto);
            await this.storage.Save(GetSeatAssignmentsBlobId(dto.AssignmentsId), "text/plain", Encoding.UTF8.GetBytes(writer.ToString()));
        }
    }
}
