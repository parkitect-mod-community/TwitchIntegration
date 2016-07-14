using System;

namespace TwitchIntegration
{
    public class TwitchCredentials
    {
        public string name { get; private set;}
        public string token {get;private set;}

        public TwitchCredentials (string name)
        {
            this.name = name;
        }

        public TwitchCredentials(string name, string token)
        {
            this.name = name;
            this.token = token;
        }
    }
}

