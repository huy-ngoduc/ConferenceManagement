// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// �2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ConferenceDao : IConferenceDao
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public ConferenceDao(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<ConferenceDetails> GetConferenceDetails(string conferenceCode)
        {
            await using var context = this.contextFactory.Invoke();
            return await context
                .Set<Conference>()
                .Where(dto => dto.Code == conferenceCode)
                .Select(x => new ConferenceDetails
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    Location = x.Location,
                    Tagline = x.Tagline,
                    TwitterSearch = x.TwitterSearch,
                    StartDate = x.StartDate
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ConferenceAlias> GetConferenceAlias(string conferenceCode)
        {
            // TODO: should cache this data.
            await using var context = this.contextFactory.Invoke();
            return await context
                .Set<Conference>()
                .Where(dto => dto.Code == conferenceCode)
                .Select(x => new ConferenceAlias { Id = x.Id, Code = x.Code, Name = x.Name, Tagline = x.Tagline })
                .FirstOrDefaultAsync();
        }

        public async Task<IList<ConferenceAlias>> GetPublishedConferences()
        {
            await using var context = this.contextFactory.Invoke();
            return await context
                .Set<Conference>()
                .Where(dto => dto.IsPublished)
                .Select(x => new ConferenceAlias { Id = x.Id, Code = x.Code, Name = x.Name, Tagline = x.Tagline })
                .ToListAsync();
        }
//
//        public async Task<IList<SeatType>> GetPublishedSeatTypes(Guid conferenceId)
//        {
//            await using var context = this.contextFactory.Invoke();
//            return await context.Set<SeatType>()
//                .Where(c => c.ConferenceId == conferenceId)
//                .ToListAsync();
//        }
//
//        public async Task<IList<SeatTypeName>> GetSeatTypeNames(IEnumerable<Guid> seatTypes)
//        {
//            var distinctIds = seatTypes.Distinct().ToArray();
//            if (distinctIds.Length == 0)
//                return new List<SeatTypeName>();
//
//            await using var context = this.contextFactory.Invoke();
//            return await context.Set<SeatType>()
//                .Where(x => distinctIds.Contains(x.Id))
//                .Select(s => new SeatTypeName { Id = s.Id, Name = s.Name })
//                .ToListAsync();
//        }
    }
}