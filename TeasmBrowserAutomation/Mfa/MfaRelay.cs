using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Signal.DBus;
using Tmds.DBus;
using Nito.AsyncEx;
using System.Collections.Concurrent;

#nullable enable

namespace TeasmBrowserAutomation.Mfa
{
    public class MfaRelay
    {
        private const string signalCliServiceName = "org.asamk.Signal";
        private const string signalCliObjectPath = "/org/asamk/Signal";
        private Connection _connection;
        private ISignal? _signal = null;
        private string _receiverMobileNumber;
        private ConcurrentQueue<string> _receivedMessages = new();
        private AsyncAutoResetEvent _newMessageSignal = new AsyncAutoResetEvent(false);
        private bool _isListeningForMessages = false;

        public MfaRelay(string receiverMobileNumber)
        {
            _connection = Connection.Session;
            _receiverMobileNumber = receiverMobileNumber;
        }

        public async Task<bool> IsRelayAvailableAsync()
        {
            try
            {
                var availableServices = await _connection.ListServicesAsync();
                return availableServices.Where(s => s.Equals(signalCliServiceName, StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            catch (ConnectException)
            {
                // on Windows we have no D-Bus
                return false;
            }
        }

        private async Task<bool> EnsureSignalProxy()
        {
            if (_signal == null && await IsRelayAvailableAsync())
            {
                _signal = _connection.CreateProxy<ISignal>(signalCliServiceName, new ObjectPath(signalCliObjectPath));
            }

            return _signal != null;
        }

        // https://github.com/AsamK/signal-cli/blob/master/man/signal-cli-dbus.5.adoc
        public async Task RegisterMessageListenerAsync()
        {
            if (!_isListeningForMessages && await EnsureSignalProxy() && _signal != null)
            {
                await _signal.WatchMessageReceivedAsync(data => MessageReceiver(data.Item1, data.Item2, data.Item3, data.Item4, data.Item5), MessageErrorHandler);
                _isListeningForMessages = true;
                //var timestamp = await signal.sendMessageAsync("test", new string[0], _receiverMobileNumber);
            }
        }

        private void MessageErrorHandler(Exception e)
        {
            // todo: add logging
            Console.WriteLine("MessageErrorHandler: " + e.ToString());
#if DEBUG
            Debugger.Break();
#endif
        }

        private void MessageReceiver(long timestamp, string sender, byte[] groupId, string body, string[] attachments)
        {
            _receivedMessages.Enqueue(body);
            _newMessageSignal.Set();
        }

        public async Task<string?> SendMessageAndWaitForReplyAsync(string messageToSend, Func<string, bool> checkMessage)
        {
            if (await EnsureSignalProxy() && _signal != null)
            {
                _receivedMessages.Clear();
                await _signal.sendMessageAsync(messageToSend, new string[0], _receiverMobileNumber);

                var foundMessage = "";
                do
                {
                    await _newMessageSignal.WaitAsync();
                    while (_receivedMessages.TryDequeue(out var receivedMessage))
                    {
                        if (checkMessage(receivedMessage))
                        {
                            foundMessage = receivedMessage;
                            break;
                        }
                    }
                } while (string.IsNullOrEmpty(foundMessage));

                return foundMessage;
            }
            return null;
        }
    }
}