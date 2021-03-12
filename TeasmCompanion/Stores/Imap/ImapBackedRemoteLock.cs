using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using MailKit;
using System.Threading.Tasks;
using System.Threading;
using Serilog;
using MimeKit;
using MailKit.Search;
using System.Diagnostics;
using TeasmCompanion.Misc;
using TeasmCompanion.Interfaces;

namespace TeasmCompanion.Stores.Imap
{
    /// <summary>
    /// Implements a locking mechanism based on IMAP messages. Needs the CONDSTORE IMAP capability.
    /// 
    /// The CONDSTORE IMAP capability allows to modify the user defined tags of a message in a transactional way. You try to modify tags and 
    /// the modification fails if somebody else already modified them. For us this means that if one client succeeds to set the tags he holds the lock. All
    /// other clients fail to set the tags and then wait for the lock to be released.
    /// 
    /// Each lock comes with a lifetime. If a client fails to release or renew its lock another client will be able to acquire the lock after this time.
    /// In theory it is possible that a lock-holding client that was put to sleep resumes working on a shared source that he alledgedly still holds the lock
    /// on while another client works on the resource as well. But we won't be able to solve this without controlling the server implementation - and in case 
    /// of existing IMAP servers we don't have this control.
    /// </summary>
    public class ImapBackedRemoteLock
    {
        private readonly string LockMessageSubject = "[LOCK] Don't modify me!";

        public class LockResult
        {
            public bool IsSuccess { get; set; }
            public int? ResultingLockCookie { get; set; }
            public TimeSpan TryAgainIn { get; set; } = TimeSpan.FromSeconds(0);
            public string LockResourceName { get; }

            public LockResult(string lockResourceName)
            {
                LockResourceName = lockResourceName;
            }
        }

        public static Random rand = new Random();
        private readonly ILogger logger;

        public TimeSpan LockTimeOut { get; set; }
        public long TimeoutCount { get; private set; }

        public ImapBackedRemoteLock(ILogger logger)
        {
            LockTimeOut = TimeSpan.FromSeconds(30);
            this.logger = logger.ForContext<ImapBackedRemoteLock>();
        }

        private MimeMessage CreateLockMimeMessage()
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            msg.To.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            msg.Subject = LockMessageSubject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = "Message used as resource lock; Don't modify me"
            };
            msg.Body = bodyBuilder.ToMessageBody();

