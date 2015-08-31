namespace DuplexPipe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal sealed class LocalBus : IBus
    {
        private readonly Dictionary<Type, HashSet<Event>> callbacks =
            new Dictionary<Type, HashSet<Event>>();

        public void Publish(object payload)
        {
            Type payloadType = payload.GetType();

            HashSet<Event> items;
            if (callbacks.TryGetValue(payloadType, out items))
            {
                foreach (Event @event in items.ToList())
                {
                    if (!@event.Execute(payload))
                    {
                        items.Remove(@event);
                    }
                }
            }
        }

        private void RegisterEventHandler<T>(Action<T> callback, bool permanent)
        {
            Type payloadType = typeof(T);
            HashSet<Event> items;
            if (!callbacks.TryGetValue(payloadType, out items))
            {
                items = new HashSet<Event>();
                callbacks[payloadType] = items;
            }

            items.Add(new Event(callback, permanent));
        }

        public void Always<T>(Action<T> callback)
        {
            RegisterEventHandler(callback, permanent: true);
        }

        public void Once<T>(Action<T> callback)
        {
            RegisterEventHandler(callback, permanent: false);
        }

        public void Forget<T>()
        {
            HashSet<Event> items;
            if (callbacks.TryGetValue(typeof(T), out items))
            {
                items.Clear();
            }
        }

        public void ForgetAll()
        {
            callbacks.Clear();
        }

        private sealed class Event : IEquatable<Event>
        {
            private readonly bool permanent;

            private readonly Delegate callback;

            public Event(Delegate callback, bool permanent)
            {
                if (callback == null) throw new ArgumentNullException(nameof(callback));

                this.permanent = permanent;
                this.callback = callback;
            }

            public bool Execute<T>(T payload)
            {
                callback.DynamicInvoke(payload);
                return permanent;
            }

            public bool Equals(Event other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return permanent == other.permanent && Equals(callback, other.callback);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Event && Equals((Event) obj);
            }

            public override int GetHashCode()
            {
                const int prime = 397;
                unchecked
                {
                    return (permanent.GetHashCode() * prime) ^ callback.GetHashCode();
                }
            }

            public static bool operator ==(Event left, Event right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Event left, Event right)
            {
                return !Equals(left, right);
            }
        }
    }
}