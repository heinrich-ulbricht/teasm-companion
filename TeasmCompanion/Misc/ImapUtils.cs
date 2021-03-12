using MailKit;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace TeasmCompanion.Misc
{
    public class ImapUtils
    {
        public static IMailFolder? FindFolder(IMailFolder toplevel, string name)
        {
            var subfolders = toplevel.GetSubfolders().ToList();

            foreach (var subfolder in subfolders)
            {
                if (subfolder.Name == name)
                    return subfolder;
            }

            foreach (var subfolder in subfolders)
            {
                var folder = FindFolder(subfolder, name);

                if (folder != null)
                    return folder;
            }

            return null;
        }

        public static async Task VisitChildren(ILogger logger, IMailFolder parent, Func<IMailFolder, int, int, Task<bool>>? visitor)
        {
            if (visitor == null)
                return;

            var subfolders = parent.GetSubfolders().ToList();
            var subfolderCount = subfolders.Count;
            var currentSubfolderCount = 0;
            foreach (var subfolder in subfolders)
            {
                try
                {
                    if (!await visitor(subfolder, ++currentSubfolderCount, subfolderCount))
                        return;
                } catch (Exception e) when (!(e is OperationCanceledException) && !(e is TaskCanceledException))
                {
                    logger.Error(e, "Exception while visiting child folder '{ChildName}' of parent folder '{FolderFullName}', ignoring and continuing with next child", subfolder.Name, parent.FullName);
                }
            }
        }
    }
}
