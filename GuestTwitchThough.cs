using System;

namespace TwitchIntegration
{
    public class GuestTwitchThough : Thought
    {
        public Guest guest {
            get;
            private set;
        }

        public TwitchUser twitchUser {
            get;
            private set;
        }

        public GuestTwitchThough (TwitchUser twitchUser,string text,Guest guest, Thought.Emotion emotion,SerializedMonoBehaviour thinkAbout): base(text,emotion,thinkAbout)
        {
            
            this.guest = guest;
            this.twitchUser = twitchUser;
        }
    }
}

