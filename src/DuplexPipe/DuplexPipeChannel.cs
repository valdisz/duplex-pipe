namespace DuplexPipe
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Runtime.Serialization.Formatters;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    internal sealed class DuplexPipeChannel : IDisposable, IDuplexChannel
    {
        private readonly ChannelSettings settings;

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
        private readonly Task listentingTask;
        private readonly CancellationTokenSource threadCancelationTokens;
        private bool disposed;

        public DuplexPipeChannel(PipeStream outStream, PipeStream inStream, ChannelSettings settings = null)
        {
            this.settings = settings ?? ChannelSettings.Default;
            this.OutStream = outStream;
            this.InStream = inStream;

            writer = new StreamWriter(outStream);
            reader = new StreamReader(inStream);

            threadCancelationTokens = new CancellationTokenSource();

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
                threadCancelationTokens.Cancel();
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

                writer.Dispose();
                reader.Dispose();

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
