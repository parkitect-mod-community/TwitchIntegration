using System;
using NUnit.Framework;
using System.Collections.Generic;
using TwitchIntegration;

namespace TwitchIntegrationTest
{
    [TestFixture ()]
    public class SortedMessageTest
    {
        [Test]
        public void TestTimeSort ()
        {
            SortedList<MessagePriority,string> messageQueue = new SortedList<MessagePriority, string> (100, new MessageSort ());
            messageQueue.Add(new MessagePriority(100,1,10),"a");
            messageQueue.Add(new MessagePriority(100,1,11),"b");
            messageQueue.Add(new MessagePriority(100,1,12),"c");
            messageQueue.Add(new MessagePriority(100,1,13),"d");
            messageQueue.Add(new MessagePriority(100,1,14),"e");
            messageQueue.Add(new MessagePriority(100,1,15),"f");

            Assert.AreEqual (messageQueue.Values [0], "a");
            Assert.AreEqual (messageQueue.Values [1], "b");
            Assert.AreEqual (messageQueue.Values [2], "c");
            Assert.AreEqual (messageQueue.Values [3], "d");
            Assert.AreEqual (messageQueue.Values [4], "e");
            Assert.AreEqual (messageQueue.Values [5], "f");
        }

        [Test]
        public void TestPriortySort ()
        {
            SortedList<MessagePriority,string> messageQueue = new SortedList<MessagePriority, string> (100, new MessageSort ());
            messageQueue.Add(new MessagePriority(100,5,10),"d");
            messageQueue.Add(new MessagePriority(100,5,11),"e");
            messageQueue.Add(new MessagePriority(100,2,12),"a");
            messageQueue.Add(new MessagePriority(100,2,13),"b");
            messageQueue.Add(new MessagePriority(100,5,14),"c");
            messageQueue.Add(new MessagePriority(100,5,15),"f");

            Assert.AreEqual (messageQueue.Values [0], "d");
            Assert.AreEqual (messageQueue.Values [1], "e");
            Assert.AreEqual (messageQueue.Values [2], "c");
            Assert.AreEqual (messageQueue.Values [3], "f");
            Assert.AreEqual (messageQueue.Values [4], "a");
            Assert.AreEqual (messageQueue.Values [5], "b");
        }

        [Test]
        public void TestPriortyMax ()
        {
            SortedList<MessagePriority,string> messageQueue = new SortedList<MessagePriority, string> (100, new MessageSort ());
            messageQueue.Add(new MessagePriority(100,5,10),"d");
            messageQueue.Add(new MessagePriority(100,5,11),"e");
            messageQueue.Add(new MessagePriority(100,2,12),"a");
            messageQueue.Add(new MessagePriority(100,2,13),"b");
            messageQueue.Add(new MessagePriority(100,5,14),"c");
            messageQueue.Add(new MessagePriority(100,5,15),"f");
            messageQueue.Add(new MessagePriority(100,-1,15),"g");

            Assert.AreEqual (messageQueue.Values [6], "g");
            Assert.AreEqual (messageQueue.Values [0], "d");
            Assert.AreEqual (messageQueue.Values [1], "e");
            Assert.AreEqual (messageQueue.Values [2], "c");
            Assert.AreEqual (messageQueue.Values [3], "f");
            Assert.AreEqual (messageQueue.Values [4], "a");
            Assert.AreEqual (messageQueue.Values [5], "b");
        }
    }
}