            return msg;
        }

        private UniqueId GetFirstExistingLockMessage(IMailFolder folder, CancellationToken cancellationToken = default)
        {
            folder.Open(FolderAccess.ReadOnly);
            var lockMessages = folder.Search(SearchQuery.SubjectContains(LockMessageSubject), cancellationToken);
            var orderedLockMessages = lockMessages?.OrderBy(m => m.Id);
            if (orderedLockMessages.Count() > 1)
            {
                var idToRemove = orderedLockMessages.Last();
                try
                {
                    folder.Open(FolderAccess.ReadWrite);
                    folder.SetFlags(idToRemove, MessageFlags.Deleted, true, cancellationToken);
                    folder.Expunge();
                }
                catch
                {
                    logger.Debug("Failed to remove duplicate lock message with ID {0}; note that this might be ok if another client already removed the duplicate", idToRemove);
                }
            }

            return orderedLockMessages.First();
        }

        private IMailFolder GetOrCreateLockFolder(IMailFolder parentFolderOfLockFolder, string resourceName, CancellationToken cancellationToken = default)
        {
            var lockFolderName = parentFolderOfLockFolder.MakeSafeFolderName(resourceName);
            IMailFolder lockFolder = null;
            try
            {
                lockFolder = parentFolderOfLockFolder.GetSubfolder(lockFolderName, cancellationToken);
            }
            catch
            {
                logger.Information("Could not get lock folder '{LockFolderName}' - maybe it does not yet exist or there was an error", lockFolderName);
            }
            if (lockFolder == null)
            {
                try
                {
                    logger.Information("Creating new lock folder with name '{LockFolderName}'", lockFolderName);
                    // note: on some mail servers (Greenmail) this will throw if the folder already exists, other servers don't throw and return any existing foldre with this name (Dovecot)
                    lockFolder = parentFolderOfLockFolder.Create(lockFolderName, true);
                }
                catch
                {
                    logger.Information("Error while creating lock folder '{LockFolderName}' - maybe it already exists or ther was an error", lockFolderName);
                }
            }

            if (lockFolder == null)
            {
                throw new TeasmCompanionException($"Could not get or create lock folder '{lockFolderName}'");
            }

            lockFolder.Open(FolderAccess.ReadOnly);
            var existingLockMessages = lockFolder.Search(SearchQuery.SubjectContains(LockMessageSubject), cancellationToken);
            if (existingLockMessages.Count == 0)
            {
                // note: this might create duplicates if run by multiple clients; we'll remove the duplicates later
                lockFolder.Append(CreateLockMimeMessage());
                Thread.Sleep(1000); // give other parallel clients the chance to produce their duplicates if any; we then always choose the first one (selected by UniqueId)
            }

            return lockFolder;
        }

        private string GetCookieStringForCookie(int cookie)
        {
            return $"cookie-{cookie}";
        }

        private string GetDateTimeString()
        {
            return $"filetimeutc-{DateTime.UtcNow.ToFileTimeUtc()}";
        }

        private LockResult AcquireLockInternal(IMailFolder parentFolder, string resourceName, int? previousCookie, CancellationToken cancellationToken)
        {
            var resourceNameForLogging = $"{parentFolder.FullName}::{resourceName}";
            logger.Verbose("{0:0000000000} [{ResourceName}] Starting AcquireLockInternal", previousCookie, resourceNameForLogging);
            int cookie;
            if (previousCookie.HasValue)
            {
                cookie = previousCookie.Value;
            }
            else
            {
                lock (rand)
                {
                    cookie = rand.Next();
                }
            }
            var cookiestring = GetCookieStringForCookie(cookie);
            logger.Verbose("{0:0000000000} [{ResourceName}] Previous cookie: {PrevCookie}, cookiestring: {CookieString}", cookie, resourceNameForLogging, previousCookie, cookiestring);

            var lockFolder = GetOrCreateLockFolder(parentFolder, resourceName, cancellationToken);
            lockFolder.UidValidityChanged += (a, b) => { logger.Warning("0:00 UI VALIDITY changed!", cookie); };
            var idOfLockEmail = GetFirstExistingLockMessage(lockFolder, cancellationToken);
            logger.Verbose("{0:0000000000} [{ResourceName}] Lock email ID: {ID}", cookie, resourceNameForLogging, idOfLockEmail);

            try
            {
                IMessageSummary summary;
                IList<UniqueId> failList;
                var newKeywords = new HashSet<string>() { cookiestring, GetDateTimeString() };
                ulong modSeqMax;

                // might return multiple unrelated summaries...
                var summaries = lockFolder.Fetch(new List<UniqueId>() { idOfLockEmail }, MessageSummaryItems.Flags | MessageSummaryItems.UniqueId | MessageSummaryItems.ModSeq, cancellationToken);
                summary = summaries.Where(s => s.UniqueId == idOfLockEmail).Single();
                modSeqMax = summary.ModSeq.Value;

                var summariesWithKeywords = summaries.Where(s => s.UniqueId == idOfLockEmail && s.Keywords?.Count > 0);
                var existingKeywords = summariesWithKeywords.FirstOrDefault()?.Keywords;
                var lockState = GetLockState(existingKeywords, cookie);
                logger.Verbose("{0:0000000000} [{ResourceName}] Current lock state: {@LockState}", cookie, resourceNameForLogging, lockState);
                lockFolder.Open(FolderAccess.ReadWrite, cancellationToken);
                if (lockState.LockType == LockType.ActiveLockByMe)
                {
                    // add new time before removing the old one; note that this for a short period of time adds a third flag
                    failList = lockFolder.AddFlags(new List<UniqueId>() { idOfLockEmail }, modSeqMax, MessageFlags.UserDefined, new HashSet<string>() { GetDateTimeString() }, true, cancellationToken);
                    if (failList.Count != 0)
                    {
                        logger.Warning("{0:0000000000} [{ResourceName}] Failed to update our own lock (old keywords: {OldKeywords}). This is not right.", cookie, resourceNameForLogging, existingKeywords);
                        return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = previousCookie, TryAgainIn = TimeSpan.FromMilliseconds(rand.Next(50, 2000)) };
                    }
                    if (lockState.FileTimeUtcKeywords.Any())
                    {
                        // this is our lock! update it (note that this should always succeed as we are holding the lock
                        lockFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.UserDefined, lockState.FileTimeUtcKeywords, true, cancellationToken);
                    }
                    return new LockResult(resourceName) { IsSuccess = true, ResultingLockCookie = previousCookie };
                }

                if (lockState.LockType == LockType.MakesNoSense)
                {
                    logger.Warning("{0:0000000000} [{ResourceName}] Apparently the lock keywords make no sense. Removing those that make no sense ({KeywordsNoSense}) and signaling to start over.", cookie, resourceNameForLogging, lockState.RemoveThoseThatMakeNoSense);

                    if (lockState.RemoveThoseThatMakeNoSense.Any())
                    {
                        lockFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.UserDefined, lockState.RemoveThoseThatMakeNoSense, true, cancellationToken);
                    }
                    return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = previousCookie, TryAgainIn = TimeSpan.FromMilliseconds(rand.Next(200, 2000)) };
                }

                if (lockState.LockType == LockType.ActiveLockByOther)
                {
                    logger.Debug("{0:0000000000} [{ResourceName}] Got {SummaryCount} summary with keywords {Keywords}, need to wait", cookie, resourceNameForLogging, summariesWithKeywords.Count(), summariesWithKeywords.First().Keywords);
                    return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = previousCookie, TryAgainIn = lockState.LockCookieTimeLeft };
                }

                // got an expired lock?
                if (lockState.LockType == LockType.ExpiredLockByMe || lockState.LockType == LockType.ExpiredLockByOther)
                {
                    try
                    {
                        // try to remove the existing keywords; note that other clients might do the same at the same time
                        lockFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, modSeqMax, MessageFlags.UserDefined, existingKeywords, false, cancellationToken);
                    } catch (Exception e)
                    {
                        logger.Debug(e, "{0:0000000000} [{ResourceName}] Could not remove keywords with modseq {ModSeq}, there was an exception; ignoring and trying to acquire", cookie, resourceNameForLogging, modSeqMax);
                    }
                    return AcquireLockInternal(parentFolder, resourceName, previousCookie, cancellationToken);
                }

                logger.Verbose("{0:0000000000} [{ResourceName}] Trying to acquire lock", cookie, resourceNameForLogging);

                //summary = mailFolder.Fetch(indizes, MessageSummaryItems.ModSeq | MessageSummaryItems.UniqueId);
                lockFolder.Open(FolderAccess.ReadWrite, cancellationToken);
                try
                {
                    failList = lockFolder.SetFlags(new List<UniqueId>() { idOfLockEmail }, modSeqMax, MessageFlags.UserDefined, newKeywords, false);
                } catch (Exception e)
                {
                    logger.Debug(e, "{0:0000000000} [{ResourceName}] Could not set keywords with modSeq {ModSeq}, there was an exception; starting over", cookie, resourceNameForLogging, modSeqMax);
                    return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = previousCookie, TryAgainIn = TimeSpan.FromMilliseconds(rand.Next(50, 2000)) };
                }
                if (failList.Count > 0)
                {
                    logger.Debug("{0:0000000000} [{ResourceName}] Could not set keywords with modSeq {ModSeq} (another client might have been first), starting over", cookie, resourceNameForLogging, modSeqMax);
                    return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = previousCookie, TryAgainIn = TimeSpan.FromMilliseconds(rand.Next(50, 2000)) };
                }
                else
                {
                    logger.Verbose("{0:0000000000} [{ResourceName}] Successfully set keywords, modSeq was {ModSeq}", cookie, resourceNameForLogging, modSeqMax);

                    // really?? check this...
                    var lockMessageSummary = lockFolder
                            .Fetch(new List<UniqueId>() { idOfLockEmail }, MessageSummaryItems.Flags | MessageSummaryItems.UniqueId | MessageSummaryItems.ModSeq, cancellationToken)
                            .Where(s => s.UniqueId == idOfLockEmail)
                            .SingleOrDefault();
                    if (lockMessageSummary == null)
                    {
                        logger.Error("{0:0000000000} [{ResourceName}] Could not fetch lock email summary!", cookie, resourceNameForLogging);
                        return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = previousCookie, TryAgainIn = TimeSpan.FromMilliseconds(rand.Next(0, 500)) };
                    }
                    logger.Verbose("{0:0000000000} [{ResourceName}] Got lock message keywords to check successful set: {Keywords}", cookie, resourceNameForLogging, lockMessageSummary.Keywords);
                    if (lockMessageSummary.Keywords.Count > 2)
                    {
                        // with Dovecot it sometimes happened that multiple threads succeed in setting their keywords although only one should succeed (as both use the same modSeq number)
                        var newCookie = rand.Next();
                        logger.Warning("{0:0000000000} [{ResourceName}] Got wrong number of keywords, starting over with new cookie {NewCookie}", cookie, resourceNameForLogging, newCookie);
                        lockFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.UserDefined, lockMessageSummary.Keywords, true, cancellationToken);
                        return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = newCookie, TryAgainIn = TimeSpan.FromMilliseconds(rand.Next(0, 500)) };
                    }
                    if (!lockMessageSummary.Keywords.Contains(cookiestring))
                    {
                        // with Dovecot it sometimes happened that the keywords cannot be set but failList nevertheless is empty (which it should not be)
                        logger.Warning("{0:0000000000} [{ResourceName}] Control keyword f-up: {Keywords} (expected cookiestring {CookieString}); starting over", cookie, resourceNameForLogging, lockMessageSummary.Keywords, cookiestring);
                        return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = previousCookie, TryAgainIn = TimeSpan.FromMilliseconds(rand.Next(0, 500)) };
                    }

                    if (lockState.RemoveThoseThatMakeNoSense.Count > 0)
                    {
                        logger.Warning("{0:0000000000} [{ResourceName}] Removing some keywords that make no sense: {KeywordsNoSense}", cookie, resourceNameForLogging, lockState.RemoveThoseThatMakeNoSense);
                        lockFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.UserDefined, lockState.RemoveThoseThatMakeNoSense, true, cancellationToken);
                    }

                    lockFolder.AddFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.Seen, true, cancellationToken);
                    logger.Debug("{0:0000000000} [{ResourceName}] Successfully acquired lock", cookie, resourceNameForLogging);
                    return new LockResult(resourceName) { IsSuccess = true, ResultingLockCookie = cookie };
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Unknown exception while trying to get lock for resource '{ResourceName}'", resourceNameForLogging);
                return new LockResult(resourceName) { IsSuccess = false, ResultingLockCookie = previousCookie, TryAgainIn = TimeSpan.FromMilliseconds(rand.Next(500, 3000)) };
            }
        }

        private enum LockType
        {
            NotLocked,
            ActiveLockByOther,
            ActiveLockByMe,
            ExpiredLockByOther,
            ExpiredLockByMe,
            MakesNoSense
        }

        private class LockState
        {
            public LockType LockType { get; set; }
            public int LockCookie { get; set; }
            public TimeSpan LockCookieTimeLeft { get; set; }
            public HashSet<string> RemoveThoseThatMakeNoSense { get; private set; } = new HashSet<string>();
            public HashSet<string> FileTimeUtcKeywords { get; private set; } = new HashSet<string>();
        }

        private LockState GetLockState(HashSet<string> keywords, int cookie)
        {
            logger.Verbose("{0:0000000000} Calculating lock state from keywords {1}", cookie, keywords);
            var result = new LockState();
            if (keywords == null || keywords.Count == 0)
            {
                result.LockType = LockType.NotLocked;
                return result;
            }

            var cookieStringFromLockmessage = keywords.Where(k => k.StartsWith("cookie-")).Select(k => k.Split('-')[1]);
            // multiple file times are supported, although this is a half-errorneous state where a previous lock extension somehow failed to remove the old time value
            var filetimeStringFromLockMessage = keywords.Where(k => k.StartsWith("filetimeutc-")).Select(k => k.Split('-')[1]);
            var otherKeywords = keywords.Where(k => !k.StartsWith("filetimeutc-") && !k.StartsWith("cookie-"));

            if (otherKeywords.Any())
            {
                logger.Warning("{0:0000000000} Detected unexpected keywords for lock message; all keywords: {0}, unexpected: {1}; marking for removal to resolve this inconsistency", cookie, keywords, otherKeywords);
                result.RemoveThoseThatMakeNoSense.AddRange(otherKeywords);
            }

            if (cookieStringFromLockmessage.Count() == 1)
            {
                if (int.TryParse(cookieStringFromLockmessage.First(), out var lockCookie))
                {
                    // cool, one parsable cookie value
                    result.LockCookie = lockCookie;
                }
                else
                {
                    // not parsable? then there is no cookie value left...
                    // no cookie value at all? this cannot work - remove all
                    result.RemoveThoseThatMakeNoSense.AddRange(keywords);
                    result.LockType = LockType.NotLocked;
                    return result;
                }
            }
            else if (!cookieStringFromLockmessage.Any())
            {
                // no cookie value at all? this cannot work - remove all
                result.RemoveThoseThatMakeNoSense.AddRange(keywords);
                result.LockType = LockType.NotLocked;
                return result;
            }
            else
            {
                // more than one cookie and timeout value(s)? this might be an intermediary state; do NOT remove them as this is expected to resolve itself
                if (filetimeStringFromLockMessage.Any())
                {
                    result.LockType = LockType.MakesNoSense;
                    return result;
                }
                else
                {
                    // more than one cookie and NOT timeout value? this is broken, remove all
                    result.RemoveThoseThatMakeNoSense.AddRange(keywords);
                    result.LockType = LockType.MakesNoSense;
                    return result;
                }
            }

            var orderedCookieTimesDesc = filetimeStringFromLockMessage.Select(k =>
            {
                try
                {
                    if (long.TryParse(k, out var parsedFileTimeUtc))
                    {
                        var dateTimeUtc = DateTime.FromFileTimeUtc(parsedFileTimeUtc);
                        result.FileTimeUtcKeywords.Add(k);
                        return dateTimeUtc;
                    }
                    else
                    {
                        // parse error? remove this errorneous value
                        result.RemoveThoseThatMakeNoSense.Add(k);
                    }
                }
                catch
                {
                    // other error? remove this errorneous value
                    result.RemoveThoseThatMakeNoSense.Add(k);
                }
                return DateTime.MinValue;
            }).OrderByDescending(o => o);

            // no valid time found? this makes no sense as every cookie should be accompanied with a timeout value
            if (orderedCookieTimesDesc.Count() == 0)
            {
                result.RemoveThoseThatMakeNoSense.AddRange(keywords);
                result.LockType = LockType.MakesNoSense;
                return result;
            }

            // take newest time; note: multiple times can be present if the lock holder renews his lock - the new time is added first making 2 times present for a short period of time
            var cookieTimeUtcFromLogMessage = orderedCookieTimesDesc.First();

            // not our log; check expiration
            if (DateTime.UtcNow - cookieTimeUtcFromLogMessage > LockTimeOut)
            {
                if (result.LockCookie == cookie)
                {
                    result.LockType = LockType.ExpiredLockByMe;
                }
                else
                {
                    result.LockType = LockType.ExpiredLockByOther;
                }

                logger.Information("{0:0000000000} Existing lock of cookie {1} expired on {2} local time", cookie, result.LockCookie, cookieTimeUtcFromLogMessage.ToLocalTime() + LockTimeOut);
                TimeoutCount++;
            }
            else
            {
                var timeLeft = cookieTimeUtcFromLogMessage + LockTimeOut - DateTime.UtcNow;
                if (result.LockCookie == cookie)
                {
                    logger.Verbose("{0:0000000000} I still hold the lock for this long: {1}", cookie, timeLeft);
                    result.LockType = LockType.ActiveLockByMe;
                    result.LockCookieTimeLeft = timeLeft;
                }
                else
                {
                    logger.Debug("{0:0000000000} Other Cookie {1} still holds the lock for this long: {2}", cookie, result.LockCookie, timeLeft);
                    result.LockType = LockType.ActiveLockByOther;
                    // don't wait for whole other lock time as the lock most likely is going to be released soon since locks should be only hold for a short period of time; but also don't hammer the server
                    result.LockCookieTimeLeft = TimeSpan.FromMilliseconds(Math.Min(timeLeft.TotalMilliseconds, rand.Next(1000, 5000)));
                }
            }

            return result;
        }

        private bool ReleaseLockInternal(IMailFolder parentFolder, string resourceName, int? cookie, CancellationToken cancellationToken)
        {
            if (!cookie.HasValue)
            {
                return true;
            }

            var resourceNameForLogging = $"{parentFolder.FullName}::{resourceName}";
            logger.Verbose("{0:0000000000} Processing request to release resource '{1}'", cookie, resourceNameForLogging);
            var lockFolder = GetOrCreateLockFolder(parentFolder, resourceName, cancellationToken);
            lockFolder.UidValidityChanged += (a, b) => { logger.Warning("{0:0000000000} UI VALIDITY changed!", cookie); };
            lockFolder.Open(FolderAccess.ReadWrite);
            var idOfLockEmail = GetFirstExistingLockMessage(lockFolder, cancellationToken);
            var summary = lockFolder
                .Fetch(new List<UniqueId>() { idOfLockEmail }, MessageSummaryItems.ModSeq | MessageSummaryItems.Flags)
                .Where(s => s.UniqueId == idOfLockEmail)
                .Single();
            var keywords = summary.Keywords;

            var lockState = GetLockState(keywords, cookie.Value);
            logger.Verbose("{0:0000000000} Current lock state for resource '{1}': {@LockState}", cookie, resourceNameForLogging, lockState);
            try
            {
                if (lockState.RemoveThoseThatMakeNoSense.Count > 0)
                {
                    lockFolder.Open(FolderAccess.ReadWrite);
                    lockFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.UserDefined, lockState.RemoveThoseThatMakeNoSense, true);
                }
            }
            catch (Exception e)
            {
                logger.Warning(e, "{0:0000000000} [{ResourceName}] Error while removing keywords {KeywordsNoSense}; not critical but shouldn't happen", cookie, resourceNameForLogging, lockState.RemoveThoseThatMakeNoSense);
            }

            if (lockState.LockType == LockType.NotLocked)
            {
                logger.Debug("{0:0000000000} [{ResourceName}] No lock present for resource, guess that's a successful unlock", cookie, resourceNameForLogging);
                return true;
            }

            if (lockState.LockType == LockType.ActiveLockByOther)
            {
                logger.Debug("{0:0000000000} [{ResourceName}] Active lock present for resource, expires in {TimeLeft}", cookie, resourceNameForLogging, lockState.LockCookieTimeLeft);
                return false;
            }

            // covered all special cases above - if we are here we are free to remove the existing lock (expired lock or lock by us)
            logger.Verbose("{0:0000000000} Releasing lock by removing all keywords", cookie);
            lockFolder.Open(FolderAccess.ReadWrite);
            lockFolder.RemoveFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.UserDefined, keywords, true);
            lockFolder.AddFlags(new List<UniqueId>() { idOfLockEmail }, MessageFlags.Seen, true);
            logger.Debug("{0:0000000000} [{ResourceName}] DONE releasing lock", cookie, resourceNameForLogging);
            return true;
        }

        public async Task<bool> HasLockAsync(IMailFolder parentFolder, string resourceName)
        {
            return await Task.Run(() =>
            {
                var lockFolder = GetOrCreateLockFolder(parentFolder, resourceName);
                var idOfLockEmail = GetFirstExistingLockMessage(lockFolder);
                var summary = lockFolder
                    .Fetch(new List<UniqueId>() { idOfLockEmail }, MessageSummaryItems.ModSeq)
                    .Where(s => s.UniqueId == idOfLockEmail)
                    .Single();
                var keywords = summary.Keywords;

                return keywords.Count > 0;
            });
        }

        public IObservable<LockResult> AcquireLock(IMailFolder folder, string resourceName, int? previousCookie = null)
        {
            return Observable.FromAsync((cancellationToken) => Task.Run(() => AcquireLockInternal(folder, resourceName, previousCookie, cancellationToken)));
        }

        public IObservable<LockResult> AcquireLockRetry(IMailFolder folder, string resourceName, int? previousCookie = null, int overallTimeoutSecs = 60, bool agressiveMode = false)
        {
            return Observable.FromAsync((cancellationToken) => Task.Run(() =>
            {
                var watch = Stopwatch.StartNew();
                while (true)
                {
                    LockResult lockResult;
                    lock (folder.SyncRoot)
                    {
                        lockResult = AcquireLockInternal(folder, resourceName, previousCookie, cancellationToken);
                    }
                    if (lockResult.IsSuccess)
                        return lockResult;

                    if (watch.Elapsed.TotalSeconds > overallTimeoutSecs)
                    {
                        return lockResult;
                    }

                    if (!agressiveMode)
                    {
                        Thread.Sleep(lockResult.TryAgainIn);
                    }
                }
            }));
        }


        public IObservable<bool> ReleaseLock(IMailFolder folder, string resourceName, int? cookie)
        {
            return Observable.FromAsync((cancellationToken) => Task.Run(() => ReleaseLockInternal(folder, resourceName, cookie, cancellationToken)));
        }

    }
}
