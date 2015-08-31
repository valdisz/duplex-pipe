namespace DuplexPipe
{
    public interface IDuplexChannel : IBus
    {
        void Signal<T>(T payload);
    }
}