using System;

namespace TwitchIntegration
{
    public class IrcChannel
    {
        public string channel{ get; private set;}

        public IrcChannel (string channel)
        {
            if (channel.StartsWith ("#"))
                channel = channel.Substring (1);
            this.channel = channel;
        }
    }
}

