/**
* Copyright 2019 Michael Pollind
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

namespace TwitchIntegration
{
    public class Settings : SerializedRawObject
    {
        public enum AuthorizationLevel
        {
            NOBODY,
            SUBSCRIBERS,
            ANYONE
        }

        [Serialized] public AuthorizationLevel authPostAlerts = AuthorizationLevel.SUBSCRIBERS;

        [Serialized] public AuthorizationLevel authSpawnGuests = AuthorizationLevel.SUBSCRIBERS;

        [Serialized] public AuthorizationLevel authTwitchFeedGuestThoughts = AuthorizationLevel.SUBSCRIBERS;

        [Serialized] public bool defaultGuestSpawning = true;

        [Serialized] public bool subscriptionNotification = true;

        [Serialized] public string twitchChannelName = "";


        [Serialized] public string twitchOAuthToken = "";

        [Serialized] public bool twitchSpawnGuestNotification = true;

        [Serialized] public string twitchUsername = "";
    }
}