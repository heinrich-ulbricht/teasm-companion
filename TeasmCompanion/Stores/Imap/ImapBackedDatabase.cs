using MailKit;
using MimeKit;
using Serilog;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Misc;
using static TeasmCompanion.Stores.Imap.ImapBackedRemoteLock;

#nullable enable

namespace TeasmCompanion.Stores.Imap
{
    public class ImapBackedDatabase
    {
        private readonly ILogger logger;
        private readonly ImapConnectionFactory imapConnectionFactory;
        private readonly ImapBackedRemoteLock remoteLock;

        public ImapBackedDatabase(ILogger logger, ImapConnectionFactory imapConnectionFactory, ImapBackedRemoteLock remoteLock)
        {
            this.logger = logger.ForContext<ImapBackedDatabase>();
            this.imapConnectionFactory = imapConnectionFactory;
            this.remoteLock = remoteLock;
        }

        private async Task<(MimeMessage, UniqueId)> CreateEmptyStorageMessageAsync(IMailFolder folder, string messageId, Action<MimeMessage>? initializationCallback)
        {
            var resultMessage = new MimeMessage(new Header(HeaderId.MessageId, messageId));
            resultMessage.From.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            resultMessage.To.Add(new MailboxAddress(Constants.AppName, Constants.AppFakeEmail));
            resultMessage.Date = new DateTime(2000, 01, 01);
            resultMessage.Subject = Constants.MetadataMessageSubjectPrefix;
            resultMessage.Body = new Multipart();

            // gives caller the chance to further initialize the message
            if (initializationCallback != null)
            {
                initializationCallback(resultMessage);
            }

            // note: this creates the message on the server AND receives it again - this could be optimized if made more context aware
            await folder.OpenAsync(FolderAccess.ReadWrite);
            logger.Verbose("Creating storage message in folder {FolderPath}", folder.FullName);
            var newId = await folder.AppendAsync(resultMessage, MessageFlags.Seen);
            if (!newId.HasValue)
            {
                throw new TeasmCompanionException("Cannot create storage message");
            }
            resultMessage = await folder.GetMessageAsync(newId.Value);
            logger.Debug("Created storage message in folder {FolderPath} with {@ID}", folder.FullName, newId);
            return (resultMessage, newId.Value);
        }

        // note: the acquired lock MUST be released!
        private async Task<(MimeMessageWithUniqueId, LockResult)> GetOrCreateStorageMessageInFolderAsync(IMailFolder folder, string uniqueStoreName, string lockFolderName, bool unlockResource, Action<MimeMessage>? initializationCallback = null)
        {
            var messageIdHeaderValue = uniqueStoreName.EnsureContentIdFormat();
            logger.Debug("Searching storage message in folder {FolderPath} with Message-Id header value {MessageIdHeaderValue}", folder.FullName, messageIdHeaderValue);
            var lockResult = await remoteLock.AcquireLockRetry(folder, lockFolderName);
            if (!lockResult.IsSuccess)
            {
                throw new CannotLockException($"Cannot get lock for resource '{lockFolderName}'");
            }

            try
            {
                var searchResult = await folder.FindIdByMessageIdHeader(messageIdHeaderValue, false, false);
                var uniqueId = searchResult.Item1;
                MimeMessage? resultMessage = null;
                if (uniqueId.IsValid)
                {
                    var message = await folder.GetMessageOrDefaultAsync(uniqueId);
                    logger.Debug("Found storage message in folder {FolderPath} for Message-Id header value {MessageIdHeaderValue} with ID {ID}", folder.FullName, messageIdHeaderValue, message.Item2);
                    resultMessage = message.Item1;
                }

                if (resultMessage == null)
                {
                    Action<MimeMessage> initializationCallbackPlusSubjectInit = (message) => 
                    {
                        message.Subject = EmailBackedKeyValueStore.GetSubjectFromUniqueStoreName(uniqueStoreName);
                        initializationCallback?.Invoke(message);
                    };
                    (resultMessage, uniqueId) = await CreateEmptyStorageMessageAsync(folder, messageIdHeaderValue, initializationCallback);
                }
                return (new MimeMessageWithUniqueId() { Message = resultMessage, UniqueId = uniqueId }, lockResult);
            }
            finally
            {
                if (unlockResource)
                {
                    var unlockResult = await remoteLock.ReleaseLock(folder, lockFolderName, lockResult.ResultingLockCookie);
                    if (!unlockResult)
                    {
                        logger.Warning("Could not unlock the following lock: {@LockResult}", lockResult);
                    }
                }
            }
        }

