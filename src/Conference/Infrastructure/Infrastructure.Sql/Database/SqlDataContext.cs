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
 using Infrastructure.Database;
 using Infrastructure.Messaging;
 using MassTransit;
 using Microsoft.EntityFrameworkCore;

 namespace Infrastructure.Sql.Database
{
    using System;

    public class SqlDataContext<T> : IDataContext<T> where T : class, IAggregateRoot
    {
        private readonly IBusControl bus;
        private readonly DbContext context;

        public SqlDataContext(Func<DbContext> contextFactory, IBusControl bus)
        {
            this.bus = bus;
            this.context = contextFactory.Invoke();
        }

        public async Task<T> Find(Guid id)
        {
            return await this.context.Set<T>().FindAsync(id);
        }

        public async Task Save(T aggregateRoot)
        {
            var entry = this.context.Entry(aggregateRoot);

            if (entry.State == EntityState.Detached)
                this.context.Set<T>().Add(aggregateRoot);

            // Can't have transactions across storage and message bus.
            await context.SaveChangesAsync();

            if (aggregateRoot is IEventPublisher eventPublisher)
                await bus.Publish(eventPublisher.Events);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqlDataContext()
        {
            this.Dispose(false);
        }

        public async ValueTask DisposeAsync()
        {
            Dispose(false);
            await Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.context.Dispose();
            }
        }
    }
}
