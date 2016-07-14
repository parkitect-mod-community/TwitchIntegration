using System;
	public class ModConfiguration : SerializedRawObject {
		public enum AuthorizationLevel {
			NOBODY, SUBSCRIBERS, ANYONE
		}

		[Serialized]
		public AuthorizationLevel authPostAlerts = AuthorizationLevel.SUBSCRIBERS;
		[Serialized]
		public AuthorizationLevel authSpawnGuests = AuthorizationLevel.SUBSCRIBERS;

		[Serialized]
		public string twitchOAuthToken = "";
		[Serialized]
		public string twitchUsername = "";
		[Serialized]
		public string twitchChannelName = "";

		public ModConfiguration() {

		}
	}


