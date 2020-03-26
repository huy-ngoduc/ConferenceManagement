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
 using Microsoft.Extensions.Caching.Memory;

 namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CachingConferenceDao : IConferenceDao
    {
        private readonly IConferenceDao decoratedDao;
        private readonly IMemoryCache  cache;

        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public CachingConferenceDao(Func<ConferenceRegistrationDbContext> contextFactory, IMemoryCache  cache)
        {
            this.decoratedDao = new ConferenceDao(contextFactory);
            this.cache = cache;
        }

        public async Task<ConferenceDetails> GetConferenceDetails(string conferenceCode)
        {
            var key = "ConferenceDao_Details_" + conferenceCode;
            if (this.cache.Get(key) is ConferenceDetails conference) return conference;
            conference  = await this.decoratedDao.GetConferenceDetails(conferenceCode);
            if (conference != null)
            {
                this.cache.Set(key, conference, DateTimeOffset.UtcNow.AddMinutes(10));
            }

            return conference;
        }

        public async Task<ConferenceAlias> GetConferenceAlias(string conferenceCode)
        {
            var key = "ConferenceDao_Alias_" + conferenceCode;
            if (this.cache.Get(key) is ConferenceAlias conference) return conference;
            conference = await this.decoratedDao.GetConferenceAlias(conferenceCode);
            if (conference != null)
            {
                this.cache.Set(key, conference, DateTimeOffset.UtcNow.AddMinutes(20));
            }

            return conference;
        }

        public async Task<IList<ConferenceAlias>> GetPublishedConferences()
        {
            const string key = "ConferenceDao_PublishedConferences";
            if (this.cache.Get(key) is IList<ConferenceAlias> cached) return cached;
            cached = await this.decoratedDao.GetPublishedConferences();
            if (cached != null)
            {
                this.cache.Set(key, cached, DateTimeOffset.UtcNow.AddSeconds(10));
            }

            return cached;
        }
//
//        public async Task<IList<SeatType>> GetPublishedSeatTypes(Guid conferenceId)
//        {
//            var key = "ConferenceDao_PublishedSeatTypes_" + conferenceId;
//            if (this.cache.Get(key) is IList<SeatType> seatTypes) return seatTypes;
//            seatTypes = await this.decoratedDao.GetPublishedSeatTypes(conferenceId);
//            if (seatTypes == null) return null;
//            // determine how long to cache depending on criminality of using stale data.
//            TimeSpan timeToCache;
//            if (seatTypes.All(x => x.AvailableQuantity > 200 || x.AvailableQuantity <= 0))
//            {
//                timeToCache = TimeSpan.FromMinutes(5);
//            }
//            else if (seatTypes.Any(x => x.AvailableQuantity < 30 && x.AvailableQuantity > 0))
//            {
//                // there are just a few seats remaining. Do not cache.
//                timeToCache = TimeSpan.Zero;
//            }
//            else if (seatTypes.Any(x => x.AvailableQuantity < 100 && x.AvailableQuantity > 0))
//            {
//                timeToCache = TimeSpan.FromSeconds(20);
//            }
//            else
//            {
//                timeToCache = TimeSpan.FromMinutes(1);
//            }
//
//            if (timeToCache > TimeSpan.Zero)
//            {
//                this.cache.Set(key, seatTypes, DateTimeOffset.UtcNow.Add(timeToCache));
//            }
//
//            return seatTypes;
//        }
//
//        public async Task<IList<SeatTypeName>> GetSeatTypeNames(IEnumerable<Guid> seatTypes)
//        {
//            return await this.decoratedDao.GetSeatTypeNames(seatTypes);
//        }
    }
}
