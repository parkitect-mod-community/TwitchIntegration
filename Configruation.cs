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


using System;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
using UnityEngine;

namespace TwitchIntegration
{
    public class Configruation
    {
        public const string twitchClientID = "ogtnoqm4m86chl2oflq5myfznk8u5oq";
        public const string twitchRedirectUri = "http://www.themeparkitect.com/twitch_auth.html";
        private Page page = Page.STATUS;
        private readonly string path;
        private Vector2 scrollPosition;
        private Rect uiRect = new Rect(4, 30, 300, 300);

        public Configruation()
        {
            path = FilePaths.getFolderPath("twitch_integration.config");
            ;
            settings = new Settings();
            Debug.Log(path);
        }

        public Settings settings { get; }

        private void SetGlobalSettings()
        {
            Global.PERSON_SPAWN = settings.defaultGuestSpawning;
        }

        public void Save()
        {
            SetGlobalSettings();
            var context = new SerializationContext(SerializationContext.Context.Savegame);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(Json.Serialize(Serializer.serialize(context, settings)));
                }
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(path))
                    using (var reader = new StreamReader(path))
                    {
                        string jsonString;

                        var context = new SerializationContext(SerializationContext.Context.Savegame);
                        while ((jsonString = reader.ReadLine()) != null)
                        {
                            var values = (Dictionary<string, object>) Json.Deserialize(jsonString);
                            Serializer.deserialize(context, settings, values);
                        }

                        reader.Close();
                    }
            }
            catch (Exception e)
            {
                Debug.Log("Couldn't properly load settings file! " + e);
            }

            SetGlobalSettings();
        }

        public void DrawGUI()
        {
            //if (!drawGUI) {
            //return;
            //}

            //GUILayout.BeginArea(uiRect);
            //GUI.Box(new Rect(0, 0, uiRect.width, uiRect.height), "");
            //scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Status")) page = Page.STATUS;

            if (GUILayout.Button("Settings")) page = Page.SETTINGS;
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            if (page == Page.STATUS)
            {
                if (string.IsNullOrEmpty(settings.twitchUsername))
                {
                    GUILayout.Label("You need to enter a twitch username on the settings tab");
                }
                else if (string.IsNullOrEmpty(settings.twitchChannelName))
                {
                    GUILayout.Label("You need to enter a twitch channel on the settings tab");
                }
                else if (string.IsNullOrEmpty(settings.twitchOAuthToken))
                {
                    GUILayout.Label("You need to enter a twitch auth token on the settings tab");
                }
                else
                {
                    if (Main.instance != null)
                    {
                        if (!Main.instance.ircClient.IsConnected && GUILayout.Button("Connect"))
                            Main.instance.ircClient.Connect(false, Main.configuration.settings.twitchUsername,
                                Main.configuration.settings.twitchOAuthToken);
                        else if (Main.instance.ircClient.IsConnected && GUILayout.Button("Disconnect"))
                            Main.instance.ircClient.Disconnect();

                        foreach (var guest in Main.instance.collection.GetGuests())
                            if (guest != null)
                                if (GUILayout.Button(guest.getName()))
                                    GameController.Instance.cameraController.lockOnto(guest.gameObject);
                    }

                    // GUILayout.Label(ircLogString);
                }
            }
            else if (page == Page.SETTINGS)
            {
                GUILayout.Label("!alert: Sending notifications");
                drawAuthToggleGroup(ref settings.authPostAlerts);
                GUILayout.Space(20);

                GUILayout.Label("!spawn: Spawning a guest");
                drawAuthToggleGroup(ref settings.authSpawnGuests);
                GUILayout.Space(20);

                GUILayout.Label("Notification when a guest is spawned");
                drawAuthToggleGroup(ref settings.authSpawnGuests);
                GUILayout.Space(20);

                GUILayout.Label("Twitch feed and guest thoughts");
                drawAuthToggleGroup(ref settings.authTwitchFeedGuestThoughts);
                GUILayout.Space(20);


                //TODO:needs to be implemented
                settings.subscriptionNotification =
                    GUILayout.Toggle(settings.subscriptionNotification, "Subscription notification");
                GUILayout.Space(20);

                settings.twitchSpawnGuestNotification = GUILayout.Toggle(settings.twitchSpawnGuestNotification,
                    "Twitch guest spawning notification");
                GUILayout.Space(20);


                settings.defaultGuestSpawning =
                    GUILayout.Toggle(settings.defaultGuestSpawning, "Default guest spawning");
                GUILayout.Space(20);


                GUILayout.Label("Twitch user name");
                settings.twitchUsername = GUILayout.TextField(settings.twitchUsername);
                GUILayout.Space(20);

                GUILayout.Label("Twitch channel name");
                settings.twitchChannelName = GUILayout.TextField(settings.twitchChannelName);
                GUILayout.Space(20);

                GUILayout.Label("Twitch auth token");
                settings.twitchOAuthToken = GUILayout.PasswordField(settings.twitchOAuthToken, '*');

                if (GUILayout.Button("Request an auth token"))
                    Application.OpenURL(string.Format(
                        "https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id={0}&redirect_uri={1}&scope=chat_login+channel_check_subscription",
                        twitchClientID, twitchRedirectUri));
            }

            //GUILayout.EndScrollView();
            //GUILayout.EndArea();
        }


        private void drawAuthToggleGroup(ref Settings.AuthorizationLevel authLevel)
        {
            authLevel = GUILayout.Toggle(authLevel == Settings.AuthorizationLevel.NOBODY, "Nobody")
                ? Settings.AuthorizationLevel.NOBODY
                : authLevel;
            authLevel = GUILayout.Toggle(authLevel == Settings.AuthorizationLevel.SUBSCRIBERS, "Subscribers")
                ? Settings.AuthorizationLevel.SUBSCRIBERS
                : authLevel;
            authLevel = GUILayout.Toggle(authLevel == Settings.AuthorizationLevel.ANYONE, "Anyone")
                ? Settings.AuthorizationLevel.ANYONE
                : authLevel;
        }

        private enum Page
        {
            STATUS,
            SETTINGS
        }
    }
}