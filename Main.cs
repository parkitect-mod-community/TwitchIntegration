using System;
using UnityEngine;


namespace TwitchIntegration {
	public class Main : IMod, IModSettings {
        public static TwitchIntegration instance;
        public static Configruation configuration;

		public void onEnabled() {
            if (Main.configuration == null) {
                Main.configuration = new Configruation (this.Path);
                Main.configuration.Load ();
            }
			
            GameObject go = new GameObject(Name);
			instance = go.AddComponent<TwitchIntegration>();
		}
		
		public void onDisabled() {
			if (instance != null) {
				UnityEngine.Object.Destroy(instance.gameObject);
				instance = null;
			}
		}

		public void onDrawSettingsUI() {
            Main.configuration.DrawGUI ();
		}

		public void onSettingsOpened() {
            if (Main.configuration == null)
                Main.configuration = new Configruation (this.Path);
            Main.configuration.Load ();
            
        }
		public void onSettingsClosed() {
            Main.configuration.Save ();
        }
		
		public string Name {
			get { return "Twitch Integration"; }
		}

		public string Identifier {
            get; set;
		}

		public string Description {
			get { return "Allows viewers of your Twitch livestream to interact with the game."; }
		}

        public string Path{get;set;}
	}
}