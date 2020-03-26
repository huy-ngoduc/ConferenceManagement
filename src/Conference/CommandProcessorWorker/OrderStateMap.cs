using MassTransit.EntityFrameworkCoreIntegration.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registration;

namespace WorkerRoleCommandProcessor
{
    public class OrderStateMap : SagaClassMap<OrderState>
    {
        protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
        {
            entity.Property(x => x.ConferenceId);
            entity.Property(x => x.CurrentState).HasMaxLength(100);
            entity.Property(x => x.ExpirationId);
            entity.Property(x => x.OrderId);
            entity.Property(x => x.ReservationId);
            entity.Property(x => x.ReservationAutoExpiration);
            entity.Property(x => x.RowVersion).IsRowVersion();
        }
    }
}