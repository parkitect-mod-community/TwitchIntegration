using System;

namespace TwitchIntegration
{
    public class TwitchUser
    {
        public string name{get;private set;}
        public string token{get;private set;}

        public TwitchUser (string name)
        {
            this.name = name;
        }

        public TwitchUser (string name,string token)
        {
            this.name = name;
            this.token = token;
        }

        public bool Equals(TwitchUser user)
        {
            return this.name == user.name;
            
        }


    }
}

