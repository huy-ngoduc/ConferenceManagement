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
    using System;
    using System.Diagnostics;
    using Conference;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    // TODO: should work correctly with out of order messages instead of dropping events!
    public class ConferenceViewModelGenerator :
        IConsumer<ConferenceCreated>,
        IConsumer<ConferenceUpdated>,
        IConsumer<ConferencePublished>,
        IConsumer<ConferenceUnpublished>
//        ,
//        IConsumer<SeatCreated>,
//        IConsumer<SeatUpdated>,
//        IConsumer<AvailableSeatsChanged>,
//        IConsumer<SeatsReserved>,
//        IConsumer<SeatsReservationCancelled>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public ConferenceViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task Consume(ConsumeContext<ConferenceCreated>  consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            repository.Set<Conference>().Add(new Conference(
                consumeContext.Message.SourceId,
                consumeContext.Message.Slug,
                consumeContext.Message.Name,
                consumeContext.Message.Description,
                consumeContext.Message.Location,
                consumeContext.Message.Tagline,
                consumeContext.Message.TwitterSearch,
                consumeContext.Message.StartDate));

            await repository.SaveChangesAsync();
        }

        public async Task Consume(ConsumeContext<ConferenceUpdated> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var confDto = await repository.Find<Conference>(consumeContext.Message.SourceId);
            if (confDto != null)
            {
                confDto.Code = consumeContext.Message.Slug;
                confDto.Description = consumeContext.Message.Description;
                confDto.Location = consumeContext.Message.Location;
                confDto.Name = consumeContext.Message.Name;
                confDto.StartDate = consumeContext.Message.StartDate;
                confDto.Tagline = consumeContext.Message.Tagline;
                confDto.TwitterSearch = consumeContext.Message.TwitterSearch;

                await repository.SaveChangesAsync();
            }
            else
            {
                Trace.TraceError("Failed to locate Conference read model for updated conference with id {0}.", consumeContext.Message.SourceId);
            }
        }

        public async Task Consume(ConsumeContext<ConferencePublished> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Find<Conference>(consumeContext.Message.SourceId);
            if (dto != null)
            {
                dto.IsPublished = true;

                await repository.Save(dto);
            }
            else
            {
                Trace.TraceError("Failed to locate Conference read model for published conference with id {0}.", consumeContext.Message.SourceId);
            }
        }

        public async Task Consume(ConsumeContext<ConferenceUnpublished> consumeContext)
        {
            await using var repository = this.contextFactory.Invoke();
            var dto = await repository.Find<Conference>(consumeContext.Message.SourceId);
            if (dto != null)
            {
                dto.IsPublished = false;

                await repository.Save(dto);
            }
            else
            {
                Trace.TraceError("Failed to locate Conference read model for unpublished conference with id {0}.", consumeContext.Message.SourceId);
            }
        }
