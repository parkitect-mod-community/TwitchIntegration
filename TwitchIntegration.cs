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

namespace TwitchIntegration {
	/// <summary>
	/// Allows your Twitch viewers to spawn a guest in the game and follow their actions, and post notifications.
	/// Uses IRC.Net, backported to work with .Net 2.0.
	/// </summary>
	public class TwitchIntegration : MonoBehaviour {


		private Dictionary<string, string> twitchApiHeaders = new Dictionary<string, string>();

		private List<string> ircLog = new List<string>();
		private StringBuilder ircLogStringBuilder = new StringBuilder();
		private string ircLogString;
		// we have to interact with the game from the main thread, so we queue all irc messages and evaluate them in Update()
        private Queue<TwitchMessage> messageQueue = new Queue<TwitchMessage>();

		private Dictionary<string, Guest> twitchUserGuestAssoc = new Dictionary<string, Guest>();

		private TwitchIrcClient ircClient;
        private IrcChannel channel;
	

		
		private bool drawGUI = true;


		#region MonoBehaviour lifecycle events
		void Start() {
	

			//twitchApiHeaders.Add("Accept", "application/vnd.twitchtv.v3+json");
			//twitchApiHeaders.Add("Authorization", "OAuth " + settings.twitchOAuthToken);
			//twitchApiHeaders.Add("Client-ID", twitchClientID);
       
            ircClient = new TwitchIrcClient();

                // Twitch doesn't allow sending more than 20 messages within 30 seconds
			//ircClient.FloodPreventer = new IrcStandardFloodPreventer(1, (long)((30f / 20) * 1000));

			ircClient.Connected += onIrcConnected;
            ircClient.OnJoinChannel += onIrcJoined;
			//ircClient.Disconnected += onIrcDisconnected;
			//ircClient.Registered += onIrcRegistered;
			//ircClient.Error += onIrcError;
			//ircClient.NoticeMessage += onIrcErrorMessageReceived;
            ircClient.OnMessage += onIrcChannelMessageReceived;
            ircClient.Connect (false, Main.configuration.settings.twitchUsername, Main.configuration.settings.twitchOAuthToken);
		}
		
		void Update() {
			if (Input.GetKeyDown(KeyCode.T)) {
				drawGUI = !drawGUI;
			}
			
			if (messageQueue.Count > 0) {
				parseMessage(messageQueue.Dequeue());
			}
		}
		
		void OnDestroy() {
			
			//ircClient.Disconnect();
		}

		
		#endregion

		#region IRC events
		private void onIrcConnected(object sender, EventArgs e) {
            addIrcLogEntry("Connected, logging into account...");

            channel =  ircClient.joinChannel (Main.configuration.settings.twitchChannelName.ToLower());
            ircClient.EnableMembership();
            ircClient.EnableTags ();

		}

        private void onIrcJoined(object sender, UserChannelArgs e) {
			addIrcLogEntry("Logged in, joining channel...");


            this.channel = e.channel;
            ircClient.SendMessage(channel, "Hi, I'm the Parkitect Twitch Integration!");
		}

		private void onIrcDisconnected(object sender, EventArgs e) {
			addIrcLogEntry("Disconnected.");
		}

        /*private void onIrcError(object sender, ErrorEventArgs e) {
			//addIrcLogEntry("Error: " + e.Error.ToString());
		}

        private void onIrcErrorMessageReceived(object sender, NoticeMessageArg e) {
			//addIrcLogEntry("Error msg: " + e.Message);
		}*/
        
        private void onIrcChannelMessageReceived(object sender, TwitchMessage e) {
            UnityEngine.Debug.Log (e.message);
            //addIrcLogEntry(e.displayName + ": " + e.message);
			messageQueue.Enqueue(e);
		}
		#endregion

		private void addIrcLogEntry(string logEntry) {
			ircLog.Add(logEntry);
			if (ircLog.Count > 30) {
				ircLog.RemoveAt(0);
			}

			ircLogStringBuilder.Length = 0;
			for (int i = 0; i < ircLog.Count; i++) {
				ircLogStringBuilder.AppendLine(ircLog[i]);
			}

			ircLogString = ircLogStringBuilder.ToString();
		}

