namespace DuplexPipe
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Runtime.Serialization.Formatters;
    using System.Threading;
    using Newtonsoft.Json;

    internal sealed class DuplexPipeChannel : IDisposable, IDuplexChannel
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public PipeStream OutStream { get; }
        private readonly TextWriter writer;

        public PipeStream InStream { get; }
        private readonly TextReader reader;

        private readonly LocalBus bus = new LocalBus();
        //private readonly Thread listentingThread;
        private readonly CancellationTokenSource threadCancelationTokens;
        private bool disposed;

        public DuplexPipeChannel(PipeStream outStream, PipeStream inStream)
        {
            this.OutStream = outStream;
            this.InStream = inStream;

            writer = new StreamWriter(outStream);
            reader = new StreamReader(inStream);

            threadCancelationTokens = new CancellationTokenSource();
            //listentingThread = new Thread(Listener)
            //{
            //    Name = "Listener-" + DateTime.Now.Ticks
            //};
            //listentingThread.Start(this);
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

        private static void Listener(object args)
        {
            DuplexPipeChannel channel = (DuplexPipeChannel)args;

            IBus bus = channel.bus;
            TextReader reader = channel.reader;
            CancellationTokenSource cts = channel.threadCancelationTokens;
            CancellationToken token = cts.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var readTask = reader.ReadLineAsync();
                    readTask.Wait(token);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    string payload = readTask.Result;
                    if (string.IsNullOrWhiteSpace(payload))
                    {
                        continue;
                    }

                    if (!token.IsCancellationRequested)
                    {
                        object hydratedPayload = Hydrate(payload);
                        bus.Publish(hydratedPayload);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // we do nothing here as it is expected exception
            }
        }

        private static string Dehydrate<T>(T payload)
        {
            string json = JsonConvert.SerializeObject(payload, JsonSettings);
            return json;
        }

        private static object Hydrate(string json)
        {
            object hydrated = JsonConvert.DeserializeObject(json, JsonSettings);
            return hydrated;
        }

        public void Signal<T>(T payload)
        {
            ThrowIfDisposed();

            var dehydreated = Dehydrate(payload);

            writer.WriteLine(dehydreated);
            writer.Flush();
            // wed don't care that client actually reads it before we exit
            //outStream.WaitForPipeDrain();
        }

        public void Publish(object payload)
        {
            ThrowIfDisposed();

            bus.Publish(payload);
        }

        public void Always<T>(string key, Action<T> callback)
        {
            ThrowIfDisposed();

            bus.Always<T>(key, callback);
        }

        public void Once<T>(string key, Action<T> callback)
        {
            ThrowIfDisposed();

            bus.Always<T>(key, callback);
        }

        public void Forget<T>(string key)
        {
            ThrowIfDisposed();

            bus.Forget<T>(key);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                threadCancelationTokens.Cancel();
                //if (!listentingThread.Join(TimeSpan.FromSeconds(30)))
                //{
                //    listentingThread.Abort();
                //}

                writer.Dispose();
                reader.Dispose();

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
