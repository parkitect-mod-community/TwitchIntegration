using System;
using System.Collections.Generic;
using System.Threading;

namespace TwitchIntegration
{
    public class TwitchGuestAssocCollection
    {
        private Dictionary<string, Guest> twitchUserGuestAssoc = new Dictionary<string, Guest> ();
        private Mutex guestAssocLock = new Mutex();




        public TwitchGuestAssocCollection ()
        {
        }

        public void RegisterUser(TwitchUser user,Guest g)
        {
            guestAssocLock.WaitOne ();
            if (twitchUserGuestAssoc.ContainsKey (user.name)) {
                
                twitchUserGuestAssoc.Remove (user.name);
            }
            twitchUserGuestAssoc.Add (user.name, g);
            guestAssocLock.ReleaseMutex ();
        }

        public Guest GetGuest(TwitchUser user)
        {
            guestAssocLock.WaitOne ();
            Guest guest = null;
            if (twitchUserGuestAssoc.ContainsKey (user.name)) {
                if (twitchUserGuestAssoc [user.name] == null) {
                    twitchUserGuestAssoc.Remove (user.name);

                } else {
                    guest = twitchUserGuestAssoc [user.name];
                }
            } 
            guestAssocLock.ReleaseMutex ();
            return guest;

        }

        public  Dictionary<string, Guest>.ValueCollection GetGuests()
        {
            return twitchUserGuestAssoc.Values;
        }


    }
}

