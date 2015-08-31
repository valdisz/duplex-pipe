namespace DuplexPipe
{
    using System;
    using System.IO;
    using System.IO.Pipes;

    public class DuplexAnonymousPipeServer : IDisposable
    {
        private readonly DuplexPipeChannel channel;
        private bool disposed;

        public IDuplexChannel Channel => channel;

        public DuplexAnonymousPipeServer(ChannelSettings? settings = null)
            : this(sender: new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable),
                   receiver: new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable),
                   settings: settings)
        {
        }

        public DuplexAnonymousPipeServer(AnonymousPipeServerStream sender, AnonymousPipeServerStream receiver, ChannelSettings? settings = null)
        {
            channel = new DuplexPipeChannel(sender, receiver, settings);
        }

        ~DuplexAnonymousPipeServer()
        {
            Dispose(false);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(DuplexAnonymousPipeServer));
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