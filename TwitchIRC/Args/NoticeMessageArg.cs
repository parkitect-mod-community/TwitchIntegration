using System;
using System.Collections.Generic;

namespace TwitchIntegration
{
    public class NoticeMessageArg : EventArgs
    {
        public enum Notices{
            SubsOn,
            AlreadySubsOn,
            SubsOff,
            AlreadySubsOff,
            SlowOn,
            SlowOff,
            r9kOn,
            AlreadyR9kOn,
            r9kOff,
            AlreadyR9kOff,
            HostOn,
            badHostHosting,
            hostOff,
            hostsRemaining,
            emoteOnlyOn,
            alreadyEmoteOnlyOn,
            emoteOnlyOff,
            alreadyEmoteOnlyOff,
            msgChannelSuspended,
            timeoutSuccess,
            banSuccess,
            unbanSuccess,
            badUnbanNoBan,
            alreadyBanned,
            unrecognizedCmd
        }
        public Notices notice{get;private set;}
        public IrcChannel channel{get;private set;}


        public NoticeMessageArg (List<KeyValuePair<string,string>> arguments,IrcChannel channel)
        {
            this.channel = channel;
            foreach (KeyValuePair<string,string> arg in arguments) {
                if (arg.Key == "msg-id") {
                    switch (arg.Value) {
                    case "subs_on":
                        notice = Notices.SubsOn;
                        break;
                    case "already_subs_on":
                        notice = Notices.AlreadySubsOn;
                        break;
                    case "subs_off":
                        notice = Notices.SubsOff;
                        break;
                    case "already_subs_off":
                        notice = Notices.AlreadySubsOff;
                        break;
                    case "slow_on":
                        notice = Notices.SlowOn;
                        break;
                    case "slow_off":
                        notice = Notices.SlowOff;
                        break;
                    case "r9k_on":
                        notice = Notices.r9kOn;
                        break;
                    case "already_r9k_on":
                        notice = Notices.AlreadyR9kOn;
                        break;
                    case "r9k_off":
                        notice = Notices.r9kOff;
                        break;
                    case "already_r9k_off":
                        notice = Notices.AlreadyR9kOff;
                        break;
                    case "host_on":
                        notice = Notices.HostOn;
                        break;
                    case "bad_host_hosting":
                        notice = Notices.badHostHosting;
                        break;
                    case "host_off":
                        notice = Notices.hostOff;
                        break;
                    case "hosts_remaining":
                        notice = Notices.hostsRemaining;
                        break;
                    case "emote_only_on":
                        notice = Notices.emoteOnlyOn;
                        break;
                    case "already_emote_only_on":
                        notice = Notices.alreadyEmoteOnlyOn;
                        break;
                    case "emote_only_off":
                        notice = Notices.emoteOnlyOff;
                        break;
                    case "already_emote_only_off":
                        notice = Notices.alreadyEmoteOnlyOff;
                        break;
                    case "msg_channel_suspended":
                        notice = Notices.msgChannelSuspended;
                        break;
                    case "timeout_success":
                        notice = Notices.timeoutSuccess;
                        break;
                    case "ban_success":
                        notice = Notices.banSuccess;
                        break;
                    case "unban_success":
                        notice = Notices.unbanSuccess;
                        break;
                    case "bad_unban_no_ban":
                        notice = Notices.badUnbanNoBan;
                        break;
                    case "already_banned":
                        notice = Notices.alreadyBanned;
                        break;
                    case "unrecognized_cmd":
                        notice = Notices.unrecognizedCmd;
                        break;
                    }
                }
            }
        }
    }
}

