using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

#nullable enable

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]
namespace Signal.DBus
{
    [DBusInterface("org.asamk.Signal")]
    interface ISignal : IDBusObject
    {
        Task<string> versionAsync();
        Task<bool> isRegisteredAsync();
        Task setContactBlockedAsync(string arg0, bool arg1);
        Task setGroupBlockedAsync(byte[] arg0, bool arg1);
        Task joinGroupAsync(string arg0);
        Task quitGroupAsync(byte[] arg0);
        Task<byte[]> updateGroupAsync(byte[] arg0, string arg1, string[] arg2, string arg3);
        Task updateProfileAsync(string arg0, string arg1, string arg2, string arg3, bool arg4);
        Task<bool> isMemberAsync(byte[] arg0);
        Task<long> sendGroupRemoteDeleteMessageAsync(long arg0, byte[] arg1);
        Task<long> sendRemoteDeleteMessageAsync(long arg0, string[] arg1);
        Task<long> sendRemoteDeleteMessageAsync(long arg0, string arg1);
        Task<long> sendMessageAsync(string arg0, string[] arg1, string arg2);
        Task<long> sendMessageAsync(string arg0, string[] arg1, string[] arg2);
        Task<long> sendMessageReactionAsync(string arg0, bool arg1, string arg2, long arg3, string arg4);
        Task<long> sendMessageReactionAsync(string arg0, bool arg1, string arg2, long arg3, string[] arg4);
        Task<long> sendNoteToSelfMessageAsync(string arg0, string[] arg1);
        Task sendEndSessionMessageAsync(string[] arg0);
        Task<long> sendGroupMessageAsync(string arg0, string[] arg1, byte[] arg2);
        Task<long> sendGroupMessageReactionAsync(string arg0, bool arg1, string arg2, long arg3, byte[] arg4);
        Task<string> getContactNameAsync(string arg0);
        Task setContactNameAsync(string arg0, string arg1);
        Task<byte[][]> getGroupIdsAsync();
        Task<string> getGroupNameAsync(byte[] arg0);
        Task<string[]> getGroupMembersAsync(byte[] arg0);
        Task<string[]> listNumbersAsync();
        Task<string[]> getContactNumberAsync(string arg0);
        Task<bool> isContactBlockedAsync(string arg0);
        Task<bool> isGroupBlockedAsync(byte[] arg0);
        Task<IDisposable> WatchSyncMessageReceivedAsync(Action<(long, string, string, byte[], string, string[])> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchReceiptReceivedAsync(Action<(long, string)> handler, Action<Exception>? onError = null);
        Task<IDisposable> WatchMessageReceivedAsync(Action<(long, string, byte[], string, string[])> handler, Action<Exception>? onError = null);
    }
}