//
//        public async Task Consume(ConsumeContext<SeatCreated> consumeContext)
//        {
//            await using var repository = this.contextFactory.Invoke();
//            // NOTE: Ideally this should trust the sender, and create the seat type even if the ConferenceCreated event was not
//            // yet handled (so the reference to Conference does not yet exist).
//            var dto = await repository.Find<Conference>(consumeContext.Message.ConferenceId);
//            if (dto != null)
//            {
//                dto.Seats.Add(new SeatType(consumeContext.Message.SourceId, consumeContext.Message.ConferenceId, consumeContext.Message.Name, consumeContext.Message.Description, consumeContext.Message.Price, consumeContext.Message.Quantity));
//
//                await consumeContext.Send(new AddSeats
//                {
//                    ConferenceId = consumeContext.Message.ConferenceId,
//                    SeatType = consumeContext.Message.SourceId,
//                    Quantity = consumeContext.Message.Quantity
//                });
//                
//                await repository.Save(dto);
//            }
//            else
//            {
//                Trace.TraceError("Failed to locate Conference read model for created seat, with conference id {0}.", consumeContext.Message.ConferenceId);
//            }
//        }
//
//        public async Task Consume(ConsumeContext<SeatUpdated> consumeContext)
//        {
//            await using var repository = this.contextFactory.Invoke();
//            var dto = await repository.Set<Conference>().Include(x => x.Seats).FirstOrDefaultAsync(x => x.Id == consumeContext.Message.ConferenceId);
//            if (dto != null)
//            {
//                var seat = dto.Seats.FirstOrDefault(x => x.Id == consumeContext.Message.SourceId);
//                if (seat != null)
//                {
//                    seat.Description = consumeContext.Message.Description;
//                    seat.Name = consumeContext.Message.Name;
//                    seat.Price = consumeContext.Message.Price;
//
//                    // Calculate diff to drive the seat availability.
//                    // Is this appropriate to have it here?
//                    var diff = consumeContext.Message.Quantity - seat.Quantity;
//
//                    seat.Quantity = consumeContext.Message.Quantity;
//
//                    await repository.Save(dto);
//
//                    if (diff > 0)
//                    {
//                        await consumeContext.Send(new AddSeats
//                        {
//                            ConferenceId = consumeContext.Message.ConferenceId,
//                            SeatType = consumeContext.Message.SourceId,
//                            Quantity = diff,
//                        });
//                    }
//                    else
//                    {
//                        await consumeContext.Send(new RemoveSeats
//                        {
//                            ConferenceId = consumeContext.Message.ConferenceId,
//                            SeatType = consumeContext.Message.SourceId,
//                            Quantity = Math.Abs(diff),
//                        });
//                    }
//                }
//                else
//                {
//                    Trace.TraceError("Failed to locate Seat Type read model being updated with id {0}.", consumeContext.Message.SourceId);
//                }
//            }
//            else
//            {
//                Trace.TraceError("Failed to locate Conference read model for updated seat type, with conference id {0}.", consumeContext.Message.ConferenceId);
//            }
//        }
//
//        public async Task Consume(ConsumeContext<AvailableSeatsChanged> consumeContext)
//        {
//            await this.UpdateAvailableQuantity(consumeContext.Message, consumeContext.Message.Seats);
//        }
//
//        public async Task Consume(ConsumeContext<SeatsReserved> consumeContext)
//        {
//            await this.UpdateAvailableQuantity(consumeContext.Message, consumeContext.Message.AvailableSeatsChanged);
//        }
//
//        public async Task Consume(ConsumeContext<SeatsReservationCancelled> consumeContext)
//        {
//            await this.UpdateAvailableQuantity(consumeContext.Message, consumeContext.Message.AvailableSeatsChanged);
//        }

//        private async Task UpdateAvailableQuantity(IVersionedEvent @event, IEnumerable<SeatQuantity> seats)
//        {
//            await using var repository = this.contextFactory.Invoke();
//            var dto = await repository.Set<Conference>().Include(x => x.Seats).FirstOrDefaultAsync(x => x.Id == @event.SourceId);
//            if (dto == null)
//            {
//                Trace.TraceError(
//                    "Failed to locate Conference read model for updated seat type, with conference id {0}.",
//                    @event.SourceId);
//            }
//            else
//            {
//                // This check assumes events might be received more than once, but not out of order
//                if (@event.Version > dto.SeatsAvailabilityVersion)
//                {
//                    foreach (var seat in seats)
//                    {
//                        var seatDto = dto.Seats.FirstOrDefault(x => x.Id == seat.SeatType);
//                        if (seatDto != null)
//                        {
//                            seatDto.AvailableQuantity += seat.Quantity;
//                        }
//                        else
//                        {
//                            // TODO should reject the entire update?
//                            Trace.TraceError("Failed to locate Seat Type read model being updated with id {0}.",
//                                seat.SeatType);
//                        }
//                    }
//
//                    dto.SeatsAvailabilityVersion = @event.Version;
//
//                    await repository.Save(dto);
//                }
//                else
//                {
//                    Trace.TraceWarning(
//                        "Ignoring availability update message with version {1} for conference id {0}, last known version {2}.",
//                        @event.SourceId,
//                        @event.Version,
//                        dto.SeatsAvailabilityVersion);
//                }
//            }
//        }
//        
    }
}
