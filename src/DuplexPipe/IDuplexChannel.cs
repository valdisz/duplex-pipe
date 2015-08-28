namespace DuplexPipe
{
    internal interface IDuplexChannel : IBus
    {
        void Signal<T>(T payload);
    }
}