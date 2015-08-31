namespace DuplexPipe
{
    using System;
    using System.IO.Pipes;

    public class DuplexAnonymousPipeClient : IDisposable
    {
        private readonly DuplexPipeChannel channel;
        private bool disposed;

        public IDuplexChannel Channel => channel;

        public DuplexAnonymousPipeClient(string senderHandle, string receiverHandle, ChannelSettings settings = null)
            : this(sender: new AnonymousPipeClientStream(PipeDirection.Out, receiverHandle),
                   receiver: new AnonymousPipeClientStream(PipeDirection.In, senderHandle),
                   settings: settings)
        {
        }

        public DuplexAnonymousPipeClient(AnonymousPipeClientStream sender, AnonymousPipeClientStream receiver, ChannelSettings settings = null)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));

            channel = new DuplexPipeChannel(sender, receiver, settings);
        }

        ~DuplexAnonymousPipeClient()
        {
            Dispose(false);
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
