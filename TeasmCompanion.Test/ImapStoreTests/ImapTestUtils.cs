using MailKit;
using MailKit.Search;
using Ninject;
using Ninject.MockingKernel.FakeItEasy;
using Serilog;
using System;
using System.Diagnostics;
using TeasmCompanion.Stores.Imap;

namespace TeasmCompanion.Test.ImapStoreTests
{
    public class ImapTestUtils
    {

        public static void CreateFolder(string testFolderName, Configuration config)
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var imapFac = kernel.Get<ImapConnectionFactory>();
            using var connection = imapFac.GetImapConnectionAsync().Result;
            var parentFolder = connection.GetFolder(connection.PersonalNamespaces[0].Path);
            var mailFolder = parentFolder.Create(testFolderName, true);
            mailFolder.Open(FolderAccess.ReadWrite);
            if (!mailFolder.PermanentFlags.HasFlag(MessageFlags.UserDefined))
            {
                //throw new TeasmCompanionException("Folder does not support user defined flags");
            }
            connection.Disconnect(true);
        }

        public static void RemoveFolder(string folderName, Configuration config)
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var imapFac = kernel.Get<ImapConnectionFactory>();
            using var connection = imapFac.GetImapConnectionAsync().Result;
            var parentFolder = connection.GetFolder(connection.PersonalNamespaces[0].Path);
            IMailFolder mailFolder;
            try
            {
                mailFolder = parentFolder.GetSubfolder(folderName);
            }
            catch
            {
                // most likely folder not found
                return;
            }
            RemoveFolder(mailFolder, config);
            connection.Disconnect(true);
        }

        public static void RemoveFolder(IMailFolder folder, Configuration config)
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<Configuration>().ToConstant(config);
            kernel.Rebind<ImapStore>().ToSelf().InSingletonScope();
            kernel.Rebind<ImapConnectionFactory>().ToSelf().InSingletonScope();
            var imapFac = kernel.Get<ImapConnectionFactory>();
            using var connection = imapFac.GetImapConnectionAsync().Result;

            var subFolders = folder.GetSubfolders();
            foreach (var subFolder in subFolders)
            {
                RemoveFolder(subFolder, config);
            }

            folder.Open(FolderAccess.ReadWrite);
            var allMessages = folder.Search(SearchQuery.All);
            folder.SetFlags(allMessages, MessageFlags.Deleted, true);
            folder.Expunge();
            folder.Close();
            try
            {
                folder.Delete();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while deleting folder: " + e.ToString());
            }
            connection.Disconnect(true);
        }

    }
}
