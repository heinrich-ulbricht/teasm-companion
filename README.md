# Teasm Companion

A companion app for Microsoft Teams. Pronounced "Teams Companion".

Created in lockdown times to counter code withdrawal. Playground for [C# 9](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9), [Reactive Extensions](http://reactivex.io/), some [Polly](https://github.com/App-vNext/Polly), [LevelDB](https://github.com/google/leveldb) and more IMAP than expected.

## What does it do?

Teasm Companion is like a new Teams client. It fetches your chats and chat messages (those you would normally see when clicking on the "Chat" icon in Microsoft Teams) and stores them locally.

Teasm Companion retrieves the following data for any of your authenticated Teams accounts:
* Chat messages (not all at once, but over time)
* Some chat metadata
* Information about chat participants
* Basic Tenant information

In essence its getting chat messages from chats and meetings you participated in and stores those messages in such a way that you can access and search them in an e-mail client of your choice (Thunderbird, Claws Mail, Outlook, ...).

_Note: It can access no more data than you can access in the Teams client._

## What's the use case?

Making sure that *you* have always access to *your* conversations. Those conversations often contain important information that must not be lost.

## How does it work?

Teasm Companion can do everything Microsoft Teams can do and is dependent on Microsoft Teams running at the same time (Desktop app, Chrome or new Edge).

Teasm Companion uses existing access tokens from Teams to access the same APIs Teams uses. It borrows those tokens from a running Teams client. So some time after you authenticate in the Teams app or in a browser tab Teasm Companion will grab those tokens and do its work.

There is no authentication done by Teasm Companion. You authenticate in Teams. The Companion will ride along.

## Why not build a "normal" Teams app?

Why not use [Microsoft Graph](https://docs.microsoft.com/en-us/graph/overview) and build an [app for Microsoft Teams](https://docs.microsoft.com/en-us/microsoftteams/platform/overview)?

It's not feasible for a variety of reasons.

## What are the prerequisites?

The following is needed to run Teasm Companion:

* Windows 10 or Linux (tested on Fedora 33)
* .NET 5.0
* A local IMAP server, see section "You need an IMAP server" below for details
* `TeamsTokenRetrieval\precompiled\ldbdump.exe` (or `ldbdump` on Linux) - which are included in this repo - must be present and executable, see [TeamsTokenRetrieval\precompiled\README.md](TeamsTokenRetrieval\precompiled\README.md) for details; you can build them yourself if you want, instructions are in the README

## How are chat messages retrieved?

Chats are retrieved in roughly the same order as they are listed in Microsoft Teams. Most recent ones are retrieved first.

### Initial Chat Retrieval

When you start Teasm Companion for the first time it retrieves a list of all your chats (*without* messages), _just like Teams does_.

This will only be done once. The data will be cached and further on only changed and new chats will be retrieved. _Just like Teams does it._

### Chat Retrieval Pace

Teasm Companion plays nice with the APIs by not requesting too many chat messages at once. Thus only chats from the past days are retrieved a bit "faster" and after that chats are retrieved more "slowly". (See the Configuration section on how to configure "fast" and "slow".)

### Live Chat Notifications

Teasm Companion also subscribes to chat notifications (_just like Teams does_) and as soon as it receives a "new chat message" notification the message is stored right away as a kind of "draft". This draft is later updated when the whole chat is updated incrementally.

### Incremental Chat Updates

Teasm Companion regularely checks for chats with new messages and gets only those messages. It retrieves the list of changed chats _just like Teams does it_ and updates them, leaving old chats untouched.
## You need an IMAP server

Why IMAP? Because chat messages map pretty well to e-mails. There's a sender, a list of recipients, a message, attachments and so on. (Apparently there is a whole chat client out there [built on the basis of IMAP](https://delta.chat/en/).)

And you get a proven user interface. Any e-mail client fits the purpose and allows to read, manage and search chat messages.

Since Teasm Companion needs the [CONDSTORE](https://tools.ietf.org/html/rfc4551) IMAP capability to implement locking you have a reduced set of IMAP servers and services available with Dovecot being the most popular one. Have a look at the "[IMAP extensions and server support](https://www.imapwiki.org/Specs)" table to see all your options. 

It's best to use a local Dovecot IMAP server as this is guaranteed to work well. There are plenty of Docker images out there giving you exactly that even on Windows. For example with [docker-imap-devel](https://github.com/antespi/docker-imap-devel) you get a working and lightweight server set up in no time.

A remote server could also work but the code is absolutely not optimized for this use case. Also, be careful. Don't put your data at risk in places where it doesn't belong or can get lost.

## What can be configured?

You don't really have to configure anything, except for the `Imap*` properties. All other properties have reasonable default values.

Rename `config.template.json` to `config.json`. Then, in `config.json`, you can configure the following:

| Setting | Data type | Meaning | Sample value
|---------|-----------|---------|-------------
| ImapHostName | text  | Host name of the IMAP server | "localhost"
| ImapPort | number | IMAP port | 10143
| ImapUserName | text | User name for the IMAP account | "user@localdomain.local"
| ImapPassword | text | Password for the IMAP account | "correct horse battery staple"
| FastChatRetrievalDays | number | Number of days to perform "fast chat retrieval" for (used to quickly pull recent chats) | 7
| FastChatRetrievalWaitTimeMin | number | Maximum number of minutes to wait between retrieving chats in "fast retrieval mode" | 1
| SlowChatRetrievalWaitTimeMin | number | Maximum number of minutes to wait between retrieving chats in "slow retrieval mode" | 30
| UpdatedChatRetrievalWaitTimeSec | number | Number of seconds to wait after updating an existing chat | 5
| ResolveUnknownUserIdsJobIntervalMin | number | A job runs periodically to resolve unknown user IDs in already stored messages; this is its interval in minutes | 10
| TenantIdsToNotSubscribeForNotifications | array of text | IDs of tenants that should not be watched for notifications (use for inactive tenants) | [ "00000000-0000-beef-0000-000000000000" ]
| ChatIdIgnoreList | array of text | IDs of chats that should not be retrieved | [ "19:something@unq.gbl.spaces" ]
| DebugDisableEmailServerCertificateCheck | boolean | Ignore IMAP server certificate errors; use this for local IMAP servers or testing | false
| DebugClearLocalCacheOnStart | boolean | Clear the local cache on start; note: after application updates the cache is cleared automatically | false
| LogLevel | text; one of "Debug" or "Information" | How much information do you want to see in the console and log files? | "Debug"

Again, the IMAP configuration is the most important that must be set.

## Logging

Teasm Companion comes with extensive logging. It is fun watching the notifications flow into the console as you interact within Teams. 

The default log level is "Debug" which shows nearly everything the application does. Logs are shown in the console window and are also stored in the "logs" sub-directory. Log files are automatically removed after a couple of days.

## How do I run it?

Clone the repo. Use Visual Studio or Visual Studio Code to build `TeasmCompanion`.

Then create and configure a `config.json` and run `TeasmCompanion.exe` (or `TeasmCompanion.dll` on Linux).

## Troubleshooting

### Teasm Companion cannot find a token for initial chat list retrieval

Tokens for initial chat retrieval have a lifetime of about an hour and need to be renewed afterwarts. You can trigger renewal in your Teams client by switching to another tenant and back. You can also clear the cache of either the Teams client or your browser, then restart/reload the client. That should renew the token as well.

*Note: The tokens needed to get the actual chat messages per chat have a alonger lifetime of >1 day, so once the chat list has been retrieved you can let it run unattended for some time. The Teams clients might even renew this token from time to time automatically.* 

### Some e-mails seem empty - why?

Configure your e-mail client to always show or prioritize HTML content. Some e-mails might come with empty "text" content while having only its "HTML" content set.

## Disclaimer

There is no warranty. Assess your risk and proceed accordingly. Everything might break at any time.