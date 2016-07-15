//#define DEBUG_LOGGING

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Parkitect.UI;
using System.IO;
using MiniJSON;
using System.Text.RegularExpressions;
using TwitchIntegration;
using System.Threading;

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

            ircClient.Connected += onIrcConnected;
            ircClient.OnJoinChannel += onIrcJoined;
            ircClient.OnLeaveChannel += OnLeavelRoom;
            //ircClient.Disconnected += onIrcDisconnected;
            //ircClient.Registered += onIrcRegistered;
            //ircClient.Error += onIrcError;
            //ircClient.NoticeMessage += onIrcErrorMessageReceived;

            ircClient.OnMessage += onIrcChannelMessageReceived;
            ircClient.Connect (false, Main.configuration.settings.twitchUsername, Main.configuration.settings.twitchOAuthToken);
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

            ircClient.SendMessage (channel, "Hi, I'm the Parkitect Twitch Integration!");
            UnityEngine.Debug.Log ("joined channel");
        }

        private void onIrcJoined (object sender, UserChannelArgs e)
        {
            if (!isPermitted (e.user, Main.configuration.settings.authPostAlerts)) {
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
                    userGuest.name = e.user.name;

                    Match match = Regex.Match (e.user.name, @"(\w+)(?:\s+|\_+)(\w+)", RegexOptions.IgnoreCase);
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
                    UnityEngine.Debug.Log ("user guest has spawned" + e.user.name);

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
                    g.startLongTermPlan(new LeaveParkPlan(g, GameController.Instance.park.spawns[0].centerPosition));
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

        private void onIrcErrorMessageReceived (object sender, NoticeMessageArg e)
        {
            //addIrcLogEntry("Error msg: " + e.Message);
        }



        private void onIrcChannelMessageReceived (object sender, TwitchMessage e)
        {
            TwitchMessage ms = e;


            UnityEngine.Debug.Log (ms.message);
            //addIrcLogEntry(e.displayName + ": " + e.message);
            if (ms.message.StartsWith ("!alert")) {
                if (!isPermitted (ms.twitchUser, Main.configuration.settings.authPostAlerts)) {
                    ircClient.SendMessagePrivate (channel, e.twitchUser, "You need to be subscribed to this channel to do this.");

                    return;
                }
                syncHandle += (object sr, EventArgs ev) => {
                    NotificationBar.Instance.addNotification (ms.displayName + ": " + ms.message.Remove (0, "!alert".Length + 1),default(Vector3),null);
                };
            } 

            if (!isPermitted (ms.twitchUser, Main.configuration.settings.authSpawnGuests)) {
                ircClient.SendMessagePrivate (channel, e.twitchUser, "You need to be subscribed to this channel to do this.");

                return;
            }

            if (ms.message.Equals ("!thoughts")) {
                Guest guest = collection.GetGuest (ms.twitchUser);
               
                if (guest == null)
                    return;
                if (guest.thoughts.Count > 0) {
                    ircClient.SendMessage (channel, "Thoughts of " + guest.getName () + ": " + guest.thoughts [guest.thoughts.Count - 1].text);
                }
            } else if (ms.message.StartsWith ("!actions")) {
                Guest guest = collection.GetGuest (ms.twitchUser);
                if (guest == null)
                    return;
                List<Experience> experiences = guest.experienceLog.getExperiences ();
                if (experiences.Count > 0) {
                    ircClient.SendMessage (channel, "Actions of " + guest.getName () + ": " + experiences [experiences.Count - 1].getDescription ());
                } 
            } else if (ms.message.StartsWith ("!status")) {
                Guest guest = collection.GetGuest (ms.twitchUser);
                if (guest == null)
                    return;
                ircClient.SendMessage (channel, "Status of " + guest.getName () + ": " + guest.currentBehaviour.getDescription () + ".");
            } else if (ms.message.StartsWith ("!spawn")) {
                onIrcJoined(sender,new UserChannelArgs(e.twitchUser,e.channel));
            }
            else if (ms.message.StartsWith ("!inventory")) {
                Guest guest = collection.GetGuest (ms.twitchUser);;
                if (guest == null)
                    return;
                List<string> inventoryItems = new List<string> ();
                foreach (Item item in guest.getFromInventory<Item>()) {
                    inventoryItems.Add (item.getDescription ());
                }
                ircClient.SendMessage (channel, "Inventory of " + guest.getName () + ": " + string.Join (", ", inventoryItems.ToArray ()));
            } else {
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