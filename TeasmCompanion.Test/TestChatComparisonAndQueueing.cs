using Microsoft.VisualStudio.TestTools.UnitTesting;
using Priority_Queue;
using System;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;
using TeasmCompanion.TeamsMonitors;

namespace TeasmCompanion.Test
{
    [TestClass]
    public class TestChatComparisonAndQueueing : TestBase
    {
        [TestMethod]
        public void TestChatComparerSimple()
        {
            var lowVersion = new HigherVersionWinsComparerChat(new Chat() { version = 1 });
            var highVersion = new HigherVersionWinsComparerChat(new Chat() { version = 10 });

            Assert.AreEqual(-1, lowVersion.CompareTo(highVersion));
            Assert.AreEqual(1, highVersion.CompareTo(lowVersion));
            Assert.AreEqual(0, lowVersion.CompareTo(lowVersion));
            Assert.AreEqual(1, lowVersion.CompareTo(null));
        }

        [TestMethod]
        public void TestChatComparerWithLastMessage()
        {
            var highestVersion = new HigherVersionWinsComparerChat(new Chat() { version = 1000000 });
            var lastMessageLowVersion = new HigherVersionWinsComparerChat(new Chat() { version = 1, LastMessage = new Message() { version = "1" } });
            var lastMessageHighVersion = new HigherVersionWinsComparerChat(new Chat() { version = 1, LastMessage = new Message() { version = "10" } });

            // last message version always wins
            Assert.AreEqual(-1, highestVersion.CompareTo(lastMessageLowVersion));
            Assert.AreEqual(1, lastMessageLowVersion.CompareTo(highestVersion));

            // compare last message versions
            Assert.AreEqual(-1, lastMessageLowVersion.CompareTo(lastMessageHighVersion));
            Assert.AreEqual(1, lastMessageHighVersion.CompareTo(lastMessageLowVersion));
            Assert.AreEqual(0, lastMessageLowVersion.CompareTo(lastMessageLowVersion));
        }

        [TestMethod]
        public void TestChatComparerWithNulls()
        {
            var nullChat = new HigherVersionWinsComparerChat(null);
            var chat = new HigherVersionWinsComparerChat(new Chat() { version = 10 });

            // last message version always wins
            Assert.AreEqual(-1, nullChat.CompareTo(chat));
            Assert.AreEqual(1, chat.CompareTo(nullChat));
            Assert.AreEqual(0, nullChat.CompareTo(nullChat));
        }


        [TestMethod]
        public void TestChatPriorityQueue()
        {
            var chatsToRetrieve = new SimplePriorityQueue<(TeamsDataContext, HigherVersionWinsComparerChat), HigherVersionWinsComparerChat>(new LowerVersionWinsChatComparer());

            var dummyContext = new TeamsDataContext();
            var chatLowVersion = new HigherVersionWinsComparerChat(new Chat() { version = 5 });
            var chatMediumVersion = new HigherVersionWinsComparerChat(new Chat() { version = 50 });
            var chatHighVersion = new HigherVersionWinsComparerChat(new Chat() { version = 500 });

            // note: last message version _always_ comes before any "normal" thread version
            var chatWithLowestLastMessageVersion = new HigherVersionWinsComparerChat(new Chat() { version = 5000, LastMessage = new Message() { version = "1" } });
            var chatWithHighestLastMessageVersion = new HigherVersionWinsComparerChat(new Chat() { version = 1, LastMessage = new Message() { version = "50000" } });

            chatsToRetrieve.Enqueue((dummyContext, chatLowVersion), chatLowVersion);
            chatsToRetrieve.Enqueue((dummyContext, chatHighVersion), chatHighVersion);
            chatsToRetrieve.Enqueue((dummyContext, chatMediumVersion), chatMediumVersion);
            chatsToRetrieve.Enqueue((dummyContext, chatWithLowestLastMessageVersion), chatWithLowestLastMessageVersion);
            chatsToRetrieve.Enqueue((dummyContext, chatWithHighestLastMessageVersion), chatWithHighestLastMessageVersion);

            (TeamsDataContext, HigherVersionWinsComparerChat) chat;

            chat = chatsToRetrieve.Dequeue();
            Assert.AreEqual(1, chat.Item2.Chat.version);
            chat = chatsToRetrieve.Dequeue();
            Assert.AreEqual(5000, chat.Item2.Chat.version);
            chat = chatsToRetrieve.Dequeue();
            Assert.AreEqual(500, chat.Item2.Chat.version);
            chat = chatsToRetrieve.Dequeue();
            Assert.AreEqual(50, chat.Item2.Chat.version);
            chat = chatsToRetrieve.Dequeue();
            Assert.AreEqual(5, chat.Item2.Chat.version);
        }

        [TestMethod]
        public void TestQueueOperations()
        {
            var chatsToRetrieve = new SimplePriorityQueue<ChatQueueItem, HigherVersionWinsComparerChat>(new LowerVersionWinsChatComparer());

            var dummyContext = new TeamsDataContext((TeamsParticipant)"00000000-0000-beef-0000-000000000000", new ProcessedTenant(new Tenant(), DateTime.UtcNow));
            
            var chatLowVersion = new HigherVersionWinsComparerChat(new Chat() { version = 5, id = "1" });
            var chatMediumVersion = new HigherVersionWinsComparerChat(new Chat() { version = 50, id = "2" });
            var chatHighVersion = new HigherVersionWinsComparerChat(new Chat() { version = 500, id = "3" });

            chatsToRetrieve.Enqueue(new ChatQueueItem(dummyContext, chatLowVersion), chatLowVersion);
            chatsToRetrieve.Enqueue(new ChatQueueItem(dummyContext, chatMediumVersion), chatMediumVersion);
            chatsToRetrieve.Enqueue(new ChatQueueItem(dummyContext, chatHighVersion), chatHighVersion);

            var isRemoved = chatsToRetrieve.TryRemove(new ChatQueueItem(dummyContext, chatMediumVersion));
            Assert.IsTrue(isRemoved);
            isRemoved = chatsToRetrieve.TryRemove(new ChatQueueItem(dummyContext, chatMediumVersion));
            Assert.IsFalse(isRemoved);
        }
    }
}
