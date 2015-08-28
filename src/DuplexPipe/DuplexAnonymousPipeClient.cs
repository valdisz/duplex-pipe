namespace DuplexPipe
{
    using System;
    using System.IO.Pipes;

    public class DuplexAnonymousPipeClient : IDuplexChannel, IDisposable
    {
        private readonly DuplexPipeChannel channel;
        private bool disposed;

        public DuplexAnonymousPipeClient(string senderHandle, string receiverHandle)
            : this(sender: new AnonymousPipeClientStream(PipeDirection.Out, receiverHandle),
                   receiver: new AnonymousPipeClientStream(PipeDirection.In, senderHandle))
        {
        }

        public DuplexAnonymousPipeClient(AnonymousPipeClientStream sender, AnonymousPipeClientStream receiver)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));

            channel = new DuplexPipeChannel(sender, receiver);
        }

        ~DuplexAnonymousPipeClient()
        {
            Dispose(false);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(DuplexAnonymousPipeClient));
        }

        public void Publish(object payload)
        {
            ThrowIfDisposed();

            throw new NotSupportedException();
        }

        public void Signal<T>(T payload)
        {
            ThrowIfDisposed();

            channel.Signal(payload);
        }

        public void Always<T>(string key, Action<T> callback)
        {
            ThrowIfDisposed();

            channel.Always(key, callback);
        }

        public void Once<T>(string key, Action<T> callback)
        {
            ThrowIfDisposed();

            channel.Once(key, callback);
        }

        public void Forget<T>(string key)
        {
            ThrowIfDisposed();

            channel.Forget<T>(key);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                channel.Dispose();
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
