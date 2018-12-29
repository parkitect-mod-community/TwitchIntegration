//#define DEBUG_LOGGING

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Parkitect.UI;
using System.Text.RegularExpressions;

namespace TwitchIntegration
{
    /// <summary>
    /// Allows your Twitch viewers to spawn a guest in the game and follow their actions, and post notifications.
    /// Uses IRC.Net, backported to work with .Net 2.0.
    /// </summary>
    public class TwitchIntegration : MonoBehaviour
    {

        private event EventHandler syncHandle;

        private Dictionary<string, string> twitchApiHeaders = new Dictionary<string, string> ();

        private List<string> ircLog = new List<string> ();
        private StringBuilder ircLogStringBuilder = new StringBuilder ();
        private string ircLogString;
        // we have to interact with the game from the main thread, so we queue all irc messages and evaluate them in Update()

        public TwitchGuestAssocCollection collection{ get;set;}

        public TwitchIrcClient ircClient{ get; set;}
        public IrcChannel channel{ get; set;}
	

		
        private bool drawGUI = true;


        #region MonoBehaviour lifecycle events

        void Start ()
        {
            collection = new TwitchGuestAssocCollection ();

            twitchApiHeaders.Add("Accept", "application/vnd.twitchtv.v3+json");
            twitchApiHeaders.Add("Authorization", "OAuth " + Main.configuration.settings.twitchOAuthToken);
            twitchApiHeaders.Add("Client-ID", "ogtnoqm4m86chl2oflq5myfznk8u5oq");


            ircClient = new TwitchIrcClient ();

            ircClient.OnConnected += onIrcConnected;
            ircClient.OnJoinChannel += onIrcJoined;
            ircClient.OnLeaveChannel += OnLeavelRoom;
            ircClient.OnSubscribe += IrcClient_OnSubscribe;
            //ircClient.Disconnected += onIrcDisconnected;
            //ircClient.Registered += onIrcRegistered;
            //ircClient.Error += onIrcError;
            //ircClient.NoticeMessage += onIrcErrorMessageReceived;

            ircClient.OnMessage += onIrcChannelMessageReceived;
            ircClient.Connect (true, Main.configuration.settings.twitchUsername, Main.configuration.settings.twitchOAuthToken);
        }

 

		
        void Update ()
        {
            var handle = syncHandle;

            if (handle != null) {
                handle (this, new EventArgs ());
                syncHandle = null;
            }

            if (Input.GetKeyDown (KeyCode.T)) {
                drawGUI = !drawGUI;
            }
	
        }

        void OnDestroy ()
        {
			
            ircClient.Disconnect();
        }

        #endregion

        #region IRC events

        private void onIrcConnected (object sender, EventArgs e)
        {
            addIrcLogEntry ("Connected, logging into account...");

            channel = ircClient.joinChannel (Main.configuration.settings.twitchChannelName.ToLower ());
            ircClient.EnableMembership ();
            ircClient.EnableTags ();
            ircClient.EnableCommands ();

            ircClient.SendMessage (channel,1000,-1, "Hi, I'm the Parkitect Twitch Integration!");
            UnityEngine.Debug.Log ("joined channel");

           
        }

        private void IrcClient_OnSubscribe (object sender, SubscribeArgs e)
        {
            if (Main.configuration.settings.subscriptionNotification) {
                syncHandle += (object sr, EventArgs ev) => {
                    
                    NotificationBar.Instance.addNotification (new Notification("Twitch",e.displayName + " has subbed for  " + e.numberOfMonths + " months in a row: " + e.message));
                
                };
            }
        }

        private void onIrcJoined (object sender, UserChannelArgs e)
        {
            if (!isPermitted (e.user, Main.configuration.settings.authSpawnGuests)) {
                return;
            }

           
            if (collection.GetGuest (e.user) != null)
                return;


            Guest userGuest = null;//GameController.Instance.park.getGuests ().Where (x => x.nickname.ToLower () == e.user.name.ToLower() ).DefaultIfEmpty(null).Single();

            // check if the guest of this user already exists in the park (e.g. loaded from savegame, or user reconnected)
            foreach (Guest parkGuest in GameController.Instance.park.getGuests()) {
                if (parkGuest.nickname.ToLower ().Equals (e.user.name)) {
                    userGuest = parkGuest;
                    break;
                }
            }
       
            // no matching guest found, spawn a new one
            if (userGuest == null) {
                syncHandle += (object sr, EventArgs ev) => {
                    userGuest = GameController.Instance.park.spawnUnInitializedPerson (Prefabs.Guest) as Guest;
                    userGuest.nickname = e.user.name;

                    Match match = Regex.Match (e.user.name, @"(\w+)(?:\s+|_+)(\w+)", RegexOptions.IgnoreCase);
                    if (match.Success) {
                        userGuest.forename = match.Groups [1].Value;
                        if (match.Groups [2].Success) {
                            userGuest.surname = match.Groups [2].Value;
                        }
                    }
                    else
                    {
                        match = Regex.Match (e.user.name, @"([A-Z][a-z]+)([A-Z][a-z]+)", RegexOptions.IgnoreCase);
                        if (match.Success) {
                            userGuest.forename = match.Groups [1].Value;
                            if (match.Groups [2].Success) {
                                userGuest.surname = match.Groups [2].Value;
                            }
                        }
                        else
                        {
                            userGuest.forename = e.user.name;
                            userGuest.surname = "-";
                        }
                    }

                    userGuest.Initialize ();

                    if (Main.configuration.settings.twitchSpawnGuestNotification) {
                        NotificationBar.Instance.addNotification (new Notification("Twitch",e.user.name + " has spawned in as a guest",Notification.Type.DEFAULT,userGuest.transform.position));
                    }


                    collection.RegisterUser(e.user,userGuest);
                };
            } else {
                syncHandle += (object sr, EventArgs ev) => {
                    Guest guest = collection.GetGuest(e.user);
                    if(guest == null)
                        return;
                    
                    guest.startLongTermPlan(null);
                };
            }
            
        }


