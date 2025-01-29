using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace XDM.Messaging
{
    public class MessageBus
    {
        private static readonly Lazy<MessageBus> _instance = new(() => new MessageBus());
        private readonly ConcurrentDictionary<string, Func<Message, Task>> _subscribers;

        public static MessageBus Instance => _instance.Value;

        private MessageBus()
        {
            _subscribers = new ConcurrentDictionary<string, Func<Message, Task>>();
        }

        public string Subscribe(Func<Message, Task> handler)
        {
            var id = Guid.NewGuid().ToString();
            _subscribers.TryAdd(id, handler);
            return id;
        }

        public bool Unsubscribe(string subscriptionId)
        {
            return _subscribers.TryRemove(subscriptionId, out _);
        }

        public async Task PublishAsync(Message message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var tasks = new ConcurrentBag<Task>();

            foreach (var subscriber in _subscribers.Values)
            {
                try
                {
                    tasks.Add(subscriber(message));
                }
                catch (Exception)
                {
                    // Log error but continue with other subscribers
                }
            }

            await Task.WhenAll(tasks);
        }

        public void Publish(Message message)
        {
            _ = PublishAsync(message);
        }

        public void Clear()
        {
            _subscribers.Clear();
        }
    }
}
