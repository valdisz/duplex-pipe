namespace DuplexPipe
{
    using System;

    public interface IBus
    {
        void Publish(object payload);
        void Always<T>(string key, Action<T> callback);
        void Once<T>(string key, Action<T> callback);
        void Forget<T>(string key);
    }
}