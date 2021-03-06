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

using Microsoft.EntityFrameworkCore;

namespace Conference
{
    /// <summary>
    /// Data context for this ORM-based domain.
    /// </summary>
    public class ConferenceContext : DbContext
    {
        public const string SchemaName = "ConferenceManagement";

        public ConferenceContext(DbContextOptions contextOptions)
            : base(contextOptions)
        {
        }
        
        public virtual DbSet<ConferenceInfo> Conferences { get; set; }
//        public virtual DbSet<SeatType> Seats { get; set; }
//        public virtual DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // NOTE: ToTable used for all entities so that we can prepend 
            // the schema name. This allows all pieces of the application 
            // to be deployed to a single SQL Azure database, yet avoid 
            // table name collisions, while reducing the deployment costs.

            modelBuilder.Entity<ConferenceInfo>().ToTable("Conferences", SchemaName);
//            // Make seat infos required to have a conference info associated, but without 
//            // having to add a navigation property (don't polute the object model).
//            modelBuilder.Entity<ConferenceInfo>()
//                .HasMany(x => x.Seats)
//                .WithOne(x=>x.ConferenceInfo)
//                .IsRequired();
//            modelBuilder.Entity<SeatType>().ToTable("SeatTypes", SchemaName);
//            modelBuilder.Entity<Order>().ToTable("Orders", SchemaName);
//            modelBuilder.Entity<OrderSeat>().ToTable("OrderSeats", SchemaName);
//            modelBuilder.Entity<OrderSeat>(entity => { entity.OwnsOne(e => e.Attendee); });
//            modelBuilder.Entity<OrderSeat>().HasKey(seat => new {seat.OrderId, seat.Position});
        }
    }
}