using MailKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ninject;
using Ninject.MockingKernel.FakeItEasy;
using Serilog;
using System;
using System.Threading.Tasks;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;
using TeasmCompanion.Stores.Imap;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;

#nullable enable

namespace TeasmCompanion.Test.ImapStoreTests
{
    [TestClass]
    public class TestEmailBackedDatabase : TestBase
    {
        TeamsDataContext fakeContext;
        Configuration config = GetConfig();
        Random rand = new Random();

        [TestInitialize]
        public void Initialize()
        {
            fakeContext = new TeamsDataContext((TeamsParticipant)"test_fakemainuser", new ProcessedTenant(new Tenant() { tenantId = "Tenant ID", userId = "User ID", tenantName = "Fake User" }, DateTime.UtcNow));
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public async Task TestReadOnlyAccess()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapBackedDatabase>().ToSelf().InSingletonScope();
            var logger = kernel.Get<ILogger>();
            var fac = kernel.Get<ImapConnectionFactory>();
            var remoteLock = kernel.Get<ImapBackedRemoteLock>();

            var db = new ImapBackedDatabase(logger, fac, remoteLock);
            var folderName = "TestEmailBackedDatabase_TestReadOnlyAccess";
            ImapTestUtils.RemoveFolder(folderName, config);
            ImapTestUtils.CreateFolder(folderName, config);

            var imapClient = await fac.GetImapConnectionAsync();
            try
            {
                var testheadervalue = rand.Next().ToString();
                var store = await db.GetStoreForReading(folderName, "Fictional Store", message => { message.Subject = "[META] Fictional Store"; message.Headers.Add("testheaderkey", testheadervalue); });

                var folder = await imapClient.GetFolderAsync(folderName);
                folder.Open(FolderAccess.ReadOnly);
                var metaMessage = await folder.GetMessageAsync(store.MessageAndId.UniqueId);
                Assert.IsTrue(metaMessage.Headers.Contains("testheaderkey"));
                Assert.AreEqual(testheadervalue, metaMessage.Headers["testheaderkey"]);
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }

        [TestMethod]
        public async Task TestWriteAccess()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapBackedDatabase>().ToSelf().InSingletonScope();
            var logger = kernel.Get<ILogger>();
            var fac = kernel.Get<ImapConnectionFactory>();
            var remoteLock = kernel.Get<ImapBackedRemoteLock>();

            var db = new ImapBackedDatabase(logger, fac, remoteLock);
            var folderName = "TestEmailBackedDatabase_TestWriteAccess";
            ImapTestUtils.RemoveFolder(folderName, config);
            ImapTestUtils.CreateFolder(folderName, config);

            var imapClient = await fac.GetImapConnectionAsync();
            try
            {
                int dbValue = rand.Next();
                var (kvStore, lockResult) = await db.LockStoreForWriting(folderName, "Fictional Store");
                kvStore.Set("dbkey", dbValue);
                var uid = await db.WriteAndUnlockStore(folderName, kvStore, lockResult);
                Assert.IsTrue(uid.HasValue && uid.Value.IsValid);

                var readOnlyStore = await db.GetStoreForReading(folderName, "Fictional Store");
                var prevValue = readOnlyStore.GetOrCreateEmpty<int>("dbkey");
                Assert.AreEqual(dbValue, prevValue.AsObject);
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }
    }
}
