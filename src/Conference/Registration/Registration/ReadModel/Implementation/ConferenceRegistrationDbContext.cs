﻿﻿// ==============================================================================================================
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
 using Microsoft.EntityFrameworkCore;
 using Polly;
 using Polly.Contrib.WaitAndRetry;
 using Polly.Retry;

 namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// A repository stored in a database for the views.
    /// </summary>
    public class ConferenceRegistrationDbContext : DbContext
    {
        public const string SchemaName = "ConferenceRegistration";
        private readonly AsyncRetryPolicy retryPolicy;

        public ConferenceRegistrationDbContext(DbContextOptions options)
            : base(options)
        {
            var delay = Backoff.ConstantBackoff(TimeSpan.FromMilliseconds(200), retryCount: 5, fastFirst: true);

            this.retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(delay, (exception, i, span) =>
                {
                    Trace.TraceWarning(
                        $"An error occurred in attempt number {i} to access the database in ConferenceService: {exception.Message}");
                });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Make the name of the views match exactly the name of the corresponding property.
//            modelBuilder.Entity<DraftOrder>().ToTable("OrdersViewV3", SchemaName);
//            modelBuilder.Entity<DraftOrder>()
//                .HasMany(x => x.Lines)
//                .WithOne(x=>x.DraftOrder)
//                .IsRequired();
//            modelBuilder.Entity<DraftOrderItem>().ToTable("OrderItemsViewV3", SchemaName);
//            modelBuilder.Entity<DraftOrderItem>().HasKey(item => new { item.OrderId, item.SeatType });
//            modelBuilder.Entity<PricedOrder>().ToTable("PricedOrdersV3", SchemaName);
//            modelBuilder.Entity<PricedOrder>().HasMany(c => c.Lines).WithOne(x=>x.PricedOrder).IsRequired().HasForeignKey(x => x.OrderId);
//            modelBuilder.Entity<PricedOrderLine>().ToTable("PricedOrderLinesV3", SchemaName);
//            modelBuilder.Entity<PricedOrderLine>().HasKey(seat => new { seat.OrderId, seat.Position });
//            modelBuilder.Entity<PricedOrderLineSeatTypeDescription>().ToTable("PricedOrderLineSeatTypeDescriptionsV3", SchemaName);
//
            modelBuilder.Entity<Conference>().ToTable("ConferencesView", SchemaName);
//            modelBuilder.Entity<Conference>().HasMany(c => c.Seats).WithOne(x=>x.Conference).IsRequired();
//            modelBuilder.Entity<SeatType>().ToTable("ConferenceSeatTypesView", SchemaName);
        }

        public async Task<T> Find<T>(Guid id) where T : class
        {
            return await this.retryPolicy.ExecuteAsync(async () => await this.Set<T>().FindAsync(id));
        }
        
        public async Task Save<T>(T entity) where T : class
        {
            var entry = this.Entry(entity);

            if (entry.State == EntityState.Detached)
                this.Set<T>().Add(entity);

            await this.retryPolicy.ExecuteAsync(async () =>await this.SaveChangesAsync());
        }
    }
}
