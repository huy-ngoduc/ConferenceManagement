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
    using System.IO;
    using System.Linq;
    using System.Text;
    using Infrastructure.BlobStorage;
    using Infrastructure.Serialization;
    using Registration.Handlers;

    public class OrderDao : IOrderDao
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;
        private IBlobStorage blobStorage;
        private ITextSerializer serializer;

        public OrderDao(Func<ConferenceRegistrationDbContext> contextFactory, IBlobStorage blobStorage, ITextSerializer serializer)
        {
            this.contextFactory = contextFactory;
            this.blobStorage = blobStorage;
            this.serializer = serializer;
        }

        public async Task<Guid?> LocateOrder(string email, string accessCode)
        {
            await using var context = this.contextFactory.Invoke();
            var orderProjection = await context
                .Set<DraftOrder>()
                .Where(o => o.RegistrantEmail == email && o.AccessCode == accessCode)
                .Select(o => new { o.OrderId })
                .FirstOrDefaultAsync();

            return orderProjection?.OrderId;
        }

        public async Task<DraftOrder> FindDraftOrder(Guid orderId)
        {
            await using var context = this.contextFactory.Invoke();
            return await context.Set<DraftOrder>().Include(x => x.Lines).FirstOrDefaultAsync(dto => dto.OrderId == orderId);
        }

        public async Task<PricedOrder> FindPricedOrder(Guid orderId)
        {
            await using var context = this.contextFactory.Invoke();
            return await context.Set<PricedOrder>().Include(x => x.Lines).FirstOrDefaultAsync(dto => dto.OrderId == orderId);
        }

        public async Task<OrderSeats> FindOrderSeats(Guid assignmentsId)
        {
            return await FindBlob<OrderSeats>(SeatAssignmentsViewModelGenerator.GetSeatAssignmentsBlobId(assignmentsId));
        }

        private async Task<T> FindBlob<T>(string id)
            where T : class
        {
            var dto = await this.blobStorage.Find(id);
            if (dto == null)
            {
                return null;
            }

            await using var stream = new MemoryStream(dto);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return (T)this.serializer.Deserialize(reader);
        }
    }
}