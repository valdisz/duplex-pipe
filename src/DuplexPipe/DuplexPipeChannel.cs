namespace DuplexPipe
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class DuplexPipeChannel : IDisposable, IDuplexChannel
    {
        private readonly ChannelSettings settings;


        public PipeStream OutStream { get; }
        public PipeStream InStream { get; }

        private readonly LocalBus bus = new LocalBus();
        private readonly Task listentingTask;
        private readonly CancellationTokenSource thrCancelTokens;
        private bool disposed;

        public DuplexPipeChannel(PipeStream outStream, PipeStream inStream, ChannelSettings? settings = null)
        {
            this.settings = settings ?? ChannelSettings.Default;
            this.OutStream = outStream;
            this.InStream = inStream;

            thrCancelTokens = new CancellationTokenSource();

            listentingTask = Task.Factory.StartNew(Listener, this, TaskCreationOptions.LongRunning);
        }

        ~DuplexPipeChannel()
        {
            Dispose(false);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(DuplexPipeChannel));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct HeaderPacket
        {
            [FieldOffset(0)]
            public bool Final;

            [FieldOffset(8)]
            public int Size;
        }
        private static readonly int HeaderPacketSize = Marshal.SizeOf(typeof(HeaderPacket));

        private static int ReadBytes(byte[] buffer, Stream stream)
        {
            int red = 0;

            //Loop until we've read enough bytes (usually once) 
            while (red < buffer.Length)
            {
                red += stream.Read(buffer, red, buffer.Length - red); //Read bytes 
            }

            return red;
        }

        private static byte[] HeaderPacktToByteArray(HeaderPacket header)
        {
            byte[] buff = new byte[HeaderPacketSize];//Create Buffer
            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);//Hands off GC
            try
            {
                Marshal.StructureToPtr(header, handle.AddrOfPinnedObject(), false);
                return buff;
            }
            finally
            {
                handle.Free();
            }
        }

        private static void Listener(object args)
        {
            DuplexPipeChannel channel = (DuplexPipeChannel) args;

            IBus bus = channel.bus;
            Stream stream = channel.InStream;
            IHydrator hydrator = channel.settings.Hydrator;

            CancellationTokenSource cts = channel.thrCancelTokens;
            CancellationToken token = cts.Token;

            while (!token.IsCancellationRequested)
            {
                object payload;
                using (MemoryStream ms = new MemoryStream())
                {
                    bool final;
                    do
                    {
                        byte[] buffer = new byte[HeaderPacketSize];
                        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                        try
                        {
                            ReadBytes(buffer, stream);
                            HeaderPacket header =
                                (HeaderPacket)
                                    Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof (HeaderPacket));
                            final = header.Final;

                            if (token.IsCancellationRequested)
                            {
                                break;
                            }

                            if (header.Size > 0)
                            {
                                byte[] dataBuffer = new byte[header.Size];
                                var red = ReadBytes(dataBuffer, stream);
                                ms.Write(dataBuffer, 0, red); 
                            }
                        }
                        finally
                        {
                            handle.Free();
                        }
                    } while (!final && !token.IsCancellationRequested);

                    if (ms.Length == 0)
                    {
                        continue;
                    }
                    ms.Seek(0, SeekOrigin.Begin);

                    try
                    {
                        payload = hydrator.Hydrate(ms);
                    }
                    catch
                    {
                        payload = null;
                    }
                }

                if (payload != null)
                {
                    bus.Publish(payload);
                }
            }
        }

        public void Signal<T>(T payload)
        {
            ThrowIfDisposed();

            var hydrator = settings.Hydrator;
            using (Stream dehydreated = hydrator.Dehydrate(payload))
            {
                byte[] buffer = new byte[0x100];
                int size = 0;
                do
                {
                    size = ReadBytes(buffer, dehydreated);

                    var header = HeaderPacktToByteArray(new HeaderPacket
                    {
                        Final = size < buffer.Length,
                        Size = size
                    });

                    OutStream.Write(header, 0, header.Length);
                    if (size > 0)
                    {
                        OutStream.Write(buffer, 0, size); 
                    }
                } while (size != 0);
            }

            OutStream.Flush();

            if (settings.WaitForDrain)
            {
                OutStream.WaitForPipeDrain();
            }
        }

        public void Publish(object payload)
        {
            ThrowIfDisposed();

            bus.Publish(payload);
        }

        public void Always<T>(Action<T> callback)
        {
            ThrowIfDisposed();

            bus.Always<T>(callback);
        }

        public void Once<T>(Action<T> callback)
        {
            ThrowIfDisposed();

            bus.Always<T>(callback);
        }

        public void Forget<T>()
        {
            ThrowIfDisposed();

            bus.Forget<T>();
        }

        public void ForgetAll()
        {
            ThrowIfDisposed();

            bus.ForgetAll();
        }

        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                thrCancelTokens.Cancel();
                try
                {
                    listentingTask.Wait(settings.ReaderTimeout);
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Count != 1 || !(ex.InnerException is OperationCanceledException))
                    {
                        throw;
                    }
                }

                OutStream.Dispose();
                InStream.Dispose();

                bus.ForgetAll();

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
