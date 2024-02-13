namespace webapi.Services.BusService
{
    public class BusEvents : IBusEvent
    {
        private readonly List<Action<BusEventMessage>> _subscribers = new List<Action<BusEventMessage>>();

        public void Subscribe(Action<BusEventMessage> subscriber)
        {
            _subscribers.Add(subscriber);
        }

        public void Unsubscribe(Action<BusEventMessage> subscriber)
        {
            _subscribers.Remove(subscriber);
        }

        public void Publish(BusEventMessage eventMessage)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber(eventMessage);
            }
        }

    }
}

   /* namespace webapi.Services.BusService
    {

        public class BusEvents : IBusEvent
        {
            private readonly List<Tuple<string, List<Action<BusEventMessage>>>> _subscribers = new List<Tuple<string, List<Action<BusEventMessage>>>>();

            public void Subscribe(Action<BusEventMessage> subscriber, string idT)
            {
                if (_subscribers.Any(x => x.Item1 == idT))
                {
                    Tuple<string, List<Action<BusEventMessage>>> tup = _subscribers.Single(x => x.Item1 == idT);
                    tup.Item2.Add(subscriber);

                }
                else
                {
                    Tuple<string, List<Action<BusEventMessage>>> tup = new(idT, new List<Action<BusEventMessage>>());
                    tup.Item2.Add(subscriber);

                    _subscribers.Add(tup);
                }

            }

            public void Unsubscribe(Action<BusEventMessage> subscriber, string idT)
            {
                if (_subscribers.Any(x => x.Item1 == idT))
                {
                    Tuple<string, List<Action<BusEventMessage>>> tup = _subscribers.Single(x => x.Item1 == idT);
                    tup.Item2.Remove(subscriber);

                    if (tup.Item2.Count == 0)
                    {
                        _subscribers.Remove(tup);
                    }

                }
                else
                {
                    throw new ArgumentException("id is not available element must not be part of any list. implemennt");
                }

            }

            public void Publish(BusEventMessage eventMessage, string idT)
            {
                if (_subscribers.Any(x => x.Item1 == idT))
                {
                    Tuple<string, List<Action<BusEventMessage>>> tup = _subscribers.Single(x => x.Item1 == idT);

                    foreach (var sub in tup.Item2)
                    {
                        sub(eventMessage);
                    }

                }
                else
                {
                    throw new ArgumentException("publish event on an empty subscriber list check and implement");
                }

            }
        }
    }*/
            /*void Publish(IntegrationEvent @event);

            void Subscribe<T, TH>()
                where T : IntegrationEvent
                where TH : IIntegrationEventHandler<T>;

            void SubscribeDynamic<TH>(string eventName)
                where TH : IDynamicIntegrationEventHandler;

            void UnsubscribeDynamic<TH>(string eventName)
                where TH : IDynamicIntegrationEventHandler;

            void Unsubscribe<T, TH>()
                where TH : IIntegrationEventHandler<T>
                where T : IntegrationEvent;*//*
        }
    }
*/

