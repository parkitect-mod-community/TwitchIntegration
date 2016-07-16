using System;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace TwitchIntegration
{
    public class TwitchIrcClient
    {
        private const string TWITCH_URI = "irc.chat.twitch.tv";

        private const int TWITCH_SSL_PORT = 443;
        private const int TWITCH_PORT = 6667;
        private const float TIME_LIMIT = 30f;

        private bool isMod = false;


        private Stopwatch stopWatch = new Stopwatch ();
        private Mutex commandQueueMutex = new Mutex ();
        private Mutex MessageQueueMutex = new Mutex ();
        private EventWaitHandle sendingHandle = new AutoResetEvent (false);
        private int messageCount = 0;

        private bool stopThreads = false;

        private Queue<String> commandQueue = new Queue<string> ();
        private SortedList<MessagePriority,string> messageQueue = new SortedList<MessagePriority, string> (100, new MessageSort ());

        private Thread SendingThread;
        private Thread ReceivingThread;

        private StreamReader reader;
        private StreamWriter writer;
        private TcpClient tcp;

        public TwitchUser localUser{ get; private set; }

        public int messageLimit {
            get { 
                if (isMod)
                    return 100;
                return 20;
            }
        }

       
        //public event EventHandler<OnDisconnect> Disconnected;

        //public event EventHandler<OnRegistered> Registered;

        //public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler<OnConnectedArgs> Connected;
        public event EventHandler<NoticeMessageArg> NoticeMessage;
        public event EventHandler<TwitchMessage> OnMessage;
        public event EventHandler<SubscribeArgs> OnSubscribe;
        public event EventHandler<UserChannelArgs> OnJoinChannel;
        public event EventHandler<UserChannelArgs> OnLeaveChannel;


        public bool IsConnected { 
            get {
                if (tcp == null)
                    return false;
                return tcp.Connected;
            
            }
        }


        public TwitchIrcClient ()
        {

            
        }

        public void Connect (bool useSSL, string name, string token)
        {
            stopThreads = false;
            tcp = new TcpClient ();
            localUser = new TwitchUser (name, token);

            if (useSSL) {
                tcp.Connect (TWITCH_URI, TWITCH_SSL_PORT);
                SslStream stream = new SslStream (tcp.GetStream ());
                (stream as SslStream).AuthenticateAsClient (TWITCH_URI);
                reader = new StreamReader (stream);
                writer = new StreamWriter (stream);
            } else {
                tcp.Connect (TWITCH_URI, TWITCH_PORT);
                reader = new StreamReader (tcp.GetStream ());
                writer = new StreamWriter (tcp.GetStream ());
            }

            //send the password and name
            SendCommand ("PASS oauth:" + token);
            SendCommand ("NICK " + name);

            SendingThread = new Thread (new ThreadStart (SendingThreadProcess));
            SendingThread.Start ();

            ReceivingThread = new Thread (new ThreadStart (ReceivingThreadProcess));
            ReceivingThread.Start ();
        }


        private void SendingThreadProcess ()
        {
            stopWatch.Start ();
            //needs rate limiting
            while (!stopThreads) {
                try {
                    if ((stopWatch.ElapsedMilliseconds) / 1000.0f >= 30) {
                        messageCount = 0;
                        stopWatch.Reset ();
                        stopWatch.Start ();
                

                    }

                    while (messageCount < messageLimit) {
                  
                        commandQueueMutex.WaitOne ();
                        if (commandQueue.Count > 0) {
                            writer.WriteLine (commandQueue.Peek ());
                            commandQueue.Dequeue ();
                        } else {
                            commandQueueMutex.ReleaseMutex ();
                            break;
                        }
                        commandQueueMutex.ReleaseMutex ();
                        messageCount++;
                    }


                    int messageLimitPerCycle = Mathf.FloorToInt (messageLimit / TIME_LIMIT);
                    if(messageLimitPerCycle == 0)
                        messageLimitPerCycle = 1;
                    while (messageCount < messageLimit && messageLimitPerCycle > 0) {

                        MessageQueueMutex.WaitOne ();
                        if (messageQueue.Count > 0) {

                            if (messageQueue.Keys [0].isTimeExausted ()) {

                                UnityEngine.Debug.Log("dropped message:" + messageQueue.Values[0]);
                                messageQueue.RemoveAt (0);
                                return;
                            }


                            writer.WriteLine (messageQueue.Values [0]);
                            messageQueue.RemoveAt (0);
                            messageLimitPerCycle--;
                            messageCount++;
                        } else {
                            MessageQueueMutex.ReleaseMutex ();
                            break;
                        }
                        MessageQueueMutex.ReleaseMutex ();


                    }
                    //sleep for a second and then proceede to the next sending cycle
                    Thread.Sleep (1000);

                    writer.Flush ();
                    MessageQueueMutex.WaitOne ();
                    int messageQueueCount = messageQueue.Count;
                    MessageQueueMutex.ReleaseMutex ();

                    commandQueueMutex.WaitOne ();
                    int commandQueueCount = commandQueue.Count;
                    commandQueueMutex.ReleaseMutex ();

                    if (commandQueueCount == 0 && messageQueueCount == 0)
                        sendingHandle.WaitOne ();
                

                    //if count is exausted then go to sleep and wait for the next sending cycle
                    if (messageCount > messageLimit) {
                        long diff = 30 * 1000 - stopWatch.ElapsedMilliseconds;
                        if (diff > 0) {
                            UnityEngine.Debug.Log("hit message limit sleeping for:" + diff);
                            Thread.Sleep ((int)diff);
                            //sleep until the limit is reached and then try to send
                        }
                    }
                } catch (Exception e) {
                    UnityEngine.Debug.Log (e.Message);
                }

            }
               
        }

        private void ReceivingThreadProcess ()
        {
            while (!stopThreads) {
                try {
                    string line = reader.ReadLine ();
                    string[] del = line.Split (' ');

                    //UnityEngine.Debug.Log (line);

                    List<KeyValuePair<string,string>> arguments = new List<KeyValuePair<string, string>> ();

                    int index = 0;
                    //has arguments
                    if (del [index].StartsWith ("@")) {
                        string[] keyPairs = del [index].Substring (1).Split (';');
                        for (int x = 0; x < keyPairs.Length; x++) {
                            string[] temp = keyPairs [x].Split ('=');
                            arguments.Add (new KeyValuePair<string, string> (temp [0], temp [1]));
                        }
                        index++;
                    }

                    TwitchUser user = null; 
                    if (del [index].Contains ("!")) {
                        user = new TwitchUser (del [index].Split ('!') [0].Substring (1));
                        index++;
                    } else if (del [index].StartsWith (":")) {
                        index++;
                    }

                    //protocol
                    switch (del [index]) {
                    case "PRIVMSG":
                        {
                            index++;
                            string channel = del [index];
                            index++;
                            string payload = GetMessage (index, del);
                            if (OnMessage != null)
                                OnMessage (this, new TwitchMessage (line, arguments, new IrcChannel (channel), user, payload));
                        }
                        break;
                    case "JOIN":
                        {
                            index++;
                            string channel = del [index];
                            if (OnJoinChannel != null)
                                OnJoinChannel (this, new UserChannelArgs (user, new IrcChannel (channel)));
                        }
                        break;
                    case "PART":
                        {
                            index++;
                            string channel = del [index];
                            if (OnLeaveChannel != null)
                                OnLeaveChannel (this, new UserChannelArgs (user, new IrcChannel (channel)));
                        }
                        break;
                    case "MODE":
                        {
                            //TODO: track channels bot is subscibed to
                            index++;
                            string channel = del [index];
                            index++;
                            string op = del [index];
                            index++;
                            TwitchUser localUser = new TwitchUser (del [index]);
                            UnityEngine.Debug.Log ("you are a mod! your rate limit is now 100");
                            if (localUser.Equals (this.localUser) && op == "+o") {
                                isMod = true;
                            } else if (localUser.Equals (this.localUser) && op == "-o") {
                                isMod = false;
                            }
                        }
                        break;
                    case "NOTICE":
                        {
                            index++;
                            string channel = del [index].Substring (1);
                            if (NoticeMessage != null)
                                NoticeMessage (this, new NoticeMessageArg (arguments, new IrcChannel (channel)));
                        }
                        break;
                    case "ROOMSTATE":
                    //TODO: needs to be implemented
                        break;
                    case "USERSTATE":
                    //TODO: add user state
                        break;
                    case "USERNOTICE":
                        {
                            index++;
                            string channel = del [index];
                            index++;
                            string payload = GetMessage (index, del);
                            if (OnSubscribe != null)
                                OnSubscribe (this, new SubscribeArgs (line, arguments, new IrcChannel (channel), payload));
                        }
                        break;

                    case "CAP":
                        {
                            //TODO: CAP case
                            index++;
                            index++;
                            if (del [index] == "ACK") {
                                index++;
                                string payload = del [index].Substring (1);
                                switch (payload) {
                                case "twitch.tv/commands":
                                    break;
                                }
                            }
                        }
                        break;
                    case "372":
                        if (Connected != null)
                            Connected.Invoke (this, new OnConnectedArgs ());
                        break;
                    case "421":
                        UnityEngine.Debug.Log (line);
                        break;

                    }

                    if (line.StartsWith ("PING ")) {
                        SendCommand (line.Replace ("PING", "PONG"));
                    }
                } catch (Exception e) {
                    UnityEngine.Debug.Log (e.Message);
                }
            }
            UnityEngine.Debug.Log ("closing sending thread");
        }

        private string GetMessage (int index, string[] del)
        {
            //skip because an empty string will block the thread
            if (index > del.Length - 1)
                return "";
            StringBuilder stringBuilder = new StringBuilder ();
            for (int x = index; x < del.Length; x++) {
                stringBuilder.Append (" ");
                stringBuilder.Append (del [x]);
            }

            return stringBuilder.ToString ().Trim ().Substring (1);
        }

        public void Disconnect ()
        {
            stopThreads = true;
            sendingHandle.Set ();
            tcp.Close ();

        }

        public void SendCommand (string command)
        {
            commandQueueMutex.WaitOne ();
            commandQueue.Enqueue (command);
            commandQueueMutex.ReleaseMutex ();
            sendingHandle.Set ();
            
        }


        public void SendMessage (IrcChannel channel, float timeToLive, int priority, string message)
        {
            MessageQueueMutex.WaitOne ();
            if (!TwitchIrcGlobal.blockMessages)
                messageQueue.Add (new MessagePriority (timeToLive, priority, Time.time), "PRIVMSG #" + channel.channel + " :" + message);
            MessageQueueMutex.ReleaseMutex ();
            sendingHandle.Set ();
        }

        public void SendMessagePrivate (IrcChannel channel, float timeToLive, int priority, TwitchUser user, string message)
        {
            MessageQueueMutex.WaitOne ();
            if (!TwitchIrcGlobal.blockMessages)
                messageQueue.Add (new MessagePriority (timeToLive, priority, Time.time), "PRIVMSG #" + channel.channel + " :/w" + " " + user.name + " " + message);
            MessageQueueMutex.ReleaseMutex ();
            sendingHandle.Set ();
        }

        /* public void SendPrivateMessage()
        {
        }*/

        /**
         * join the channel
         */
        public IrcChannel joinChannel (string channel)
        {
            SendCommand ("join #" + channel);
            return new IrcChannel (channel);
        }

        public void EnableMembership ()
        {
            SendCommand ("CAP REQ :twitch.tv/membership");
        }

        public void EnableCommands ()
        {
            SendCommand ("CAP REQ :twitch.tv/commands");
        }

        public void EnableTags ()
        {
            SendCommand ("CAP REQ :twitch.tv/tags");
        }



    }
}

