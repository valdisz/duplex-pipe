namespace DuplexPipe
{
    using System;

    public struct ChannelSettings
    {
        public static readonly ChannelSettings Default = new ChannelSettings
        {
            ReaderTimeout = TimeSpan.FromSeconds(30),
            WaitForDrain = false,
            Hydrator = new JsonHydrator()
        };

        public TimeSpan ReaderTimeout { get; set; }

        public bool WaitForDrain { get; set; }

        public IHydrator Hydrator { get; set; }
    }
}