        private void parseMessage(TwitchMessage message) {


            if (message.message.StartsWith("!alert")) {
                if (!isPermitted(message.twitchUser, Main.configuration.settings.authPostAlerts)) {
					return;
				}

                NotificationBar.Instance.addNotification(message.displayName + ": " + message.message.Remove(0, "!alert".Length + 1));
			}
            else if (message.message.Equals("!spawn")) {
                if (!isPermitted(message.twitchUser, Main.configuration.settings.authSpawnGuests)) {
					return;
				}

                if (twitchUserGuestAssoc.ContainsKey(message.displayName)) {
                    if (twitchUserGuestAssoc[message.displayName] == null) {
                        twitchUserGuestAssoc.Remove(message.displayName);
					}
					else {
						// we already know the guest of this user
						return;
					}
				}

				Guest userGuest = null;

				// check if the guest of this user already exists in the park (e.g. loaded from savegame, or user reconnected)
				foreach (Guest parkGuest in GameController.Instance.park.getGuests()) {
                    if (parkGuest.nickname.Equals(message.displayName)) {
						userGuest = parkGuest;
						break;
					}
				}

				// no matching guest found, spawn a new one
				if (userGuest == null) {
					userGuest = GameController.Instance.park.spawnUnInitializedPerson(Prefabs.Guest) as Guest;
                    userGuest.nickname = message.displayName;

                    Match match = Regex.Match(message.message, @"""(\w+)""(?:\s*""(\w+)"")?", RegexOptions.IgnoreCase);
					
					if (match.Success) {
						userGuest.forename = match.Groups[1].Value;
						if (match.Groups[2].Success) {
							userGuest.surname = match.Groups[2].Value;
						}
					}

					userGuest.Initialize();
				}

                twitchUserGuestAssoc.Add(message.displayName, userGuest);
			}
            else if (message.message.Equals("!thoughts")) {
                Guest guest = twitchUserGuestAssoc[message.displayName];
				if (guest == null) {
					return;
				}

				if (guest.thoughts.Count > 0) {
                    ircClient.SendMessage(channel, "Thoughts of " + guest.getName() + ": " + guest.thoughts[guest.thoughts.Count - 1].text);
				}
			}
            else if (message.message.StartsWith("!actions")) {
                Guest guest = twitchUserGuestAssoc[message.twitchUser.name];
				if (guest == null) {
					return;
				}

				List<Experience> experiences = guest.experienceLog.getExperiences();
				if (experiences.Count > 0) {
                    ircClient.SendMessage(channel, "Actions of " + guest.getName() + ": " + experiences[experiences.Count - 1].getDescription());
				}
			}
            else if (message.message.StartsWith("!status")) {
                Guest guest = twitchUserGuestAssoc[message.twitchUser.name];
				if (guest == null) {
					return;
				}

                ircClient.SendMessage(channel, "Status of " + guest.getName() + ": " + guest.currentBehaviour.getDescription() + ".");
			}
            else if (message.message.StartsWith("!inventory")) {
                Guest guest = twitchUserGuestAssoc[message.twitchUser.name];
				if (guest == null) {
					return;
				}

				List<string> inventoryItems = new List<string>();
				foreach (Item item in guest.getFromInventory<Item>()) {
					inventoryItems.Add(item.getDescription());
				}
                ircClient.SendMessage(channel, "Inventory of " + guest.getName() + ": " + string.Join(", ", inventoryItems.ToArray()));
			}
		}

        private bool isPermitted(TwitchUser user, Settings.AuthorizationLevel authorizationLevel) {
			if (authorizationLevel == Settings.AuthorizationLevel.ANYONE) {
				return true;
			}
			else if (authorizationLevel == Settings.AuthorizationLevel.NOBODY) {
				return false;
			}
			else if (authorizationLevel == Settings.AuthorizationLevel.SUBSCRIBERS) {
                WWW www = new WWW("https://api.twitch.tv/kraken/channels/" + Main.configuration.settings.twitchChannelName.ToLower() + "/subscriptions/" + user, null, twitchApiHeaders);
				// FIXME ok this is a shitty thing to do, we really shouldn't block the main thread
				while (!www.isDone) { }

				if (www.error != null) {
                    ircClient.SendMessagePrivate (channel, user, user.name + ": You need to be subscribed to this channel to do this.");
                    //ircClient.SendMessage(channel, user + ": You need to be subscribed to this channel to do this.");
					return false;
				}

				return true;
			}

			return false;
		}
	}
}

