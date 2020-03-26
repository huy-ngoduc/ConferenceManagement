using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Conference;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Registration.Handlers;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace CommandProcessorWorker
{
    public class Program
    {
        public static AppConfig AppConfig { get; set; }
        
        public static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));
                    
                    services.AddDbContext<ConferenceContext>(options => options.UseSqlServer(hostContext.Configuration.GetConnectionString("ConferenceContext")));
//                    services.AddDbContext<PaymentsDbContext>(options => options.UseSqlServer(hostContext.Configuration.GetConnectionString("PaymentsDbContext")));
//                    services.AddDbContext<PaymentsReadDbContext>(options => options.UseSqlServer(hostContext.Configuration.GetConnectionString("PaymentsReadDbContext")));
                    services.AddDbContext<ConferenceRegistrationDbContext>(options => options.UseSqlServer(hostContext.Configuration.GetConnectionString("ConferenceRegistrationDbContext")));
//                    services.AddDbContext<BlobStorageDbContext>(options => options.UseSqlServer(hostContext.Configuration.GetConnectionString("BlobStorageDbContext")));
//                    services.AddDbContext<EventStoreDbContext>(options => options.UseSqlServer(hostContext.Configuration.GetConnectionString("EventStoreDbContext")));
                    
                    services.AddMassTransit(cfg =>
                    {
//                        cfg.AddConsumer<OrderEventHandler>();
                        cfg.AddConsumer<ConferenceViewModelGenerator>();
//                        cfg.AddConsumer<DraftOrderViewModelGenerator>();
//                        cfg.AddConsumer<OrderCommandHandler>();
//                        cfg.AddConsumer<PricedOrderViewModelGenerator>();
//                        cfg.AddConsumer<SeatAssignmentsHandler>();
//                        cfg.AddConsumer<SeatAssignmentsViewModelGenerator>();
//                        cfg.AddConsumer<SeatsAvailabilityHandler>();
                        
                        cfg.AddBus(ConfigureBus);
//                        cfg.AddSagaStateMachine<OrderStateMachine, OrderState>()
//                            .EntityFrameworkRepository(r =>
//                            {
//                                r.ConcurrencyMode = ConcurrencyMode.Optimistic; // or use Optimistic, which requires RowVersion
//
//                                r.AddDbContext<DbContext, OrderStateDbContext>((provider, b) =>
//                                {
//                                    b.UseSqlServer(hostContext.Configuration.GetConnectionString("OrderStateMachineDbContext"), m =>
//                                    {
//                                        m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
//                                        m.MigrationsHistoryTable($"__{nameof(OrderStateDbContext)}");
//                                    });
//                                });
//                            });
                    });
                    
                    services.AddHostedService<BusService>();
                    
//                    services.AddScoped<DbContext, PaymentsDbContext>();
//                    services.AddSingleton<IBlobStorage, SqlBlobStorage>();
//                    services.AddSingleton<IDataContext<ThirdPartyProcessorPayment>, SqlDataContext<ThirdPartyProcessorPayment>>();
                    services.AddSingleton<IConferenceDao, ConferenceDao>();
//                    services.AddSingleton<IOrderDao, OrderDao>();
//                    services.AddSingleton(typeof(IEventSourcedRepository<>), typeof(SqlEventSourcedRepository<>));
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            if (isService)
            {
                await builder.UseWindowsService().Build().RunAsync();
            }
            else
            {
                await builder.RunConsoleAsync();
            }
        }

        static IBusControl ConfigureBus(IServiceProvider provider)
        {
            AppConfig = provider.GetRequiredService<IOptions<AppConfig>>().Value;
            
            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(AppConfig.Host, AppConfig.VirtualHost, h =>
                {
                    h.Username(AppConfig.Username);
                    h.Password(AppConfig.Password);
                });
//                
//                cfg.ReceiveEndpoint("add_seat_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<SeatsAvailabilityHandler>(provider);
//                    
//                    EndpointConvention.Map<AddSeats>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("assign_registration_details_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<OrderCommandHandler>(provider);
//
//                    EndpointConvention.Map<AssignRegistrantDetails>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("assign_seat_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<SeatAssignmentsHandler>(provider);
//
//                    EndpointConvention.Map<AssignSeat>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("cancel_seat_reservation_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<SeatsAvailabilityHandler>(provider);
//
//                    EndpointConvention.Map<CancelSeatReservation>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("commit_seat_reservation_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<SeatsAvailabilityHandler>(provider);
//
//                    EndpointConvention.Map<CommitSeatReservation>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("confirm_order_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<OrderCommandHandler>(provider);
//
//                    EndpointConvention.Map<ConfirmOrder>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("make_seat_reservation_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<SeatsAvailabilityHandler>(provider);
//
//                    EndpointConvention.Map<MakeSeatReservation>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("mark_seats_as_reserved_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<OrderCommandHandler>(provider);
//
//                    EndpointConvention.Map<MarkSeatsAsReserved>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("register_to_conference_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<OrderCommandHandler>(provider);
//
//                    EndpointConvention.Map<RegisterToConference>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("reject_order_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<OrderCommandHandler>(provider);
//
//                    EndpointConvention.Map<RejectOrder>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("remove_seats_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<SeatsAvailabilityHandler>(provider);
//
//                    EndpointConvention.Map<RemoveSeats>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint("unassign_seat_queue", e =>
//                {
//                    e.PrefetchCount = 16;
//                    e.UseMessageRetry(x => x.Interval(2, 100));
//
//                    e.ConfigureConsumer<SeatsAvailabilityHandler>(provider);
//
//                    EndpointConvention.Map<UnassignSeat>(e.InputAddress);
//                });
//                
//                cfg.ReceiveEndpoint(host, "shopping_cart_state", e =>
//                {
//                    e.PrefetchCount = 8;
//                    e.StateMachineSaga(_machine, _repository.Value);
//                });
//
//                cfg.ReceiveEndpoint(host, ConfigurationManager.AppSettings["SchedulerQueueName"], e =>
//                {
//                    // For MT4.0, prefetch must be set for Quartz prior to anything else
//                    e.PrefetchCount = 1;
//                    cfg.UseMessageScheduler(e.InputAddress);                   
//
//                    e.Consumer(() => new ScheduleMessageConsumer(_scheduler));
//                    e.Consumer(() => new CancelScheduledMessageConsumer(_scheduler));
//                });
                
                cfg.ConfigureEndpoints(provider);
            });
        }
    }
}