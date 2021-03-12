using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using Ninject;
using Ninject.MockingKernel.FakeItEasy;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;
using TeasmCompanion.Stores.Imap;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;
using TeasmCompanion.Misc;
using MailKit.Search;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v1.users.me.conversations;
using Newtonsoft.Json;

#nullable enable

namespace TeasmCompanion.Test.ImapStoreTests
{
    [TestClass]
    public class TestImapStore : TestBase
    {
        TeamsDataContext fakeContext;
        Configuration config = GetConfig();
        Random rand = new Random();

        [TestInitialize]
        public void Initialize()
        {
            fakeContext = new TeamsDataContext((TeamsParticipant)"00000000-0000-beef-0000-000000000000", new ProcessedTenant(new Tenant() { tenantId = "00000000-0000-feeb-0000-000000000000", userId = "00000000-0000-beef-0000-000000000000", tenantName = "Fake Tenant" }, DateTime.UtcNow));
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public async Task TestWriteAndReadUser()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ITeamsUserStore>().To<ImapStore>().InSingletonScope();
            var imapStore = kernel.Get<ITeamsUserStore>();

            var userName = new Random().Next().ToString();
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid");
            user.RegisterAlternateDisplayName(userName, DateTime.UtcNow);
            user.RegisterAlternateEmailAddress("heinrich.ulbricht@localhost", DateTime.UtcNow);
            await imapStore.PersistUserAsync(fakeContext, user);
            var users = await imapStore.RetrieveUsersAsync(fakeContext);
            Assert.AreEqual(1, users.Where(u => u.DisplayName == userName).Count(), "Cannot find previously saved user");
        }

        [TestMethod]
        public async Task TestConnectionCachingForStoreUserOperation()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ITeamsUserStore>().To<ImapStore>().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var factory = kernel.Get<ImapConnectionFactory>();
            var factoryWrap = A.Fake<ImapConnectionFactory>(a => a.Wrapping(factory));
            kernel.Rebind<ImapConnectionFactory>().ToConstant(factoryWrap);

