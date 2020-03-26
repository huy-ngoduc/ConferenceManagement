using System;
using Automatonymous;

namespace Registration
{
    public class OrderState : SagaStateMachineInstance
    {
        public string CurrentState { get; set; }

        /// <summary>
        /// The expiration tag for the shopping cart, which is scheduled whenever
        /// the cart is updated
        /// </summary>
        public Guid? ExpirationId { get; set; }

        public Guid? OrderId { get; set; }

        public Guid CorrelationId { get; set; }
        public Guid ReservationId { get; set; }
        public Guid ConferenceId { get; set; }
//        public Guid SeatReservationCommandId { get; set; }
        public DateTime ReservationAutoExpiration { get; set; }
        
        // If using Optimistic concurrency, this property is required
        public byte[] RowVersion { get; set; }
    }
}