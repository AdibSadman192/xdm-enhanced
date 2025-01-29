using System;
using System.Threading.Tasks;

namespace XDM.Messaging
{
    public abstract class MessageHandler : IDisposable
    {
        private readonly string _subscriptionId;
        protected readonly MessageBus _messageBus;

        protected MessageHandler()
        {
            _messageBus = MessageBus.Instance;
            _subscriptionId = _messageBus.Subscribe(HandleMessageAsync);
        }

        protected abstract Task HandleMessageAsync(Message message);

        public void Dispose()
        {
            _messageBus.Unsubscribe(_subscriptionId);
            GC.SuppressFinalize(this);
        }
    }
}