            var imapStore = kernel.Get<ITeamsUserStore>();
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid");
            user.RegisterAlternateDisplayName("Heinrich Ulbricht", DateTime.UtcNow);
            user.RegisterAlternateEmailAddress("heinrich.ulbricht@localhost", DateTime.UtcNow);
            var user2 = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid");
            user2.RegisterAlternateDisplayName("Heinrich Ulbricht", DateTime.UtcNow);
            user2.RegisterAlternateEmailAddress("heinrich.ulbricht@localhost", DateTime.UtcNow);
            await Task.Run(() =>
            {
                imapStore.PersistUserAsync(fakeContext, user).Wait();
                imapStore.PersistUserAsync(fakeContext, user2).Wait();
            });
            A.CallTo(() => factoryWrap.GetImapConnectionAsync()).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        public async Task TestWriteAndReadManyUsers()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ITeamsUserStore>().To<ImapStore>().InSingletonScope();
            var imapStore = kernel.Get<ITeamsUserStore>();

            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < 10; i++)
            {
                var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)i.ToString());
                user.RegisterAlternateDisplayName("Massentest", DateTime.UtcNow);
                user.RegisterAlternateEmailAddress("heinrich.ulbricht@localhost", DateTime.UtcNow);
                await imapStore.PersistUserAsync(fakeContext, user);
            }
            var users = await imapStore.RetrieveUsersAsync(fakeContext);
            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds < 10000, "Needing quite a lot of time to store users");
        }

        [TestMethod]
        public async Task TestStoreAndGetAllChatMetadata()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ITeamsChatStore>().To<ImapStore>().InSingletonScope();
            var chatStore = kernel.Get<ITeamsChatStore>();

            var chat1 = new ProcessedChat(new TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users.Chat() { id="chat1", version = 10, threadVersion = 1000 });
            await chatStore.StoreMailThreadAndUpdateMetadataAsync(fakeContext, "TestGetAllChatMetadata_1", chat1, null);
            
            var chat2 = new ProcessedChat(new TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users.Chat() { id = "chat2", version = 20, threadVersion = 2000 });
            await chatStore.StoreMailThreadAndUpdateMetadataAsync(fakeContext, "TestGetAllChatMetadata_2", chat2, null);

            //await chatStore.GetAllChatMetadataRecursivelyAsync(fakeContext);

            //Assert.IsTrue(sw.ElapsedMilliseconds < 10000, "Needing quite a lot of time to store users");
        }

        [TestMethod]
        public async Task TestStoreAndRetrieveChat()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ITeamsChatStore>().To<ImapStore>().InSingletonScope();
            var chatStore = kernel.Get<ITeamsChatStore>();

            var chatId = $"chatId-{rand.Next()}";
            var chat1 = new ProcessedChat(new TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users.Chat() { id = chatId, version = 10, threadVersion = 1000 });
            await chatStore.StoreMailThreadAndUpdateMetadataAsync(fakeContext, "TestGetAllChatMetadata_1", chat1, null);

            var metadata = await chatStore.GetChatMetadataAsync(fakeContext, false, chatId);
            Assert.IsNotNull(metadata);
        }

        [TestMethod]
        public async Task TestHandlingOfConnectionErrors()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var conFac = kernel.Get<ImapConnectionFactory>();

            // note: for docker-imap-devel the connection limit per IP is 10, for Google it is 15 according to their documentation
            var connectionCount = 30;
            var tasks = new List<Task>();
            var counter = 0;
            var rand = new Random();

            for (var i = 0; i < connectionCount; i++)
            {
                var task = Task.Run(async () =>
                {
                    await Task.Delay(rand.Next(500));
                    var imapClient = await conFac.GetImapConnectionAsync();
                    var folder = await imapClient.GetFolderAsync(imapClient.PersonalNamespaces[0].Path);
                    await Task.Delay(rand.Next(1000, 3000));
                    await imapClient.DisconnectAsync(true);
                    lock(tasks)
                    {
                        counter++;
                    }
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            Assert.AreEqual(connectionCount, counter);
        }

        [TestMethod]
        public async Task TestModifyingExistingMessage()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var conFac = kernel.Get<ImapConnectionFactory>();
            var logger = kernel.Get<ILogger>();

            var imapClient = await conFac.GetImapConnectionAsync();
            var folder = imapClient.Inbox;
            await folder.OpenAsync(MailKit.FolderAccess.ReadWrite);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            message.To.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            message.Subject = "Delete me";

            var id = await folder.AppendAsync(message);
            var readMessage = await folder.GetMessageAsync(id.Value);

            var kvStore = new EmailBackedKeyValueStore(logger, readMessage);
            kvStore.SetTextContent("test text");
            kvStore.SetHtmlContent("<div>test html</div>");
            kvStore.SetJson("jsonValue", "{}");
            kvStore.Set("object", new { Data = "test data" });

            var newId = await folder.ReplaceAsync(id.Value, readMessage);
            Assert.IsTrue(newId.HasValue);

            await folder.SetFlagsAsync(newId.Value, MailKit.MessageFlags.Deleted, true);
            await folder.ExpungeAsync();
        }

        [TestMethod]
        public async Task TestModifyingExistingMessage2()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var conFac = kernel.Get<ImapConnectionFactory>();
            var logger = kernel.Get<ILogger>();

            var imapClient = await conFac.GetImapConnectionAsync();
            var folder = imapClient.Inbox;
            await folder.OpenAsync(MailKit.FolderAccess.ReadWrite);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            message.To.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            message.Subject = "Delete me";

            var id = await folder.AppendAsync(message);
            var readMessage = await folder.GetMessageAsync(id.Value);

            var wrap = new MimeMessageWrapper(logger, readMessage);
            wrap.TextContent = "test text";
            wrap.HtmlContent = "HTML content";
            wrap.MessageSubject = "Delete me - changed subject";

            var newId = await folder.ReplaceAsync(id.Value, readMessage);
            Assert.IsTrue(newId.HasValue);

            await folder.SetFlagsAsync(newId.Value, MailKit.MessageFlags.Deleted, true);
            await folder.ExpungeAsync();
        }

        [TestMethod]
        public async Task TestVisitMessages()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            kernel.Rebind<ITeamsChatStore>().To<ImapStore>().InSingletonScope();
            var conFac = kernel.Get<ImapConnectionFactory>();
            var logger = kernel.Get<ILogger>();
            var imapStore = kernel.Get<ITeamsChatStore>();


            var imapClient = await conFac.GetImapConnectionAsync();
            var rootFolder = await imapClient.GetFolderAsync(imapClient.PersonalNamespaces[0].Path);
            var mainUserFolder = await rootFolder.CreateAsync(fakeContext.MainUserId.Id.MakeSafeFolderName(rootFolder.DirectorySeparator), false);
            var tenantFolder = await mainUserFolder.CreateAsync("Fake Tenant", false);
            var chatsFolder = await tenantFolder.CreateAsync(Constants.TenantChatsFolderName, false);
            var chatFolder = await chatsFolder.CreateAsync("chat title or someting", false);
            await chatFolder.OpenAsync(MailKit.FolderAccess.ReadWrite);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            message.To.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            message.Subject = "Delete me User {{00000000-0000-beef-0000-000000000000}}";

            var kvStore = new EmailBackedKeyValueStore(logger, message);
            kvStore.Set("originalMessage", new Message() { version = "123", content = "message content" });

            var id = await chatFolder.AppendAsync(message);
            await imapStore.VisitMissingUserDisplayNames(fakeContext, async message =>
            {
                message.MessageSubject = "Delete me - Changed subject";
                message.TextContent = "new text content";
                message.HtmlContent = "new html content";
                return await Task.FromResult((true, new List<string>()));
            });

            var newId = (await chatFolder.SearchAsync(SearchQuery.All)).First();
            var changedMessage = await chatFolder.GetMessageAsync(newId);
            Assert.AreEqual("Delete me - Changed subject", changedMessage.Subject);

            await chatFolder.SetFlagsAsync(newId, MailKit.MessageFlags.Deleted, true);
            await chatFolder.ExpungeAsync();
        }

        [TestMethod]
        public async Task TestMimeMessageGenerationForMessageThatOnceFailed()
        {
            var fakeUserRegistry = A.Fake<ITeamsUserRegistry>();

            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            kernel.Rebind<ITeamsUserRegistry>().ToConstant(fakeUserRegistry);
            var imapStore = kernel.Get<ImapStore>();

            var json = "{\"id\":\"1111111111111\",\"sequenceId\":1111,\"clientmessageid\":\"1111111111111111111\",\"version\":\"1111111111111\",\"conversationid\":\"19:meeting_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@thread.v2\",\"conversationLink\":\"https://emea.ng.msg.teams.microsoft.com/v1/users/ME/conversations/19:meeting_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@thread.v2\",\"type\":\"Message\",\"messagetype\":\"Text\",\"contenttype\":\"text\",\"content\":\"aaa, aaaaaaaaaa aaaaaaaaaaa! \",\"amsreferences\":[],\"from\":\"https://emea.ng.msg.teams.microsoft.com/v1/users/ME/contacts/8:teamsvisitor:a111a1a111aa111a1aa1aa1a11aaaaa1\",\"imdisplayname\":\"aaa aaaaaaa (aaaa)\",\"composetime\":\"2000-01-01T01:01:01.111Z\",\"originalarrivaltime\":\"2000-01-01T01:01:01.111Z\",\"properties\":{}}";
            var message = JsonConvert.DeserializeObject<Message>(json);
            var processedMessage = kernel.Get<ProcessedMessage>();
            await processedMessage.InitFromMessageAsync(fakeContext, message.conversationid, message);

            // about "PrivateObject" see here: https://github.com/Microsoft/testfx/issues/366#issuecomment-580147403
            var imapStorePrivate = new PrivateObject(imapStore);
            var result = (Task<MimeMessage>)imapStorePrivate.Invoke("CreateMimeMessageFromChatMessageAsync", processedMessage, null);
            var realResult = result.Result;
        }
    }
}