        public async Task<EmailBackedKeyValueStore> GetStoreForReading(IMailFolder parentFolder, string uniqueStoreName, Action<MimeMessage>? initializationCallback = null)
        {
            logger.Debug("Trying to retrieve remote KV store...");
            var (msg, lockResult) = await GetOrCreateStorageMessageInFolderAsync(parentFolder, uniqueStoreName, "__", true, initializationCallback);
            var kvStore = new EmailBackedKeyValueStore(logger, msg, uniqueStoreName);
            return kvStore;
        }

        public async Task<EmailBackedKeyValueStore> GetStoreForReading(string parentFolderFullName, string uniqueStoreName, Action<MimeMessage>? initializationCallback = null)
        {
            // TODO: improve imap connections; caching etc.
            using var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var parentFolder = await imapClient.GetFolderAsync(parentFolderFullName);
                return await GetStoreForReading(parentFolder, uniqueStoreName, initializationCallback);
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }

        public async Task<(EmailBackedKeyValueStore, LockResult)> LockStoreForWriting(IMailFolder parentFolder, string uniqueStoreName, Action<MimeMessage>? initializationCallback = null)
        {
            logger.Debug("Trying to retrieve remote KV store...");
            var (msg, lockResult) = await GetOrCreateStorageMessageInFolderAsync(parentFolder, uniqueStoreName, "__", false, initializationCallback);
            var kvStore = new EmailBackedKeyValueStore(logger, msg, uniqueStoreName);
            return (kvStore, lockResult);
        }

        public async Task<(EmailBackedKeyValueStore, LockResult)> LockStoreForWriting(string parentFolderFullName, string uniqueStoreName, Action<MimeMessage>? initializationCallback = null)
        {
            // TODO: improve imap connections; caching etc.
            using var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var parentFolder = await imapClient.GetFolderAsync(parentFolderFullName);
                return await LockStoreForWriting(parentFolder, uniqueStoreName, initializationCallback);
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }

        public async Task<UniqueId?> WriteAndUnlockStore(IMailFolder parentFolder, EmailBackedKeyValueStore? kvStore, LockResult activeLock)
        {
            if (kvStore == null)
            {
                return UniqueId.Invalid;
            }

            try
            {
                UniqueId? replacementResult = null;
                // TBD review locking
                lock (parentFolder.SyncRoot)
                {
                    parentFolder.Open(FolderAccess.ReadWrite);
                    logger.Debug("Updating existing storage message in folder {FolderPath} with ID {@ID}", parentFolder.FullName, kvStore.MessageAndId.UniqueId);
                    replacementResult = parentFolder.Replace(kvStore.MessageAndId.UniqueId, kvStore.MessageAndId.Message);
                    parentFolder.SetFlags(kvStore.MessageAndId.UniqueId, MessageFlags.Seen, true);
                }
                return replacementResult;
            }
            finally
            {
                var unlockResult = await remoteLock.ReleaseLock(parentFolder, activeLock.LockResourceName, activeLock.ResultingLockCookie);
                if (!unlockResult)
                {
                    logger.Warning("Could not unlock the following lock: {@LockResult}", activeLock);
                }
            }
        }

        public async Task<UniqueId?> WriteAndUnlockStore(string parentFolderFullName, EmailBackedKeyValueStore? kvStore, LockResult activeLock)
        {
            if (kvStore == null)
            {
                return UniqueId.Invalid;
            }

            // TODO: improve imap connections; caching etc.
            using var imapClient = await imapConnectionFactory.GetImapConnectionAsync();
            try
            {
                var parentFolder = await imapClient.GetFolderAsync(parentFolderFullName);
                return await WriteAndUnlockStore(parentFolder, kvStore, activeLock);
            }
            finally
            {
                await imapClient.DisconnectAsync(true);
            }
        }
    }
}
