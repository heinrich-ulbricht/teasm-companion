using MailKit.Net.Imap;
using MailKit.Security;
using Polly;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;

#nullable enable

namespace TeasmCompanion.Stores.Imap
{
    public class ImapConnectionFactory
    {
        private readonly string imapHostName;
        private readonly int imapPort;
        private readonly string imapUserName;
        private readonly string imapPassword;
        private readonly Configuration config;

        public ImapConnectionFactory(Configuration config)
        {
            imapHostName = config.ImapHostName;
            imapPort = config.ImapPort;
            imapUserName = config.ImapUserName ?? "";
            imapPassword = config.ImapPassword ?? "";
            this.config = config;
        }

        private async Task<ImapClient> GetImapConnectionAsyncInternal(CancellationToken cancellationToken = default)
        {
            var client = new ImapClient();
            if (config.DebugDisableEmailServerCertificateCheck)
            {
                client.ServerCertificateValidationCallback = client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            }
            try
            {
                await client.ConnectAsync(imapHostName, imapPort, SecureSocketOptions.Auto, cancellationToken); // SecureSockecOptions
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    // no server running?; terminal exception
                    throw;
                }
                throw new TeasmCompanionException("Exception while connecting to IMAP server, but we'll retry", e);
            }
            catch (ArgumentException)
            {
                // those are exceptions like host name not set etc.; terminal exception
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new TeasmCompanionException("Exception while connecting to IMAP server, but we'll retry", e);
            }
            await client.AuthenticateAsync(imapUserName, imapPassword, cancellationToken);

            return client;
        }


        /// <summary>
        /// Get an IMAP connection with built-in retry if there is a connection exception e.g. due to connection limits.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<ImapClient> GetImapConnectionAsync(CancellationToken cancellationToken)
        {
            var retryPolicy = Policy.Handle<TeasmCompanionException>().WaitAndRetryAsync(10, (retryAttempt) =>
            {
                var waitTimeSec = Math.Pow(2, retryAttempt - 1);
                return TimeSpan.FromSeconds(waitTimeSec);
            });

            return await retryPolicy.ExecuteAsync(async (cancellationToken) =>
            {
                return await GetImapConnectionAsyncInternal(cancellationToken);
            }, cancellationToken);
        }

        public virtual async Task<ImapClient> GetImapConnectionAsync()
        {
            var retryPolicy = Policy.Handle<TeasmCompanionException>().WaitAndRetryAsync(10, (retryAttempt) =>
            {
                var waitTimeSec = Math.Pow(2, retryAttempt - 1);
                return TimeSpan.FromSeconds(waitTimeSec);
            });

            return await retryPolicy.ExecuteAsync(async (cancellationToken) =>
            {
                return await GetImapConnectionAsyncInternal(cancellationToken);
            }, default);
        }

    }
}
