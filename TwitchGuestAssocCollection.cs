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

using System.Collections.Generic;
using System.Threading;

namespace TwitchIntegration
{
    public class TwitchGuestAssocCollection
    {
        private readonly Mutex guestAssocLock = new Mutex();
        private readonly Dictionary<string, Guest> twitchUserGuestAssoc = new Dictionary<string, Guest>();


        public void RegisterUser(TwitchUser user, Guest g)
        {
            guestAssocLock.WaitOne();
            if (twitchUserGuestAssoc.ContainsKey(user.name)) twitchUserGuestAssoc.Remove(user.name);
            twitchUserGuestAssoc.Add(user.name, g);
            guestAssocLock.ReleaseMutex();
        }

        public Guest GetGuest(TwitchUser user)
        {
            guestAssocLock.WaitOne();
            Guest guest = null;
            if (twitchUserGuestAssoc.ContainsKey(user.name))
            {
                if (twitchUserGuestAssoc[user.name] == null)
                    twitchUserGuestAssoc.Remove(user.name);
                else
                    guest = twitchUserGuestAssoc[user.name];
            }

            guestAssocLock.ReleaseMutex();
            return guest;
        }

        public Dictionary<string, Guest>.ValueCollection GetGuests()
        {
            return twitchUserGuestAssoc.Values;
        }
    }
}