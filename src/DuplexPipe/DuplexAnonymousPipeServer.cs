namespace DuplexPipe
{
    using System;
    using System.IO;
    using System.IO.Pipes;

    public class DuplexAnonymousPipeServer : IDuplexChannel, IDisposable
    {
        private readonly DuplexPipeChannel channel;
        private bool disposed;

        public DuplexAnonymousPipeServer()
            : this(sender: new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable),
                   receiver: new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
        {
        }

        public DuplexAnonymousPipeServer(AnonymousPipeServerStream sender, AnonymousPipeServerStream receiver)
        {
            channel = new DuplexPipeChannel(sender, receiver);
        }

        ~DuplexAnonymousPipeServer()
        {
            Dispose(false);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(DuplexAnonymousPipeServer));
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

        public Handles GetConnectionHandles()
        {
            ThrowIfDisposed();

            var sender = (AnonymousPipeServerStream)channel.OutStream;
            var receiver = (AnonymousPipeServerStream)channel.InStream;

            return new Handles
            {
                SenderHandle = sender.GetClientHandleAsString(),
                ReceiverHandle = receiver.GetClientHandleAsString()
            };
        }

        public void ReleaseClientHandles()
        {
            ThrowIfDisposed();

            var sender = (AnonymousPipeServerStream)channel.OutStream;
            var receiver = (AnonymousPipeServerStream)channel.InStream;

            sender.DisposeLocalCopyOfClientHandle();
            receiver.DisposeLocalCopyOfClientHandle();
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

        public class Handles
        {
            public string SenderHandle { get; set; }
            public string ReceiverHandle { get; set; }
        }
    }
}