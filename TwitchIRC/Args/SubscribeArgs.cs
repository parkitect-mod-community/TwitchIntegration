using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwitchIntegration
{
    public class SubscribeArgs : EventArgs
    {
        public string rawMessage { get; private set; }
        
        public string displayName{get;private set;}
        public Color color { get; private set; }
        public List<KeyValuePair<string,string>> badges { get; private set; }
        public string emoteSet { get; private set; }
        public string msgId { get; private set; }
        public TwitchUser twitchUser{ get; private set; }
        public TwitchMessage.UserType userType { get; private set; }
        public int userId{get;private set;}
        public bool turbo{ get; private set; }
        public int numberOfMonths{ get; private set; }
        public int roomId{get;private set;}
        public string systemMessage{ get; private set; }
        public IrcChannel channel { get; private set; }
        public string message{ get; private set; }
        public bool isMod{ get; private set; }
        public bool subscriber{ get; private set; }

        public SubscribeArgs (string raw, List<KeyValuePair<string,string>> arguments,IrcChannel channel,string payload)
        {
            this.rawMessage = raw;
            this.channel = channel;
            this.message = message;
            foreach (KeyValuePair<string,string> arg in arguments) {
                switch (arg.Key) {
                case "badges":
                    if(arg.Value.Contains("/"))
                    {
                        if (!arg.Value.Contains(","))
                            this.badges.Add(new KeyValuePair<string, string>(arg.Value.Split('/')[0], arg.Value.Split('/')[1]));
                        else
                            foreach (string badge in arg.Value.Split(','))
                                this.badges.Add(new KeyValuePair<string, string>(arg.Value.Split('/')[0], arg.Value.Split('/')[1]));
                    }
                    break;
                case "color":
                    if(arg.Value.Trim() != "")
                        color = ColorUtility.hexToColor (arg.Value);
                    break;
                case "display-name":
                    this.displayName = arg.Value;
                    break;
                case "emotes":
                    this.emoteSet =arg.Value;
                    break;
                case "subscriber":
                    this.subscriber = arg.Value == "1";
                    break;
                case "turbo":
                    turbo = arg.Value == "1";
                    break;
                case "msg-id":
                    this.msgId = arg.Value;
                    break;
                case "msg-param-months":
                    this.numberOfMonths =  int.Parse(arg.Value);
                    break;
                case "roomId":
                    this.roomId = int.Parse(arg.Value);
                    break;
                case "user-id":
                    userId = int.Parse(arg.Value);
                    break;
                case "system-msg":
                    this.systemMessage = arg.Value;
                    break;
                case "login":
                    twitchUser = new TwitchUser (arg.Value);
                    break;
                case "user-type":
                    switch (arg.Value)
                    {
                    case "mod":
                        userType = TwitchMessage.UserType.Moderator;
                        break;
                    case "global_mod":
                        userType = TwitchMessage.UserType.GlobalModerator;
                        break;
                    case "admin":
                        userType = TwitchMessage.UserType.Admin;
                        break;
                    case "staff":
                        userType = TwitchMessage.UserType.Staff;
                        break;
                    default:
                        userType = TwitchMessage.UserType.Viewer;
                        break;
                    }
                    break;
                case "mod":
                    isMod =arg.Value == "1";
                    break;

                }

            }

        }
    }
}

