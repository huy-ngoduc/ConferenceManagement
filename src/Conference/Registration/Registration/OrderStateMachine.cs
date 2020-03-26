using System;
using System.Linq;
using Automatonymous;
using Payments.Contracts.Events;
using Registration.Commands;
using Registration.Events;

namespace Registration
{
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public OrderStateMachine()
        {
            InstanceState(x => x.CurrentState);
            
            Event(() => OrderPlaced, x => x.CorrelateBy(order => order.OrderId, context => context.Message.SourceId)
                .SelectId(context => context.Message.SourceId));

            Event(() => OrderUpdated, x => x.CorrelateById(context => context.Message.SourceId));
            Event(() => SeatsReserved, x => x.CorrelateById(context => context.Message.SourceId));
            Event(() => PaymentCompleted, x => x.CorrelateById(context => context.Message.SourceId));
            Event(() => OrderConfirmed, x => x.CorrelateById(context => context.Message.SourceId));

            Schedule(() => OrderExpired, x => x.ExpirationId, x =>
            {
                x.Delay = TimeSpan.FromMinutes(16);
                x.Received = e => e.CorrelateById(context => context.Message.SourceId);
            });

            Initially(
                When(OrderPlaced)
                    .Then(context =>
                    {
                        context.Instance.ConferenceId = context.Data.ConferenceId;
                        context.Instance.OrderId = context.Data.SourceId;
                        context.Instance.ReservationId = context.Data.SourceId;
                        context.Instance.ReservationAutoExpiration = context.Data.ReservationAutoExpiration;
                    })
                    .Publish(context => new MakeSeatReservation
                    {
                        ConferenceId = context.Data.ConferenceId,
                        ReservationId = context.Data.SourceId,
                        Seats = context.Data.Seats.ToList()
                    })
                    .Schedule(OrderExpired, context => new OrderExpiredEvent(context.Instance))
                    .TransitionTo(AwaitingReservationConfirmation)
                );

            During(AwaitingReservationConfirmation,
                When(OrderUpdated)
                    .Publish(context =>
                    
                        new MakeSeatReservation
                            {
                                ConferenceId = context.Instance.ConferenceId,
                                ReservationId = context.Instance.ReservationId,
                                Seats = context.Data.Seats.ToList()
                            }
                    ),
                When(SeatsReserved)
                    .Publish(context => new MarkSeatsAsReserved
                    {
                        OrderId = context.Instance.OrderId ?? Guid.NewGuid(),
                        Seats = context.Data.ReservationDetails.ToList(),
                        Expiration = context.Instance.ReservationAutoExpiration,
                    })
                    .TransitionTo(ReservationConfirmationReceived),
                When(OrderExpired.Received)
                    .Publish(context => new RejectOrder(){OrderId = context.Data.SourceId})
                    .Publish(context => new CancelSeatReservation(){ConferenceId = context.Instance.ConferenceId, ReservationId = context.Instance.ReservationId})
                    .Finalize()
                );
            During(ReservationConfirmationReceived,
                When(OrderConfirmed)
                    .Publish(context =>
                        new CommitSeatReservation()
                        {
                            ConferenceId = context.Instance.ConferenceId,
                            ReservationId = context.Instance.ReservationId
                        }
                    )
                    .Finalize(),
                When(PaymentCompleted)
                    .Publish(context => new ConfirmOrder
                    {
                        OrderId = context.Instance.OrderId ?? Guid.NewGuid()
                    })
                    .TransitionTo(PaymentConfirmationReceived),
                When(OrderExpired.Received)
                    .Publish(context => new RejectOrder(){OrderId = context.Data.SourceId})
                    .Publish(context => new CancelSeatReservation(){ConferenceId = context.Instance.ConferenceId, ReservationId = context.Instance.ReservationId})
                    .Finalize()
            );
            During(PaymentConfirmationReceived,
                When(OrderConfirmed)
                    .Publish(context =>
                        new CommitSeatReservation()
                        {
                            ConferenceId = context.Instance.ConferenceId,
                            ReservationId = context.Instance.ReservationId
                        }
                    )
                    .Finalize(),
                When(PaymentCompleted)
                    .Publish(context => new ConfirmOrder
                    {
                        OrderId = context.Instance.OrderId ?? Guid.NewGuid()
                    })
                    .TransitionTo(PaymentConfirmationReceived),
                When(OrderExpired.Received)
                    .Publish(context => new RejectOrder(){OrderId = context.Data.SourceId})
                    .Publish(context => new CancelSeatReservation(){ConferenceId = context.Instance.ConferenceId, ReservationId = context.Instance.ReservationId})
                    .Finalize()
            );
            SetCompletedWhenFinalized();
        }

        public Event<OrderPlaced> OrderPlaced { get; private set; }
        public Event<OrderUpdated> OrderUpdated { get; private set; }
        public Event<SeatsReserved> SeatsReserved { get; private set; }
        public Event<PaymentCompleted> PaymentCompleted { get; private set; }
        public Event<OrderConfirmed> OrderConfirmed { get; private set; }
        public Schedule<OrderState, OrderExpired> OrderExpired { get; private set; }
        public State AwaitingReservationConfirmation { get; private set; }
        public State ReservationConfirmationReceived { get; private set; }
        public State PaymentConfirmationReceived { get; private  set; }
    }
    
    class OrderExpiredEvent :
        OrderExpired
    {
        readonly OrderState _instance;

        public OrderExpiredEvent(OrderState instance)
        {
            _instance = instance;
        }

        public Guid OrderId => _instance.CorrelationId;
    }
}