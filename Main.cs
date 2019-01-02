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

using System.Linq;
using UnityEngine;

namespace TwitchIntegration
{
    public class Main : IMod, IModSettings
    {
        public static TwitchIntegration instance;
        public static Configruation configuration;

        public string Path
        {
            get { return ModManager.Instance.getModEntries().First(x => x.mod == this).path; }
        }

        public void onEnabled()
        {
            if (configuration == null)
            {
                configuration = new Configruation();
                configuration.Load();
            }

            var go = new GameObject(Name);
            instance = go.AddComponent<TwitchIntegration>();
        }

        public void onDisabled()
        {
            if (instance != null)
            {
                Object.Destroy(instance.gameObject);
                instance = null;
            }
        }

        public string Name => "Twitch Integration";

        public string Identifier => "TwitchIntegration";

        public string Description => "Allows viewers of your Twitch livestream to interact with the game.";

        public void onDrawSettingsUI()
        {
            configuration.DrawGUI();
        }

        public void onSettingsOpened()
        {
            if (configuration == null)
                configuration = new Configruation();
            configuration.Load();
        }

        public void onSettingsClosed()
        {
            configuration.Save();
        }
    }
}