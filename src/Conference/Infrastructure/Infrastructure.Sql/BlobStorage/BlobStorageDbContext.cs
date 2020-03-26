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
 using Microsoft.EntityFrameworkCore;

 namespace Infrastructure.Sql.BlobStorage
{
    using System.IO;

    public class BlobStorageDbContext : DbContext
    {
        public const string SchemaName = "BlobStorage";

        public BlobStorageDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public async Task<byte[]> Find(string id)
        {
            var blob = await this.Set<BlobEntity>().FindAsync(id);

            return blob?.Blob;
        }

        public async Task Save(string id, string contentType, byte[] blob)
        {
            var existing = await this.Set<BlobEntity>().FindAsync(id);
            var blobString = "";
            if (contentType == "text/plain")
            {
                await using var stream = new MemoryStream(blob);
                using var reader = new StreamReader(stream);
                blobString = reader.ReadToEnd();
            }

            if (existing != null)
            {
                existing.Blob = blob;
                existing.BlobString = blobString;
            }
            else
            {
                this.Set<BlobEntity>().Add(new BlobEntity(id, contentType, blob, blobString));
            }

            await this.SaveChangesAsync();
        }

        public async Task Delete(string id)
        {
            var blob = await this.Set<BlobEntity>().FindAsync(id);
            if (blob == null)
                return;

            this.Set<BlobEntity>().Remove(blob);

            await this.SaveChangesAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BlobEntity>().ToTable("Blobs", SchemaName);
        }
    }
}
