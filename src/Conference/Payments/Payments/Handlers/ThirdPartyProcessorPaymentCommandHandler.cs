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
 using MassTransit;

 namespace Payments.Handlers
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Infrastructure.Database;
    using Payments.Contracts.Commands;

    public class ThirdPartyProcessorPaymentCommandHandler :
        IConsumer<InitiateThirdPartyProcessorPayment>,
        IConsumer<CompleteThirdPartyProcessorPayment>,
        IConsumer<CancelThirdPartyProcessorPayment>
    {
        private readonly Func<IDataContext<ThirdPartyProcessorPayment>> contextFactory;

        public ThirdPartyProcessorPaymentCommandHandler(Func<IDataContext<ThirdPartyProcessorPayment>> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task Consume(ConsumeContext<InitiateThirdPartyProcessorPayment> consumeContext)
        {
            await using var repository = this.contextFactory();
            var items = consumeContext.Message.Items.Select(t => new ThidPartyProcessorPaymentItem(t.Description, t.Amount)).ToList();
            var payment = new ThirdPartyProcessorPayment(consumeContext.Message.PaymentId, consumeContext.Message.PaymentSourceId, consumeContext.Message.Description
                , consumeContext.Message.TotalAmount, items);

            await repository.Save(payment);
        }

        public async Task Consume(ConsumeContext<CompleteThirdPartyProcessorPayment> command)
        {
            await using var repository = this.contextFactory();
            var payment = await repository.Find(command.Message.PaymentId);

            if (payment != null)
            {
                payment.Complete();
                await repository.Save(payment);
            }
            else
            {
                Trace.TraceError("Failed to locate the payment entity with id {0} for the completed third party payment.", command.Message.PaymentId);
            }
        }

        public async Task Consume(ConsumeContext<CancelThirdPartyProcessorPayment> command)
        {
            await using var repository = this.contextFactory();
            var payment = await repository.Find(command.Message.PaymentId);

            if (payment != null)
            {
                payment.Cancel();
                await repository.Save(payment);
            }
            else
            {
                Trace.TraceError("Failed to locate the payment entity with id {0} for the completed third party payment.", command.Message.PaymentId);
            }
        }
    }
}
