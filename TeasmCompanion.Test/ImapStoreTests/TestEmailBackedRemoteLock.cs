using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ninject.MockingKernel.FakeItEasy;
using Serilog;
using Ninject;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using MailKit;
using MailKit.Search;
using System.Diagnostics;
using TeasmCompanion.Stores.Imap;

namespace TeasmCompanion.Test.ImapStoreTests
{
    [TestClass]
    public class TestEmailBackedRemoteLock : TestBase
    {
        private Configuration config;
        private string testFolderName;
        private Random rand = new Random();
        private static TestContext Context;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Context = testContext;

        }
        [TestInitialize]
        public void Initialize()
        {
            config = GetConfig();
            testFolderName = $"TestEmailBackedRemoteLock_{DateTime.Now.ToString("yyyy-MM-dd HHmmss")}";
            ImapTestUtils.CreateFolder(testFolderName, config);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Context.CurrentTestOutcome == UnitTestOutcome.Passed)
            {
                ImapTestUtils.RemoveFolder(testFolderName, config);
            }
        }

        [TestMethod]
        public async Task TestAcquireAndReleaseLock()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();

            var imapFac = kernel.Get<ImapConnectionFactory>();
            var remoteLock = kernel.Get<ImapBackedRemoteLock>();

            ImapTestUtils.RemoveFolder("resourcelock", config);

            using var connection = await imapFac.GetImapConnectionAsync();
            var parentFolder = await connection.GetFolderAsync(connection.PersonalNamespaces[0].Path);
            var lockResult = await remoteLock.AcquireLock(parentFolder, "resourcelock");
            Assert.IsTrue(lockResult.IsSuccess);
            Assert.IsNotNull(lockResult.ResultingLockCookie);
            var unlockResult = await remoteLock.ReleaseLock(parentFolder, "resourcelock", lockResult.ResultingLockCookie.Value);
            Assert.IsTrue(unlockResult);
        }

        [TestMethod]
        public async Task TestUpdateLock()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();

            var imapFac = kernel.Get<ImapConnectionFactory>();
            var remoteLock = kernel.Get<ImapBackedRemoteLock>();
            remoteLock.LockTimeOut = TimeSpan.FromSeconds(5);

            using var connection = await imapFac.GetImapConnectionAsync();
            var parentFolder = await connection.GetFolderAsync(connection.PersonalNamespaces[0].Path);
            await remoteLock.ReleaseLock(parentFolder, "resourcelock", -1);
            var lockResult = await remoteLock.AcquireLock(parentFolder, "resourcelock");
            Assert.IsTrue(lockResult.IsSuccess);
            Assert.IsNotNull(lockResult.ResultingLockCookie);
            var lockResult2 = await remoteLock.AcquireLock(parentFolder, "resourcelock", lockResult.ResultingLockCookie);
            Assert.IsTrue(lockResult2.IsSuccess);
            Assert.IsNotNull(lockResult2.ResultingLockCookie);
            Assert.AreEqual(lockResult.ResultingLockCookie, lockResult2.ResultingLockCookie);

            var unlockResult = await remoteLock.ReleaseLock(parentFolder, "resourcelock", lockResult.ResultingLockCookie.Value);
            Assert.IsTrue(unlockResult);
        }

        [TestMethod]
        public async Task TestWaitForLockTimeout()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();

            var imapFac = kernel.Get<ImapConnectionFactory>();
            var remoteLock = kernel.Get<ImapBackedRemoteLock>();
            remoteLock.LockTimeOut = TimeSpan.FromSeconds(3);

            using var connection = await imapFac.GetImapConnectionAsync();
            var parentFolder = await connection.GetFolderAsync(connection.PersonalNamespaces[0].Path);
            await remoteLock.ReleaseLock(parentFolder, "resourcelock", -1);


            var successfulLock = await remoteLock.AcquireLock(parentFolder, "resourcelock");
            var unsuccessfulLock = await remoteLock.AcquireLock(parentFolder, "resourcelock");
            Assert.IsFalse(unsuccessfulLock.IsSuccess);
            Assert.IsTrue(unsuccessfulLock.TryAgainIn.TotalSeconds >= 1 && unsuccessfulLock.TryAgainIn.TotalSeconds < 3);
            await Task.Delay(unsuccessfulLock.TryAgainIn);
            var secondSuccessfulLock = await remoteLock.AcquireLock(parentFolder, "resourcelock");
            Assert.IsTrue(secondSuccessfulLock.IsSuccess);
            // got new cookie?
            Assert.AreNotEqual(successfulLock.ResultingLockCookie, secondSuccessfulLock.ResultingLockCookie);

            var unlockResult = await remoteLock.ReleaseLock(parentFolder, "resourcelock", secondSuccessfulLock.ResultingLockCookie.Value);
            Assert.IsTrue(unlockResult);
        }

        [TestMethod]
        public async Task TestReleaseWithoutAcquisition()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();

            var imapFac = kernel.Get<ImapConnectionFactory>();
            var remoteLock = kernel.Get<ImapBackedRemoteLock>();
            remoteLock.LockTimeOut = TimeSpan.FromSeconds(5);

            using var connection = await imapFac.GetImapConnectionAsync();
            var parentFolder = await connection.GetFolderAsync(connection.PersonalNamespaces[0].Path);
            await remoteLock.ReleaseLock(parentFolder, "resourcelock", -1);

            var result = await remoteLock.ReleaseLock(parentFolder, "resourcelock", 1234);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestConcurrentLockAcquisition()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();

            var imapFac = kernel.Get<ImapConnectionFactory>();
            var remoteLock = kernel.Get<ImapBackedRemoteLock>();
            remoteLock.LockTimeOut = TimeSpan.FromSeconds(5);

            using var connection = await imapFac.GetImapConnectionAsync();
            var parentFolder = await connection.GetFolderAsync(connection.PersonalNamespaces[0].Path);
            await remoteLock.ReleaseLock(parentFolder, "resourcelock", -1);
            connection.Disconnect(true);

            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var connection = await imapFac.GetImapConnectionAsync();
                    var parentFolder = await connection.GetFolderAsync(connection.PersonalNamespaces[0].Path);
                    var lockResult = await remoteLock.AcquireLock(parentFolder, "resourcelock");
                    connection.Disconnect(true);
                    while (!lockResult.IsSuccess)
                    {
                        await Task.Delay(lockResult.TryAgainIn);
                        using var connection2 = await imapFac.GetImapConnectionAsync();
                        var parentFolder2 = await connection2.GetFolderAsync(connection2.PersonalNamespaces[0].Path);
                        lockResult = await remoteLock.AcquireLock(parentFolder2, "resourcelock");
                        connection2.Disconnect(true);
                    }
                    await Task.Delay(100);
                    using var connection3 = await imapFac.GetImapConnectionAsync();
                    var parentFolder3 = await connection3.GetFolderAsync(connection3.PersonalNamespaces[0].Path);
                    var unlockResult = await remoteLock.ReleaseLock(parentFolder3, "resourcelock", lockResult.ResultingLockCookie.Value);
                    connection3.Disconnect(true);
                    Assert.IsTrue(unlockResult);
                }));
            }
            await Task.WhenAll(tasks);

            using var connection2 = await imapFac.GetImapConnectionAsync();
            var parentFolder2 = await connection2.GetFolderAsync(connection2.PersonalNamespaces[0].Path);
            var hasLock = await remoteLock.HasLockAsync(parentFolder2, "resourcelock");
            connection2.Disconnect(true);
            Assert.IsFalse(hasLock);
            Assert.AreEqual(0, remoteLock.TimeoutCount, "Unexpected number of timeouts");
        }

        [TestMethod]
        public async Task TestImapServerFolderDuplicateBehavior()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var imapFac = kernel.Get<ImapConnectionFactory>();

            var failures = 0;
            var otherFailures = 0;
            var tasks = new List<Task>();
            var taskCount = 9;
            for (var i = 0; i < taskCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var connection = await imapFac.GetImapConnectionAsync();
                    var parentFolder = await connection.GetFolderAsync(connection.PersonalNamespaces[0].Path);
                    try
                    {
                        var mailFolder = parentFolder.Create(testFolderName, true);
                        var id = mailFolder.UidNext;
                    }
                    catch (Exception e) {
                        if (e.Message.Contains("mailbox already exists", StringComparison.OrdinalIgnoreCase))
                        {
                            lock (this)
                            {
                                failures++;
                            }
                        }
                        else
                            otherFailures++;
                    }
                    connection.Disconnect(true);
                }));
            }
            await Task.WhenAll(tasks);
            Assert.AreEqual(0, otherFailures, "Unexpected number of OTHER failures");
            // Greenmail fails for duplicates, Dovecot succeeds
            Assert.IsTrue(taskCount == failures || failures == 0, "Unexpected number of failures");
        }

        private MimeMessage GetMessage(string subject, string textBody)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("Test", "test@localhost.test"));
            msg.To.Add(new MailboxAddress("Test", "test@localhost.test"));
            msg.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = textBody
            };
            msg.Body = bodyBuilder.ToMessageBody();

            return msg;
        }

        delegate Task CreateTask(int index);

        [TestMethod]
        public async Task TestModSeqBasedLockingInGeneral()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var imapFac = kernel.Get<ImapConnectionFactory>();
            var logger = kernel.Get<ILogger>();
            logger.Debug("=====> START");

            using var con1 = await imapFac.GetImapConnectionAsync();
            var parentFolder = await con1.GetFolderAsync(con1.PersonalNamespaces[0].Path);
            var mailFolder = parentFolder.GetSubfolder(testFolderName);
            mailFolder.Open(FolderAccess.ReadWrite);

            var id = mailFolder.Append(GetMessage("Lock", "0"));
            mailFolder.Fetch(new List<UniqueId>() { id.Value }, MessageSummaryItems.ModSeq);
            mailFolder.Append(GetMessage("Counter inside", "0"));
            con1.Disconnect(true);

            var taskCount = 9;
            var loopCount = 20;
            for (var loop = 0; loop < loopCount; loop++)
            {
                var tasks = new List<Task>();
                for (var i = 0; i < taskCount; i++)
                {
                    CreateTask createTask = (int index) => Task.Run(() =>
                    {
                        var taskIndex = index;
                        logger.Debug("{0:00} Starting task", taskIndex);
                        int cookie;
                        lock (rand)
                        {
                            cookie = rand.Next();
                        }

#pragma warning disable CS0618 // Type or member is obsolete
                        var cookiestring = $"{taskIndex}-{AppDomain.GetCurrentThreadId()}-{cookie}";
#pragma warning restore CS0618 // Type or member is obsolete
                        logger.Debug("{0:00} Cookiestring: {1}", taskIndex, cookiestring);
                        using var con2 = imapFac.GetImapConnectionAsync().Result;
                        var parentFolder = con2.GetFolder(con2.PersonalNamespaces[0].Path);
                        var mailFolder = parentFolder.GetSubfolder(testFolderName);
                        mailFolder.UidValidityChanged += (a, b) => { logger.Warning("0:00 UI VALIDITY changed!", taskIndex); };
                        mailFolder.Open(FolderAccess.ReadWrite);
                        var idOfLockEmail = mailFolder.Search(SearchQuery.SubjectContains("Lock")).Single();
                        logger.Debug("{0:00} Lock email ID: {1}", taskIndex, idOfLockEmail);

                        try
                        {
                            IMessageSummary summary;
                            IList<UniqueId> failList;
                            var keywords = new HashSet<string>() { cookiestring, DateTime.UtcNow.ToFileTimeUtc().ToString() };
                            ulong modSeqMax;
                            do
                            {
                            // might return multiple unrelated summaries...
                            var summaries = mailFolder.Fetch(new List<UniqueId>() { idOfLockEmail }, MessageSummaryItems.Flags | MessageSummaryItems.UniqueId | MessageSummaryItems.ModSeq);

                                if (summaries.Where(s => s.UniqueId == idOfLockEmail && s.Keywords?.Count > 0).Any())
                                {
                                    logger.Debug("{0:00} Got summary with keywords, need to wait", taskIndex);
                                    Task.Delay(rand.Next(100, 2000)).Wait();
                                    continue;
                                }
                                logger.Debug("{0:00} Got summary without keywords - trying to set them", taskIndex);

                                summary = summaries.Where(s => s.UniqueId == idOfLockEmail).Single();
                                modSeqMax = summary.ModSeq.Value;
                            //summary = mailFolder.Fetch(indizes, MessageSummaryItems.ModSeq | MessageSummaryItems.UniqueId);
                            failList = mailFolder.SetFlags(new List<UniqueId>() { idOfLockEmail }, modSeqMax, MessageFlags.UserDefined, keywords, false);
                                if (failList.Count > 0)
                                {
                                    logger.Debug("{0:00} Could not set keywords with modSeq {1}, starting over", taskIndex, modSeqMax);
                                    Task.Delay(rand.Next(100, 2000)).Wait();
                                    continue;
                                }
                                else
                                {
                                    logger.Debug("{0:00} Successfully set keywords, modSeq was {1}", taskIndex, modSeqMax);
                                // really?? check this...

                                var lockMessageSummary = mailFolder
                                        .Fetch(new List<UniqueId>() { idOfLockEmail }, MessageSummaryItems.Flags | MessageSummaryItems.UniqueId | MessageSummaryItems.ModSeq)
                                        .Where(s => s.UniqueId == idOfLockEmail)
                                        .SingleOrDefault();
                                    if (lockMessageSummary == null)
                                    {
                                        logger.Error("{0:00} Could not fetch lock message summary!", taskIndex);
                                    }
                                    logger.Debug("{0:00} Got lock messgage keywords: {1}", taskIndex, lockMessageSummary.Keywords);
                                    if (lockMessageSummary.Keywords.Count > 2)
                                    {
                                        cookie = rand.Next();
#pragma warning disable CS0618 // Type or member is obsolete
                                        cookiestring = $"{taskIndex}-{AppDomain.GetCurrentThreadId()}-{cookie}";
#pragma warning restore CS0618 // Type or member is obsolete
                                        keywords = new HashSet<string>() { cookiestring, DateTime.UtcNow.ToFileTimeUtc().ToString() };
                                    // with Dovecot it sometimes happened that two threads succeed in setting their keywords although only one should succeed (as both use the same modSeq number)
                                    logger.Warning("{0:00} Got wrong number of keywords, starting over with new keywords for thread: {1}", taskIndex, keywords);
                                        mailFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.UserDefined, lockMessageSummary.Keywords, true);
                                        continue;
                                    }
                                    if (!lockMessageSummary.Keywords.Contains(cookiestring))
                                    {
                                    // with Dovecot it sometimes happened that sometimes the keywords cannot be set but failList nevertheless is empty
                                    logger.Warning("{0:00} Control keyword f-up: {1} (expected cookiestring {2}); starting over", lockMessageSummary.Keywords, cookiestring);
                                        continue;
                                    }
                                    Assert.AreEqual(2, lockMessageSummary.Keywords.Count);

                                    break;
                                }
                            } while (true);
                            logger.Debug("{0:00} Increasing count section", taskIndex);

                            summary = mailFolder
                                .Fetch(new List<UniqueId>() { idOfLockEmail }, MessageSummaryItems.ModSeq)
                                .Where(s => s.UniqueId == idOfLockEmail)
                                .Single();

                            var idOfCounterEmail = mailFolder.Search(SearchQuery.SubjectContains("Counter inside")).Single();
                        // increase count
                        var loadedMessage = mailFolder.GetMessage(idOfCounterEmail);
                            var count = int.Parse(loadedMessage.TextBody);
                            logger.Debug("{0:00} Got old count: {1}", taskIndex, count);
                            var newBody = $"{count + 1}";
                        // note: the UniqueId does change
                        logger.Debug("{0:00} Updating count mail", taskIndex);
                            idOfCounterEmail = mailFolder.Replace(idOfCounterEmail, GetMessage("Counter inside", newBody), MessageFlags.None).Value;
                            Assert.IsNotNull(idOfCounterEmail);
                            Assert.IsTrue(idOfCounterEmail.IsValid);

                        //Assert.AreEqual(modSeqMax + 4, mailFolder.HighestModSeq);
                        logger.Debug("{0:00} Removing keywords", taskIndex);
                            mailFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.UserDefined, keywords, true);
                        //Assert.AreEqual(0, failList.Count, "Somebody removed keywords in parallel which must not happen");
                        logger.Debug("{0:00} DONE", taskIndex);
                        }
                        catch
                        {
                            Debugger.Break();
                        }
                        finally
                        {
                            con2.Disconnect(true);
                        }
                        logger.Debug("Exiting task {0}", taskIndex);

                    });
                    tasks.Add(createTask(i));
                }
                await Task.WhenAll(tasks);
            }

            using var con3 = await imapFac.GetImapConnectionAsync();
            var parentFolder2 = await con3.GetFolderAsync(con3.PersonalNamespaces[0].Path);
            var mailFolder2 = parentFolder2.GetSubfolder(testFolderName);
            mailFolder2.Open(FolderAccess.ReadWrite);
            var idOfCounterEmail = mailFolder2.Search(SearchQuery.SubjectContains("Counter inside")).Single();
            var msg = mailFolder2.GetMessage(idOfCounterEmail);
            con3.Disconnect(true);
            Assert.AreEqual(taskCount * loopCount, int.Parse(msg.TextBody));
        }

        [TestMethod]
        public async Task TestLockingWithEmailBackedRemoteLockImplementation()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var remoteLock = kernel.Get<ImapBackedRemoteLock>();
            var imapFac = kernel.Get<ImapConnectionFactory>();
            var logger = kernel.Get<ILogger>();
            logger.Debug("=====> START");

            using var con1 = await imapFac.GetImapConnectionAsync();
            var parentFolder = await con1.GetFolderAsync(con1.PersonalNamespaces[0].Path);
            var lockFolder = parentFolder.GetSubfolder(testFolderName);
            lockFolder.Open(FolderAccess.ReadWrite);
            lockFolder.Append(GetMessage("Counter inside", "0"));

            var lockFolderName = "locktest";
            // release any old lock
            await remoteLock.ReleaseLock(lockFolder, lockFolderName, -1);
            con1.Disconnect(true);
            var taskCount = 2;
            var loopCount = 20;
            for (var loop = 0; loop < loopCount; loop++)
            {
                var tasks = new List<Task>();
                for (var i = 0; i < taskCount; i++)
                {
                    CreateTask createTask = (int index) => Task.Run(async () =>
                    {
                        var taskIndex = index;
                        logger.Debug("{0:00} Starting task", taskIndex);

                        using var con2 = imapFac.GetImapConnectionAsync().Result;
                        var parentFolder = con2.GetFolder(con2.PersonalNamespaces[0].Path);
                        var lockFolder = parentFolder.GetSubfolder(testFolderName);
                        var lockResult = await remoteLock.AcquireLockRetry(lockFolder, lockFolderName, agressiveMode: true);
                        Assert.IsTrue(lockResult.IsSuccess);
                        Assert.IsNotNull(lockResult.ResultingLockCookie);

                        
                        logger.Debug("{0:00} Increasing count section", taskIndex);
                        lockFolder.Open(FolderAccess.ReadWrite);
                        var idOfCounterEmail = lockFolder.Search(SearchQuery.SubjectContains("Counter inside")).Single();
                        // increase count
                        var loadedMessage = lockFolder.GetMessage(idOfCounterEmail);
                        var count = int.Parse(loadedMessage.TextBody);
                        logger.Debug("{0:00} Got old count: {1}", taskIndex, count);
                        var newBody = $"{count + 1}";
                        // note: the UniqueId does change
                        logger.Debug("{0:00} Updating count mail", taskIndex);
                        idOfCounterEmail = lockFolder.Replace(idOfCounterEmail, GetMessage("Counter inside", newBody), MessageFlags.None).Value;
                        Assert.IsNotNull(idOfCounterEmail);
                        Assert.IsTrue(idOfCounterEmail.IsValid);

                        await remoteLock.ReleaseLock(lockFolder, lockFolderName, lockResult.ResultingLockCookie.Value);

                        con2.Disconnect(true);
                    });
                    tasks.Add(createTask(i));
                }
                await Task.WhenAll(tasks);
            }

            using var con3 = await imapFac.GetImapConnectionAsync();
            var parentFolder2 = await con3.GetFolderAsync(con3.PersonalNamespaces[0].Path);
            var mailFolder2 = parentFolder2.GetSubfolder(testFolderName);
            mailFolder2.Open(FolderAccess.ReadWrite);
            var idOfCounterEmail = mailFolder2.Search(SearchQuery.SubjectContains("Counter inside")).Single();
            var msg = mailFolder2.GetMessage(idOfCounterEmail);
            con3.Disconnect(true);
            Assert.AreEqual(taskCount * loopCount, int.Parse(msg.TextBody));
        }
    }
}
