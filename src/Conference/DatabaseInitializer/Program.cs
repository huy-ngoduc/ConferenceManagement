using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DatabaseInitializer
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = Configuration.AppSettings["defaultConnection"];
            if (args.Length > 0)
            {
                connectionString = args[0];
            }

            // Use ConferenceContext as entry point for dropping and recreating DB
            using (var context = new ConferenceContext(connectionString))
            {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            Database.SetInitializer<EventStoreDbContext>(null);
            Database.SetInitializer<MessageLogDbContext>(null);
            Database.SetInitializer<BlobStorageDbContext>(null);
            Database.SetInitializer<ConferenceRegistrationDbContext>(null);
            Database.SetInitializer<RegistrationProcessManagerDbContext>(null);
            Database.SetInitializer<PaymentsDbContext>(null);

            DbContext[] contexts =
                new DbContext[] 
                { 
                    new EventStoreDbContext(connectionString),
                    new MessageLogDbContext(connectionString),
                    new BlobStorageDbContext(connectionString),
                    new PaymentsDbContext(connectionString),
                    new RegistrationProcessManagerDbContext(connectionString),
                    new ConferenceRegistrationDbContext(connectionString),
                };

            foreach (DbContext context in contexts)
            {
                var adapter = (IObjectContextAdapter)context;

                var script = adapter.ObjectContext.CreateDatabaseScript();

                context.Database.ExecuteSqlCommand(script);

                context.Dispose();
            }

            using (var context = new ConferenceRegistrationDbContext(connectionString))
            {
                ConferenceRegistrationDbContextInitializer.CreateIndexes(context);
            }

            using (var context = new RegistrationProcessManagerDbContext(connectionString))
            {
                RegistrationProcessManagerDbContextInitializer.CreateIndexes(context);
            }

            using (var context = new PaymentsDbContext(connectionString))
            {
                PaymentsReadDbContextInitializer.CreateViews(context);
            }

            MessagingDbInitializer.CreateDatabaseObjects(connectionString, "SqlBus");
        }
        }
    }
}