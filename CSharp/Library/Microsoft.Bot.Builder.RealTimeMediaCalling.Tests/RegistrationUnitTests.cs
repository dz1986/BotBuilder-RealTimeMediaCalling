﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Tests
{
    [TestFixture]
    public class RegistrationUnitTests
    {
        private class RealTimeMediaBot : IRealTimeMediaBot
        {
            public IRealTimeMediaBotService RealTimeMediaBotService { get; }

            public RealTimeMediaBot(IRealTimeMediaBotService service)
            {
                RealTimeMediaBotService = service;
            }
        }

        private class RealTimeMediaCall : IRealTimeMediaCall
        {
            public IRealTimeMediaCallService CallService { get; }

            /// <summary>
            /// CorrelationId that needs to be set in the media platform for correlating logs across services
            /// </summary>
            public string CorrelationId { get; }

            /// <summary>
            /// Id generated locally that is unique to each RealTimeMediaCall
            /// </summary>
            public string CallId { get; }

            public RealTimeMediaCall(IRealTimeMediaCallService service)
            {
                CallService = service;
                CallService.OnIncomingCallReceived += OnIncomingCallReceived;
                CallService.OnCallCleanup += OnCallCleanup;

                CorrelationId = service.CorrelationId;
                CallId = $"{service.CorrelationId}:{Guid.NewGuid()}";
            }

            private Task OnIncomingCallReceived(RealTimeMediaIncomingCallEvent realTimeMediaIncomingCallEvent)
            {
                JObject mediaConfiguration;
                using (var writer = new JTokenWriter())
                {
                    writer.WriteRaw("MediaConfiguration");
                    mediaConfiguration = new JObject { { "Token", writer.Token } };
                }

                realTimeMediaIncomingCallEvent.RealTimeMediaWorkflow.Actions = new ActionBase[]
                {
                    new AnswerAppHostedMedia
                    {
                        MediaConfiguration = mediaConfiguration,
                        OperationId = Guid.NewGuid().ToString()
                    }
                };

                realTimeMediaIncomingCallEvent.RealTimeMediaWorkflow.NotificationSubscriptions = new[] { NotificationType.CallStateChange };

                return Task.CompletedTask;
            }

            private Task OnCallCleanup()
            {
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task CreatingBotWithIRealTimeMediaServices()
        {
            var settings = new Mock<IRealTimeMediaCallServiceSettings>();
            settings.Setup(a => a.CallbackUrl).Returns(new Uri("https://someuri/callback"));
            settings.Setup(a => a.NotificationUrl).Returns(new Uri("https://someuri/notification"));

            RealTimeMediaCalling.RegisterRealTimeMediaCallingBot(
                settings.Object,
                a => new RealTimeMediaBot(a),
                a => new RealTimeMediaCall(a));
            var bot = RealTimeMediaCalling.Container.Resolve<IRealTimeMediaBot>();

            Assert.NotNull(bot);
            Assert.NotNull(bot.RealTimeMediaBotService);
            Assert.AreSame(typeof(RealTimeMediaBot), bot.GetType());

            var requestJson = @"
{
  ""id"": ""0b022b87-f255-4667-9335-2335f30ee8de"",
  ""participants"": [
    {
      ""identity"": ""29:1kMGSkuCPgD7ReaC5V2XN08CMOjOcs9MngtbzvvJ8sNU"",
      ""languageId"": ""en-US"",
      ""originator"": true
    },
    {
      ""identity"": ""28:c89e6f90-2b47-4eee-8e3b-22d0b3a6d495"",
      ""originator"": false
    }
  ],
  ""isMultiparty"": false,
  ""presentedModalityTypes"": [
    ""audio""
  ],
  ""callState"": ""incoming""
}";

            var service = bot.RealTimeMediaBotService as IInternalRealTimeMediaBotService;
            var result = await service.ProcessIncomingCallAsync(requestJson, null);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.Null(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            var call1 = service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de") as RealTimeMediaCall;
            Assert.NotNull(call1);
            Assert.IsNotEmpty(call1.CorrelationId);
            Assert.IsNotEmpty(call1.CallId);
            Assert.AreEqual(call1.CorrelationId, call1.CallService.CorrelationId);
            Assert.IsTrue(call1.CallId.StartsWith(call1.CorrelationId));

            result = await service.ProcessIncomingCallAsync(requestJson, Guid.Empty.ToString());
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.Null(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            var call2 = service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de") as RealTimeMediaCall;
            Assert.NotNull(call2);
            Assert.IsNotEmpty(call2.CorrelationId);
            Assert.IsNotEmpty(call2.CallId);
            Assert.AreEqual(call2.CorrelationId, call2.CallService.CorrelationId);
            Assert.IsTrue(call2.CallId.StartsWith(call2.CorrelationId));
            Assert.AreNotEqual(call1, call2);

            requestJson = requestJson.Replace("0b022b87", "0b022b88");

            result = await service.ProcessIncomingCallAsync(requestJson, null);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(2, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.NotNull(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            var call3 = service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de") as RealTimeMediaCall;
            Assert.NotNull(call3);
            Assert.IsNotEmpty(call3.CorrelationId);
            Assert.IsNotEmpty(call3.CallId);
            Assert.AreEqual(call3.CorrelationId, call3.CallService.CorrelationId);
            Assert.IsTrue(call3.CallId.StartsWith(call3.CorrelationId));
            Assert.AreNotEqual(call1, call3);
            Assert.AreNotEqual(call2, call3);

            // TODO: There is no cleanup task, as far as I can tell.
        }
    }
}
