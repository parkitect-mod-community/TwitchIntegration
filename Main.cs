using System.Linq;
using UnityEngine;

namespace TwitchIntegration {
	public class Main : IMod, IModSettings {
        public static TwitchIntegration instance;
        public static Configruation configuration;

		public void onEnabled() {
            if (configuration == null) {
                configuration = new Configruation ();
                configuration.Load ();
            }
			
            GameObject go = new GameObject(Name);
			instance = go.AddComponent<TwitchIntegration>();
		}
		
		public void onDisabled() {
			if (instance != null) {
				Object.Destroy(instance.gameObject);
				instance = null;
			}
		}

		public void onDrawSettingsUI() {
            configuration.DrawGUI ();
		}

		public void onSettingsOpened() {
            if (configuration == null)
                configuration = new Configruation ();
            configuration.Load ();
            
        }
		public void onSettingsClosed() {
            configuration.Save ();
        }
		
		public string Name => "Twitch Integration";

		public string Identifier => "TwitchIntegration";

		public string Description => "Allows viewers of your Twitch livestream to interact with the game.";
		public string Path
		{
			get
			{
				return ModManager.Instance.getModEntries().First(x => x.mod == this).path;
			}
		}
	}
}