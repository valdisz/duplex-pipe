namespace DuplexPipe
{
    using System.IO;

    public interface IHydrator
    {
        Stream Dehydrate<T>(T payload);

        object Hydrate(Stream stream);
    }
}