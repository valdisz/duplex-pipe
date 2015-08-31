namespace DuplexPipe
{
    using System;

    public interface IBus
    {
        void Publish(object payload);

        void Always<T>(Action<T> callback);

        void Once<T>(Action<T> callback);

        void Forget<T>();

        void ForgetAll();
    }
}