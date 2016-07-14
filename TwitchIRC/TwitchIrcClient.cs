using System;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace TwitchIntegration
{
    public class TwitchIrcClient
    {
        private const string TWITCH_URI = "irc.chat.twitch.tv";

        private const int TWITCH_SSL_PORT = 443;
        private const int TWITCH_PORT = 6667;
        private bool isMod = false;

        private Stopwatch stopWatch = new Stopwatch ();
        private Mutex sendingMutex = new Mutex ();
        private EventWaitHandle sendingHandle = new AutoResetEvent (false);
        private int messageCount = 0;


        private bool stopThreads = false;
        private Queue<String> commandQueue = new Queue<string> ();

        private Thread SendingThread;
        private Thread ReceivingThread;

        private StreamReader reader;
        private StreamWriter writer;
        private TcpClient tcp;

        public TwitchUser localUser{ get; private set; }

       
        //public event EventHandler<OnDisconnect> Disconnected;

        //public event EventHandler<OnRegistered> Registered;

        //public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler<OnConnectedArgs> Connected;
        public event EventHandler<NoticeMessageArg> NoticeMessage;
        public event EventHandler<TwitchMessage> OnMessage;
        public event EventHandler<UserChannelArgs> OnJoinChannel;
        public event EventHandler<UserChannelArgs> OnLeaveChannel;


        public bool IsConnected{ 
            get 
            {
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

                if ((stopWatch.ElapsedMilliseconds) / 1000.0f > 30) {
                    messageCount = 0;
                    stopWatch.Reset ();
                    stopWatch.Start ();

                }

                if (messageCount < 100 && isMod || messageCount < 20) {

                    sendingMutex.WaitOne ();
                    if (commandQueue.Count > 0) {

                        UnityEngine.Debug.Log ("SENDING------" + commandQueue.Peek ());
                        writer.WriteLine (commandQueue.Peek ());
                        writer.Flush ();

                        commandQueue.Dequeue ();
                        sendingMutex.ReleaseMutex ();
                    } else {
                        sendingMutex.ReleaseMutex ();
                        sendingHandle.WaitOne ();
                    }



                    messageCount++;

                } else {
                    long diff = 30 * 1000 - stopWatch.ElapsedMilliseconds;
                    if (diff > 0) {
                        Thread.Sleep ((int)diff);
                        //sleep until the limit is reached and then try to send
                    }
                }
            }
               
        }

        private void ReceivingThreadProcess ()
        {
            while (!stopThreads) {
                string line = reader.ReadLine ();
                string[] del = line.Split (' ');

                UnityEngine.Debug.Log (line);

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
                        string payload = line.Substring (line.IndexOf (':', line.IndexOf (channel) + channel.Length) + 1);   
                        if (OnMessage != null)
                            OnMessage (this, new TwitchMessage (line, arguments, new IrcChannel (channel), user, payload));
                    }
                    break;
                 case "JOIN":
                    {
                        index++;
                        string channel = del [index].Substring (1);
                        if (OnJoinChannel != null)
                            OnJoinChannel (this, new UserChannelArgs (user, new IrcChannel (channel)));
                    }
                    break;
                case "PART":
                    {
                        index++;
                        string channel = del [index].Substring (1);
                        if (OnLeaveChannel != null)
                            OnLeaveChannel (this, new UserChannelArgs (user, new IrcChannel (channel)));
                    }
                    break;
                case "MODE":
                    {
                        //TODO: track channels bot is subscibed to
                        index++;
                        string channel = del [index].Substring (1);
                        index++;
                        string op = del [index];
                        index++;
                        TwitchUser localUser = new TwitchUser(del [index]);
                        UnityEngine.Debug.Log ("you are a mod! your rate limit is now 100");
                        if (localUser.Equals(this.localUser) && op == "+o") {
                            isMod = true;
                        } else if (localUser.Equals(this.localUser) && op == "-o") {
                            isMod = false;
                        }
                    }
                    break;
                case "NOTICE":
                    {
                    index++;
                    string channel = del [index].Substring (1);
                    if (NoticeMessage != null)
                            NoticeMessage (this,new NoticeMessageArg(arguments,new IrcChannel(channel)));
                }
                    break;
                case "ROOMSTATE":
                    //TODO: needs to be implemented
                    break;
                case "USERSTATE":
                    //TODO: needs to be implemented
                    break;
                case "USERNOTICE":
                    //TODO: needs to be implemented
                    break;

                case "CAP":
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
            }
            UnityEngine.Debug.Log ("closing sending thread");
        }

        public void Disconnect()
        {
            stopThreads = true;
            sendingHandle.Set ();
            tcp.Close ();

        }

        public void SendCommand (string command)
        {
            sendingMutex.WaitOne ();
            commandQueue.Enqueue (command);
            sendingMutex.ReleaseMutex ();
            sendingHandle.Set ();
            
        }


        public void SendMessage (IrcChannel channel, string message)
        {
            if(!TwitchIrcGlobal.blockMessages)
                SendCommand ("PRIVMSG #" + channel.channel + " :" + message);
            
        }

        public void SendMessagePrivate (IrcChannel channel,TwitchUser user, string message)
        {
            if(!TwitchIrcGlobal.blockMessages)
                SendCommand ("PRIVMSG #" + channel.channel + " :/w" +" "+user.name+" " + message);

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

