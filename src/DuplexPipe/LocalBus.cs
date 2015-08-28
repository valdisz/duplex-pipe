namespace DuplexPipe
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class LocalBus : IBus
    {
        private readonly Dictionary<Type, Dictionary<string, Delegate>> callbacks =
            new Dictionary<Type, Dictionary<string, Delegate>>();

        public void Publish(object payload)
        {
            Type payloadType = payload.GetType();
            Dictionary<string, Delegate> items;
            if (callbacks.TryGetValue(payloadType, out items))
            {
                foreach (var callback in items.Values.ToList())
                {
                    callback.DynamicInvoke(payload);
                }
            }
        }

        public void Always<T>(string key, Action<T> callback)
        {
            Type payloadType = typeof(T);
            Dictionary<string, Delegate> items;
            if (!callbacks.TryGetValue(payloadType, out items))
            {
                items = new Dictionary<string, Delegate>();
                callbacks[payloadType] = items;
            }

            items[key] = callback;
        }

        public void Once<T>(string key, Action<T> callback)
        {
            Always<T>(key, sig =>
            {
                Forget<T>(key);
                callback(sig);
            });
        }

        public void Forget<T>(string key)
        {
            Type payloadType = typeof(T);
            Dictionary<string, Delegate> items;
            if (callbacks.TryGetValue(payloadType, out items))
            {
                if (items.ContainsKey(key))
                {
                    items.Remove(key);
                }
            }
        }
    }
}