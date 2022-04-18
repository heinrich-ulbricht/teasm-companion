# Inner workings of Teasm Companion

This document is about the technical details of Teasm Companion.

*Note: If you are looking for a higher level explanation please navigate a folder level up. The README there has the bigger picture.*
## Which APIs are being used?

Teasm Companion uses the same APIs as Microsoft Teams.

You find everything related to the Teams APIs and related data structures in the `TeamsInternal` folder.

## How does Teasm Companion authenticate with the Teams API?

It doesn't, a Teams client does.

When you authenticate in a Teams client you are giving Teasm Companion everything it needs. Microsoft Teams retrieves [JWT](https://en.wikipedia.org/wiki/JSON_Web_Token)-encoded tokens for every API it needs to access. Those tokens are cached in local storage and Teasm Companion will examine this storage.

## Where are those tokens coming from?

The [Electron](https://www.electronjs.org/)-based Microsoft Teams client stores has its local storage here: `C:\Users\<user>\AppData\Roaming\Microsoft\Teams\Local Storage\leveldb`.

When using Teams in Google's Chrome the local storage (for the main profile) is at `C:\Users\<user>\AppData\Local\Google\Chrome\User Data\Default\Local Storage\leveldb`.

*Note: Different Chrome profiles use different folders. Teasm Companion only supports the main profile.*

Be it the Teams desktop app, Chrome, Chromium or Chromium-based Edge - they are all some kind of Chrome. And Chrome uses a [LevelDB](https://en.wikipedia.org/wiki/LevelDB) key-value store as its local storage.

It seems to be impossible to find a .NET-based library to properly access LevelDB stores. Therefore Teasm Companion just dumps the whole local storage as text and searches it using regular expressions (all in memory). To produce the dump it uses the [ldbdump](https://github.com/golang/leveldb/tree/master/cmd/ldbdump) tool that comes with Google's [LevelDB repo](https://github.com/golang/leveldb).

You find everything token retrieval-related in the `TeamsTokenRetrieval` folder, including the list of local storage pathes Teasm Companion handles.

## Sign-in automation

TeasmCompanion is able to automate the browser-based Teams authentication dialog - see the parent directory README for details on how to configure this.

Sign-in automation uses Selenium to automate the Chrome browser. It will navigate to the browser-based version of Teams and tries to fill in all information needed to authenticate. This will succeed for user names, passwords and checkboxes like "keep me signed in". Only the second factor for multi-factor authentication needs to be entered/unlocked manually by a human - unless it's TOTP and you provide the key. In this case the code can be generated automatically as well.

Once the authentication succeeded the tokens will be extracted from this Teams session as well. Multiple concurrent sign-ins are supported.

The automation is optional and experimental. You can test and inspect its functionality in the project `TeasmBrowserAutomation`. Note: On Windows the secrets (password, TOTP key) will be stored encrypted using the Data Protection API, on Linux they will be stored in cleartext.

## Where does Teasm Companion store chat messages?

Teasm Companion stores all chat-related data as e-mails via IMAP. Server host name, port and credentials are configured in `config.json`. Thorough testing has been done using a local Dovecot IMAP server.

*Note: Please read the "You need an IMAP server" section in the README one folder level up to get information about why you need an IMAP server and which options you have.*

E-mails are used as storage backend for chat messages. The class `ImapBackedDatabase` provides an easy way to treat an e-mail as a "database" that can be locked for reading and writing. The class `EmailBackedKeyValueStore` allows to easily store key-value pairs in e-mails. And `ImapBackedRemoteLock` implements a locking mechanism that is used to prevent concurrent access to the same e-mail. It uses the CONDSTORE IMAP capability to detect concurrent changes.

Every chat is stored in its own folder.

You find everything related to IMAP storage in the `Stores/IMAP` folder.

## How are chat folder names generated?

If a chat has a custom name set this name will be used as folder name. For meeting chats the folder name will be the meeting name. If there is no name set then the folder name will be derived from the sorted list of participant display names.

Some special characters are replaces by the underscore character.

## Which message types are handled?

The following Microsoft Teams message types are handled and stored by Teasm Companion:

* Text message and HTML/Richtext message
* Add member and delete member events
* Call started and call ended events
* Member joined and member left events
* Recording started event
* Chat topic update event

Each of the above messages will be stored as a single e-mail. The e-mail contains the message sender as "From", the message text, embedded images and the raw message JSON. (Have a look at the e-mail source code to see the original JSON.)

*Note: Further parsing of Teams message data is definitely possible and desirable. Since the original message JSON is stored with every e-mail it would even be possible to "re-create" the e-mail based on extended e-mail generation code.*

You find much related to message conversion in `ProcessedMessageBase.cs`.

## How are images and attachments handled?

Images that are embedded into Teams messages are downloaded and saved as e-mail attachments.

Emoji images are replaced by their ASCII counterparts where possible. So if somebody uses the "smile" emoji image it will be replaced by ":)". Otherwise those emoji images would be stored hundreds of times as attachments.

Other files that are linked in messages are not downloaded. Those files are usually stored in SharePoint, OneDrive, Stream or other external services. The links remain, but clicking them takes you to the external service.

Message Cards (except file attachments) are not converted to HTML and thus are probably missing, although their content is visible in the e-mail source code which contains the raw JSON retrieved from the Teams API.

## Caching

There is a local cache for certain data like the rather big initial chat list that will be retrieved on first run.

The local cache can be cleared at any time - it will be populated again from live Teams data as well as from information stored on the IMAP server.

The chat will be cleared automatically when the application file version changes.

## Concurrency

There are things running in parallel, like token retrieval or storing e-mails. Where highly concurrent operations are happening locks or concurrency-aware data structures are used. Nevertheless I'm sure concurrency issues are lurking.

Some specialized e-mail messages are used as "database" and are potentially accessed by multiple threads. Those are protected from concurrent access by the "IMAP-based remote lock". A locking mechanism that is based on e-mail tags and the fact that the CONDSTORE IMAP capability allows to detect concurrent modifications of those tags. 

Everything is basically prepared for multi-client support, where multiple Teasm Companion clients are running at the same time. But this is just theory although a fun one.

At the moment you should only run one instance of Teasm Companion per e-mail account.

## Throttling

I'm not aware of any throttling at the API level. But there is throttling built into the data retrieval modules of Teasm Companion to not be excessive.

## Special folders and messages

The general folder hierarchy looks like this:

* Main User ID *<-- that's the "home tenant user ID"*
  * Tenant 1 *<-- that's a tenant the user has access to (e.g. as guest, but also the home tenant); there will be one folder per tenant*
    * Chats *<-- all chats for the tenant are stored here as subfolders*
      * Chat 1 *<-- one folder per chat*
      * Chat 2
      * ...
    * Teams *<-- for future use ;)*

Every chat folder contains a message storing chat metadata, most importantly the chat ID and versions. Its subject starts with `[METADATA]`. If you inspect its source you will see the chat's JSON representation as received by the API. If this message gets deleted the whole chat will be retrieved again and the file will be rebuilt.

The `Chats` folder contains a message containing the chat index. This index file will be used to locate the correct chat folder for a given chat ID. If it gets deleted it will be rebuilt.

Each tenant folder contains a message that is the "user store" for the tenant. Every user that has been part of a conversation is stored here. Most importantly their ID and display name. Sometimes data gotten from APIs only contain user IDs and no names. In this case the user store will be used to look up their names (if it has been discovered previously).

The special directory "__" is used to lock access to above resources. It contains a special message that is used by the locking mechanism. Ignore this directory. If you delete it it will be created again. 

## Ideas for Further Development

* store messages in local folders and Markdown files instead of IMAP messages (to get rid of the IMAP server requirement)
* retrieve channel content as well, maybe from pinned channels only
* handle chat name changes and rename the chat folders accordingly
* further message formatting: highlight mentions, store likes and stuff, ...
* test concurrently running multiple app instances; optimize for more concurrent processing in general

## Known Issues

Chat folder names that are derived from the chat participant list can get quite long; Claws Mail seems fail to open messages in those folders; Thunderbird is ok.

Chat folder names might contain double quotes which apparently Claws Mail does not like; Thunderbird is ok.

Renamed chats might cause problems as the old chat name is still the name of the folder where the chat messages are being stored.

Two chats with the same name will for sure cause problems since they get stored in the same folder. This is currently not supported.

Users that got removed from a tenant might surface by their MRI (like "8:orgid:someguid") and might as such be reflected in chat folder names. Those MRIs are not (yet) replaced by the users's display name even if the display name has been discovered. 

There might be concurrency issues lingering when accessing IMAP resources. If there is an exception just restart Teasm Companion and it will pick up where it left. Locking could be improved, although in general it seems to work well.

Notification registration for a tenant where a token only recently has been discovered (after having no valid token for some time) might fail. Just restart the application in this case. The backoff logic needs to be improved here. 

## Roadmap

There is none. This is a fun project to explore, fiddle and learn.

## Third Party Libraries and Licenses

| Name | Purpose | License
|------|---------|---------
| [Akavache](https://github.com/reactiveui/akavache/) | Local caching | [MIT](https://github.com/reactiveui/Akavache/blob/main/LICENSE)
| [Cliwrap](https://github.com/Tyrrrz/CliWrap) | Running the ldbdump command line tool and capture the result | [MIT](https://github.com/Tyrrrz/CliWrap/blob/master/License.txt)
| [Commandline](https://github.com/commandlineparser/commandline) | Processing command line parameters | [MIT](https://github.com/commandlineparser/commandline/blob/master/License.md)
| [HtmlAgilityPack](https://github.com/zzzprojects/html-agility-pack) | Parsing HTML messages e.g. to find linked images | [MIT](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)
| [LevelDB](https://github.com/golang/leveldb) [ldbdump tool](https://github.com/golang/leveldb/tree/master/cmd/ldbdump) (in binary form) | Exporting content from LevelDB key-value stores used by Chrome and the Teams desktop client | [BSD 3](https://github.com/golang/leveldb/blob/master/LICENSE)
| [Mailkit](https://github.com/jstedfast/MailKit) | Using IMAP server as message store | [MIT](https://github.com/jstedfast/MailKit/blob/master/LICENSE)
| [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) | JSON conversion | [MIT](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
| [Ninject](https://github.com/ninject/Ninject) | Dependency injection | [Apache 2.0](https://github.com/ninject/Ninject/blob/master/LICENSE.txt)
| [Optimized Priority Queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp) | Thread-safe [priority queue](https://en.wikipedia.org/wiki/Priority_queue) used for chat retrieval | [MIT](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/blob/master/LICENSE.txt)
| [Polly](https://github.com/App-vNext/Polly) | Fault-handling library that implements [cloud design patterns](https://docs.microsoft.com/en-us/azure/architecture/patterns/index-patterns) | [BSD 3](https://github.com/App-vNext/Polly/blob/master/LICENSE.txt)
| [Serilog](https://github.com/serilog/serilog) (plus enrichers and sinks) | Logging | [Apache 2.0](https://github.com/serilog/serilog/blob/dev/LICENSE)
| [Sensitive Information Enricher](https://github.com/collector-bank/serilog-enrichers-sensitiveinformation) for Serilog | Stripping sensitive tokens from log files | [Apache 2.0](https://github.com/collector-bank/serilog-enrichers-sensitiveinformation/blob/master/LICENSE)
| [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) | Handling downloaded images | [Apache 2.0](https://github.com/SixLabors/ImageSharp/blob/master/LICENSE)

_Note: License files are included in the "License Files" folder._
