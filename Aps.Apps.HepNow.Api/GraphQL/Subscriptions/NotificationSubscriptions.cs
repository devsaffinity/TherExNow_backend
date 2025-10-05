using HotChocolate.Execution;
using HotChocolate.Subscriptions;

namespace Aps.Apps.HepNow.Api.GraphQL.Subscriptions
{
    [ExtendObjectType(typeof(Subscription))]
    public class NotificationSubscriptions
    {
        public ValueTask<ISourceStream<Notification>> SubscribeToNotificationAdded(int userId, [Service] ITopicEventReceiver receiver)
        {
            string topic = $"{userId}_NotificationAdded";
            var stream = receiver.SubscribeAsync<string, Notification>(topic);
            return stream;
        }

        [Subscribe(With = nameof(SubscribeToNotificationAdded))]
        public Notification NotificationAdded(int userId, [EventMessage] Notification notification) => notification; 
    }
}