        private void OnLeavelRoom (object sender, UserChannelArgs e)
        {
            UnityEngine.Debug.Log ("user left channel:" + e.user.name);
            syncHandle += (object sr, EventArgs ev) => {
                Guest g =  collection.GetGuest(e.user);
                if(g != null)
                {
                    g.startLongTermPlan(new LeaveParkPlan(g, GameController.Instance.park.getSpawns()[0].centerPosition));
                }
            };
        }

        private void onIrcDisconnected (object sender, EventArgs e)
        {
            addIrcLogEntry ("Disconnected.");
        }

        //private void onIrcError(object sender, ErrorEventArgs e) {
        //addIrcLogEntry("Error: " + e.Error.ToString());
        //}

        //private void onIrcErrorMessageReceived (object sender, NoticeMessageArg e)
       // {
            //addIrcLogEntry("Error msg: " + e.Message);
        //}



        private void onIrcChannelMessageReceived (object sender, TwitchMessage e)
        {
            TwitchMessage ms = e;


            UnityEngine.Debug.Log (ms.message);
            //addIrcLogEntry(e.displayName + ": " + e.message);
            if (ms.message.StartsWith ("!alert")) {
                if (!isPermitted (ms.twitchUser, Main.configuration.settings.authPostAlerts)) {
                    if(Main.configuration.settings.authPostAlerts == Settings.AuthorizationLevel.SUBSCRIBERS)
                        ircClient.SendMessagePrivate (channel,10f,1, e.twitchUser, "You need to be subscribed to this channel to do this.");
                    return;
                }
                syncHandle += (object sr, EventArgs ev) => {
                        
                    NotificationBar.Instance.addNotification (new Notification("Twitch",ms.displayName + ": " + ms.message.Remove (0, "!alert".Length + 1)));
                };

            } 
            if (ms.message.Equals ("!thoughts") || ms.message.StartsWith("!th")) {
                Guest guest = collection.GetGuest (ms.twitchUser);
               
                if (guest == null)
                    return;
                
                if (guest.thoughts.Count > 0) {
                    ircClient.SendMessage (channel, 5f, 20, "Thoughts of " + guest.getName () + ": " + guest.thoughts [guest.thoughts.Count - 1].text);
                } 
            } else if (ms.message.StartsWith ("!actions") || ms.message.StartsWith("!ac")) {
                Guest guest = collection.GetGuest (ms.twitchUser);
                if (guest == null)
                    return;
                List<Experience> experiences = guest.experienceLog.getExperiences ();
                if (experiences.Count > 0) {
                    ircClient.SendMessage (channel,5f,20, "Actions of " + guest.getName () + ": " + experiences [experiences.Count - 1].getDescription ());
                }
            } else if (ms.message.StartsWith ("!status") || ms.message.StartsWith("!st")) {
                Guest guest = collection.GetGuest (ms.twitchUser);
                if (guest == null)
                    return;
                ircClient.SendMessage (channel,5f,20, "Status of " + guest.getName () + ": " + guest.currentBehaviour.getDescription () + ".");
            } else if (ms.message.StartsWith ("!spawn") || ms.message.StartsWith("!sp")) {
                onIrcJoined(sender,new UserChannelArgs(e.twitchUser,e.channel));
            }
            else if (ms.message.StartsWith ("!inventory") || ms.message.StartsWith("!in")) {
                Guest guest = collection.GetGuest (ms.twitchUser);;
                if (guest == null)
                    return;
                List<string> inventoryItems = new List<string> ();
                foreach (Item item in guest.getFromInventory<Item>()) {
                    inventoryItems.Add (item.getDescription ());
                }
                ircClient.SendMessage (channel,5f,20, "Inventory of " + guest.getName () + ": " + string.Join (", ", inventoryItems.ToArray ()));
            } else {

                if (!isPermitted (ms.twitchUser, Main.configuration.settings.authTwitchFeedGuestThoughts)) {
                    return;
                }

                syncHandle += (object sr, EventArgs ev) => {
                    Guest guest = collection.GetGuest (ms.twitchUser);
                    if (guest == null)
                        return;
                    guest.think (new Thought (ms.message, Thought.Emotion.NEUTRAL, null));
                };
            }
        }

        #endregion

        private void addIrcLogEntry (string logEntry)
        {
            ircLog.Add (logEntry);
            if (ircLog.Count > 30) {
                ircLog.RemoveAt (0);
            }

            ircLogStringBuilder.Length = 0;
            for (int i = 0; i < ircLog.Count; i++) {
                ircLogStringBuilder.AppendLine (ircLog [i]);
            }

            ircLogString = ircLogStringBuilder.ToString ();
        }



        private bool isPermitted (TwitchUser user, Settings.AuthorizationLevel authorizationLevel)
        {
            if (authorizationLevel == Settings.AuthorizationLevel.ANYONE) {
                return true;
            } else if (authorizationLevel == Settings.AuthorizationLevel.NOBODY) {
                return false;
            } else if (authorizationLevel == Settings.AuthorizationLevel.SUBSCRIBERS) {

                WWW www = new WWW ("https://api.twitch.tv/kraken/channels/" + Main.configuration.settings.twitchChannelName.ToLower () + "/subscriptions/" + user.name, null, twitchApiHeaders);
                // FIXME ok this is a shitty thing to do, we really shouldn't block the main thread
                while (!www.isDone) {
                }

                if (www.error != null) {
                    return false;
                }
                return true;
            }

            return false;
        }
    }
}