using System;

namespace TwitchIntegration
{
    public class UserChannelArgs : EventArgs
    {
        public TwitchUser user{ get; private set; }
        public IrcChannel channel{ get; private set; }
        public UserChannelArgs(TwitchUser user,IrcChannel channel)
        {
            this.user = user;
            this.channel = channel;
        }
    }
}

