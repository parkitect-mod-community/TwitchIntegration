using System;
namespace TwitchIntegration {
	public class Settings : SerializedRawObject {
		public enum AuthorizationLevel {
			NOBODY, SUBSCRIBERS, ANYONE
		}

		[Serialized]
		public AuthorizationLevel authPostAlerts = AuthorizationLevel.SUBSCRIBERS;
		[Serialized]
		public AuthorizationLevel authSpawnGuests = AuthorizationLevel.SUBSCRIBERS;

        [Serialized]
        public AuthorizationLevel authTwitchFeedGuestThoughts = AuthorizationLevel.SUBSCRIBERS;

        [Serialized]
        public bool twitchSpawnGuestNotification = false;

        [Serialized]
        public bool subscriptionNotification = false;

        [Serialized]
        public bool defaultGuestSpawning = false;


		[Serialized]
		public string twitchOAuthToken = "";
		[Serialized]
		public string twitchUsername = "";
		[Serialized]
		public string twitchChannelName = "";

		public Settings() {

		}
	}
}

