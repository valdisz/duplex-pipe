namespace DuplexPipe
{
    using System;

    public sealed class ChannelSettings
    {
        public static readonly ChannelSettings Default = new ChannelSettings
        {
            ReaderTimeout = TimeSpan.FromSeconds(30),
            WaitForDrain = false
        };

        public TimeSpan ReaderTimeout { get; set; }

        public bool WaitForDrain { get; set; }
    }
}