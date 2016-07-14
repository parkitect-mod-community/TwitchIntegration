using System;
using System.Collections.Generic;
using UnityEngine;

namespace TwitchIntegration
{
    public class TwitchMessage : EventArgs
    {

        public enum UserType{
            Moderator,
            GlobalModerator,
            Admin,
            Staff,
            Viewer
        }

        public string emoteSet;
        public IrcChannel channel { get; set; }
        public string rawMessage { get; set;}
        public string displayName{get;set;}
        public int userId{get;set;}
        public bool turbo{get;set;}
        public bool isMod{get;set;}
        public string message{get;set;}
        public bool subscriber{get;set;}
        public Color color{get;set;}
        public UserType userType { get; set;}
        public List<KeyValuePair<string,string>> badges { get; set; }
        public TwitchUser twitchUser;

        public TwitchMessage (string raw, List<KeyValuePair<string,string>> arguments,IrcChannel channel ,TwitchUser twitchUser,string payload)
        {
            
            this.badges = new List<KeyValuePair<string, string>> ();
            this.rawMessage = raw;
            this.twitchUser = twitchUser;
            this.message = payload;
            this.channel = channel;
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

                case "user-id":
                    userId = int.Parse(arg.Value);
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

