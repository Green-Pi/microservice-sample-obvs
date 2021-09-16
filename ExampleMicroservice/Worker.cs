using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.CompilerServices;
using Obvs;
using Obvs.ActiveMQ.Configuration;
using Obvs.Configuration;
using Obvs.Serialization.Json.Configuration;
using Obvs.Types;
using Timer = System.Timers.Timer;

namespace ExampleMicroservice
{
    public interface IExampleAppMessage : Obvs.Types.IMessage
    {

    };

    public class DomainObjectChangedEvent : IExampleAppMessage, IEvent
    {
        public string ObjectType { get; set; }
        public string ObjectKey { get; set; }
    }

    // TODO: try out requests and responses too
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ExampleServiceOptions _config;
        private IServiceBus _serviceBus;
        private Timer _timer;
        private Random _rng;

        public Worker(
            ILogger<Worker> logger,
            IOptions<ExampleServiceOptions> config)
        {
            _logger = logger;
            _config = config.Value;

            _logger.LogInformation("config is  {@xxx}", _config);

            _rng = new Random();

            _timer = new Timer()
            {
                Enabled = false,
                AutoReset = true,
                Interval = (1.0 + _rng.NextDouble()) * 1000.0
            };
            _timer.Elapsed += this.SendExampleEvent;
        }

        private async void SendExampleEvent(Object o, ElapsedEventArgs args)
        {
            var eventToPublish = new DomainObjectChangedEvent()
            {
                ObjectType = _config.ServiceName.ToString(),
                ObjectKey = _rng.Next().ToString()
            };

            _logger.LogInformation("Publishing event {@eventToPublish}", eventToPublish);
            try
            {
                await _serviceBus.PublishAsync(eventToPublish);
                _logger.LogInformation("Published event {@eventToPublish}", eventToPublish);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to publish");
            }
            return;
        }

        private void HandleReceivedEvent(DomainObjectChangedEvent e)
        {
            _logger.LogInformation("Received event type {@eventDetail}", e);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceBus = ServiceBus.Configure()
                .WithActiveMQEndpoints<IExampleAppMessage>()
                .Named(_config.ServiceName.ToString())
                .UsingQueueFor<DomainObjectChangedEvent>()
                .ConnectToBroker("tcp://activemq:61616")
                .SerializedAsJson()
                .AsClientAndServer()
                .Create();

            _serviceBus.Events.OfType<DomainObjectChangedEvent>().Subscribe(HandleReceivedEvent);

            _logger.LogInformation("Starting service {serviceName}", _config.ServiceName);
            _timer.Enabled = true;
            return;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Enabled = false;
            _logger.LogInformation("Shutting down");
        }
    }
}