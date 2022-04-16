using System;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using CliWrap;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using Serilog;
using TeasmCompanion.Registries;
using TeasmCompanion.Interfaces;
using CliWrap.Exceptions;
using System.Runtime.InteropServices;
using System.Threading;
using TeasmCompanion.Misc;
using Newtonsoft.Json;
using TeasmCompanion.TeamsTokenRetrieval.Model;
using TeasmBrowserAutomation.Credentials;
using TeasmBrowserAutomation.Automation;

#nullable enable

namespace TeasmCompanion.TeamsTokenRetrieval
{
    public class TeamsTokenRetriever
    {

        public readonly IObservable<TeamsTokenInfo> TokenSource;
        private readonly ReplaySubject<TeamsTokenInfo> tokenSource = new ReplaySubject<TeamsTokenInfo>();
        private readonly Dictionary<TeamsParticipant, TeamsUserTokenContext> userTokenContexts = new Dictionary<TeamsParticipant, TeamsUserTokenContext>();
        private readonly TeamsTokenPathes tokenPathes;
        private readonly LevelDbLogFileDecoder levelDbLogFileDecoder;
        private ILogger logger { get; }
        private readonly Dictionary<string, DateTime> alreadyHandledFilePathes = new Dictionary<string, DateTime>();
        private readonly Configuration configuration;
        private readonly LoginAutomation loginAutomation;

        public TeamsTokenRetriever(ILogger logger, Configuration configuration, TeamsTokenPathes tokenPathes, LevelDbLogFileDecoder levelDbLogFileDecoder)
        {
            this.logger = logger.ForContext<TeamsTokenRetriever>();

            // filter out duplicates when reading tokens repeatedly
            TokenSource = tokenSource.Distinct(t => (t.TokenType, t.UserId, t.ValidFromUtc, t.ValidToUtc));
            this.tokenPathes = tokenPathes;
            this.levelDbLogFileDecoder = levelDbLogFileDecoder;
            this.configuration = configuration;
            this.loginAutomation = new LoginAutomation(configuration.MobileNumberForSignalMfaRelay);
        }

        public TeamsUserTokenContext? GetUserTokenContext(TeamsParticipant userId, bool createIfNotExisting)
        {
            if (!userTokenContexts.TryGetValue(userId, out var context) && createIfNotExisting)
            {
                context = new TeamsUserTokenContext(userId);
                userTokenContexts.Add(userId, context);
            }

            return context;
        }

        public TeamsUserTokenContext GetOrCreateUserTokenContext(TeamsParticipant userId)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return GetUserTokenContext(userId, true); // cannot be null
#pragma warning restore CS8603 // Possible null reference return.
        }

        // note keyAndValue can contain multiple tokens, if coming from levelDb log file chunk
        private void ExtractToken(string keyAndValue, TeamsTokenType tokenType, Func<string> getUserIdAndTokenPattern, Func<string, string> generateAuthHeader)
        {
            var pattern = getUserIdAndTokenPattern();
            var matches = Regex.Match(keyAndValue, pattern);
            if (matches.Success && matches.Groups.Count == 3)
            {
                var userId = (TeamsParticipant)matches.Groups[1].Value;
                if (!userId.IsValid)
                {
                    logger.Debug("Got user ID {TeamsUserId} via RegEx but it is invalid as per TeamsUserId validity check; ignoring", userId);
                    return;
                }
            
                string token = matches.Groups[2].Value;
                // \x01{\"skypeToken\":\"THETOKEN\",\"expiration\":1111111111111,\"userDetails\":{\"licenseDetails\":{\"isFreemium\":false,\"isBasicLiveEventsEnabled\":true,\"isTrial\":false,\"isAdvComms\":false},\"regionSettings\":{\"isUnifiedPresenceEnabled\":true,\"isOutOfOfficeIntegrationEnabled\":true,\"isContactMigrationEnabled\":true,\"isAppsDiscoveryEnabled\":true,\"isFederationEnabled\":true},\"region\":\"emea\"}}",
                var jwtToken = new JwtSecurityToken(token);
                if (jwtToken.ValidFrom > DateTime.UtcNow || jwtToken.ValidTo < DateTime.UtcNow)
                {
                    var tenantId = jwtToken.Claims.Where(c => c.Type == "tid").FirstOrDefault()?.Value;
                    logger.Debug("Invalid token, ignoring: Tenant {TenantId}, User {UserId}, {TokenType,25} token (valid from: {ValidFrom}, valid to: {ValidTo})", tenantId.Truncate(Constants.UserIdLogLength, true), userId.Truncate(Constants.UserIdLogLength, true), tokenType, jwtToken.ValidFrom, jwtToken.ValidTo);
                }
                else
                {
                    var authHeaderWithToken = generateAuthHeader(token);
                    var userContext = GetOrCreateUserTokenContext(userId);
                    var tokenInfo = new TeamsTokenInfo(userId, tokenType, token, authHeaderWithToken, jwtToken.ValidFrom, jwtToken.ValidTo);
                    logger.Debug("VALID TOKEN found: {@Token}", tokenInfo);
                    userContext.AddOrReplaceTokenInfo(tokenType, tokenInfo);
                    tokenSource.OnNext(tokenInfo);
                    //var item = cache.Get(tuple);
                    //cache.Set(tuple, authHeaderWithToken, jwtToken.ValidTo - DateTime.UtcNow);
                }
                ExtractToken(keyAndValue.Substring(matches.Groups[2].Index + matches.Groups[2].Length), tokenType, getUserIdAndTokenPattern, generateAuthHeader);
            }
        }
        public async Task<List<string>> CaptureTokensFromLevelDbLogFilesAsync(CancellationToken cancellationToken = default)
        {
            return await CaptureTokensFromLevelDbLogFilesAsync(this.tokenPathes, cancellationToken);
        }

        private async Task<List<string>> CaptureTokensFromLevelDbLogFilesAsync(TeamsTokenPathes tokenPathes, CancellationToken cancellationToken = default)
        {
            return await levelDbLogFileDecoder.ReadLevelDbLogFilesAsync(tokenPathes, fullRecord => ExtractTokensFromText(fullRecord), cancellationToken);
        }

        private void ExtractEndpoints(string value)
        {
            // ts.00000000-0000-beef-0000-000000000000.auth.gtm.table
            var pattern = @"_https://teams\.microsoft\.com(?:.){2,10}ts\.([a-zA-Z0-9-]+?)\.auth\.gtm\.table.*?\\?""chatService\\?"":\\?""(.+?)\\?""";
            var matches = Regex.Match(value, pattern);
            if (matches.Success && matches.Groups.Count == 3)
            {
                var userId = (TeamsParticipant)matches.Groups[1].Value;
                string? url = matches.Groups[2].Value;

                var userContext = GetOrCreateUserTokenContext(userId);
                userContext.ChatServiceUrl = url;
            }
        }

        private void ExtractRunningCalls(string value)
        {
            var pattern = @"_https://teams\.microsoft\.com(?:.){2,10}ts\.([a-zA-Z0-9-]+?)\.CallingDropsCollectorService:CallEntries.*?(?:(\[{.*?}\])|\[\])";
            var matches = Regex.Match(value, pattern);
            if (matches.Success && matches.Groups.Count == 3 && matches.Groups[2].Success)
            {
                var userId = (TeamsParticipant)matches.Groups[1].Value;
                string jsonLiteral = matches.Groups[2].Value;
                jsonLiteral = jsonLiteral.Replace("\\\"", "\"");
                try
                {
                    var data = JsonConvert.DeserializeObject<List<CallingDropsCollectorService_CallEntries>>(jsonLiteral);
                    logger.Debug("Detected (previously) running call: {@data}", data);
                }
                catch (Exception e)
                { // ignore for now, it's not so important
                    logger.Error(e, "Error while detecting running calls: JSON literal: {@JsonLiteral}", jsonLiteral);
                }
            }
        }

        // note: this approach did not work out and needs to be refactored away
        private static Dictionary<string, TeamsTokenType> ApiTokenType = new Dictionary<string, TeamsTokenType>(){
            { "https://emea.ng.msg.teams.microsoft.com/v1/users/ME/conversations", TeamsTokenType.MyChatsAuthHeader },
            { "https://emea.ng.msg.teams.microsoft.com/v1/users/ME/properties", TeamsTokenType.MyChatsAuthHeader },
            { "https://teams.microsoft.com/api/csa/api/v1/teams/users/me", TeamsTokenType.MyTeamsAuthHeader },
            { "https://teams.microsoft.com/api/mt/emea/beta/users/tenants", TeamsTokenType.MyTenantsAuthHeader },
            { "https://teams.microsoft.com/api/mt/emea/beta/users/fetch", TeamsTokenType.MyTenantsAuthHeader }
        };

        private void ExtractTokensFromText(string text)
        {
            // Skype token for getting conversations
            // GET https://emea.ng.msg.teams.microsoft.com/v1/users/ME/conversations
            // GET https://emea.ng.msg.teams.microsoft.com/v1/users/ME/properties
            // ================================================================================ 
            // "_https://teams.microsoft.com\x00\x01ts.00000000-0000-beef-0000-000000000000.auth.skype.token\x01\a\x00\x00\x00\x00\x00\x00"
            ExtractToken(text,
                TeamsTokenType.MyChatsAuthHeader,
                () => @"_https://teams\.microsoft\.com(?:.){2,10}ts\.([a-zA-Z0-9-]+?)\.auth\.skype\.token.*?\\?""skypeToken\\?"":\\?""(.+?)\\?""",
                (skypeToken) => $"skypetoken={skypeToken}"
                );

            // Bearer token for getting my teams
            // https://teams.microsoft.com/api/csa/api/v1/teams/users/me
            // ===============================================
            // "_https://teams.microsoft.com\x00\x01ts.00000000-0000-beef-0000-000000000000.cache.token.https://chatsvcagg.teams.microsoft.com\x01\"\x00\x00\x00\x00\x00\x00"
            ExtractToken(text,
                TeamsTokenType.MyTeamsAuthHeader,
                // note: in the "log" file there are not backslashes yet, therefore they are optional
                () => @"_https://teams\.microsoft\.com(?:.){2,10}ts\.([a-zA-Z0-9-]+?)\.cache\.token\.https://chatsvcagg\.teams\.microsoft\.com.*?\\?""token\\?"":\\?""(.+?)\\?""",
                (bearerToken) => $"Bearer {bearerToken}"
                );

            // Bearer token for getting my teams
            // GET https://teams.microsoft.com/api/mt/emea/beta/users/tenants
            // GET https://teams.microsoft.com/api/mt/emea/beta/users/Heinrich.Ulbricht%contoso.com/?throwIfNotFound=false&isMailAddress=true&enableGuest=true&includeIBBarredUsers=true&skypeTeamsInfo=true
            // GET https://teams.microsoft.com/api/mt/emea/beta/users/8:orgid:00000000-0000-beef-0000-000000000000/?throwIfNotFound=false&isMailAddress=false&enableGuest=true&includeIBBarredUsers=true&skypeTeamsInfo=true HTTP/1.1
            // ===============================================

            // "_https://teams.microsoft.com\x00\x01ts.00000000-0000-beef-0000-000000000000.cache.token.https://api.spaces.skype.com\x01\t\x00\x00\x00\x00\x00\x00"
            ExtractToken(text,
                TeamsTokenType.MyTenantsAuthHeader,
                // the first non-capturing group is like "\\x00\\x01" for the leveldb file, but different for the log
                () => @"_https://teams\.microsoft\.com(?:.){2,10}ts\.([a-zA-Z0-9-]+?)\.cache\.token\.https://api.spaces.skype.com.*?\\?""token\\?"":\\?""(.+?)\\?""",
                (bearerToken) => $"Bearer {bearerToken}"
                );

            ExtractEndpoints(text);
            ExtractRunningCalls(text);
        }

        public async Task CaptureTokensFromLevelDbLdbFilesAsync(CancellationToken cancellationToken = default)
        {
            await CaptureTokensFromLevelDbLdbFilesAsync(this.tokenPathes, cancellationToken);
        }

        private async Task CaptureTokensFromLevelDbLdbFilesAsync(TeamsTokenPathes tokenPathes, CancellationToken cancellationToken = default)
        {
            logger.Debug("Start: CaptureTokens...");
            var ldbFiles = tokenPathes.GetLevelDbLdbFilePathes();
            var stdOutBuffer = new StringBuilder();
#pragma warning disable SYSLIB0012 // Type or member is obsolete
            var currentAppDirectoryPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase ?? ".").LocalPath) ?? ".";
#pragma warning restore SYSLIB0012 // Type or member is obsolete
            string ldbExecutableFilePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ldbExecutableFilePath = Path.Combine(currentAppDirectoryPath, "TeamsTokenRetrieval", "precompiled", "ldbdump.exe");
            } else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ldbExecutableFilePath = Path.Combine(currentAppDirectoryPath, "TeamsTokenRetrieval", "precompiled", "ldbdump");
            } else {
                var errorMessage = $"Unsupported platform for ldbdump (as of now). Exiting.";
                logger.Error(errorMessage);
                throw new TeasmCompanionException(errorMessage);

            }

            if (!File.Exists(ldbExecutableFilePath))
            {
                var errorMessage = $"Cannot find ldbdump(.exe) at path '{ldbExecutableFilePath}'. See README for details. Exiting.";
                logger.Error(errorMessage);
                throw new TeasmCompanionException(errorMessage);
            }
            foreach (var path in ldbFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                if (!File.Exists(path))
                    continue;

                var skipFile = false;
                var cmd = Cli
                    .Wrap(ldbExecutableFilePath)
                    .WithArguments(@"""" + path + @"""") | stdOutBuffer;

                var retryCount = 0;
                while (true)
                {
                    try
                    {
                        var fileLastWriteTime = File.GetLastWriteTime(path);
                        if (alreadyHandledFilePathes.TryGetValue(path, out var previousFileLastWriteTime) && previousFileLastWriteTime == fileLastWriteTime)
                        {
                            logger.Debug("Already handled {Path}, skipping", path);
                            break;
                        }

                        await cmd.ExecuteAsync(cancellationToken);
                        if (!alreadyHandledFilePathes.TryAdd(path, fileLastWriteTime))
                        {
                            alreadyHandledFilePathes[path] = fileLastWriteTime;
                        }
                        logger.Debug("Handled {Path}", path);
                        break;
                    }
                    catch (CommandExecutionException e)
                    {
                        if (!File.Exists(path))
                            continue;

                        // retry some time, but then stop
                        if (retryCount++ > 5)
                        {
                            skipFile = true;
                            break;
                        }
                        logger.Information(e, "Got an exception while dumping ldb, this is worth a retry");
                        await Task.Delay(3000, cancellationToken);
                    }
                }
                if (skipFile)
                {
                    logger.Information("Cannot access '{Path}', skipping file", path);
                    continue;
                }
            }
            var s = stdOutBuffer.ToString();
            var lines = s.Split("\n");

//#if DEBUG
//            var tempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Heu.TeasmCompanion", "Temp");
//            Directory.CreateDirectory(tempPath);
//            using (StreamWriter outputFile = new StreamWriter(Path.Combine(tempPath, "leveldb.txt"), false))
//            {
//                foreach (var l in lines)
//                {
//                    outputFile.WriteLine(l);
//                }
//            }
//#endif

            foreach (var line in lines)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                ExtractTokensFromText(line);
            }
            logger.Debug("Done: CaptureTokens");
        }

        public TeamsTokenInfo? GetAnyTokenToRetrieveTenantList()
        {
            return userTokenContexts.Select(t => t.Value[TeamsTokenType.MyTenantsAuthHeader]).Where(o => o != null).FirstOrDefault();
        }

        public TeamsTokenInfo? GetTokenForIdentity(TeamsParticipant userId, string url)
        {
            var context = GetOrCreateUserTokenContext(userId);
            if (ApiTokenType.TryGetValue(url, out var tokenType))
            {
                return context[tokenType];
            }
            else
            {
                throw new TeasmCompanionException("Handle the case where we need user-dependent hosts");
            }
        }

        public IEnumerable<TeamsUserTokenContext> GetIdentitiesWithToken(TeamsTokenType tokenType)
        {
            return userTokenContexts.Values.Where(ctx => ctx[tokenType] != null);
        }

        public async Task CaptureTokensFromAutomatedBrowsersAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(configuration.ChromeBinaryPath) || string.IsNullOrWhiteSpace(configuration.WebDriverDirPath))
            {
                return;
            }

            foreach (var autoLoginTenant in configuration.AutoLogin)
            {
                if (string.IsNullOrWhiteSpace(autoLoginTenant.AccountEmail))
                {
                    logger.Warning("AccountEmail of configured auto-login is empty but mustn't be. Skipping tenant {AutoLoginAccount}.", autoLoginTenant);
                    continue;
                }

                var passwordSource = new PasswordSource();
                var (password, userDataDirPath) = await passwordSource.GetPasswordAndUserDataDirForAsync(autoLoginTenant.AccountEmail, autoLoginTenant.TenantId);

                logger.Information("Starting auto-login for {AutoLoginAccount}...", autoLoginTenant);
                var result = await loginAutomation.LogInToTeamsAsync(
                    configuration.ChromeBinaryPath, 
                    configuration.WebDriverDirPath, 
                    userDataDirPath, 
                    autoLoginTenant.AccountEmail, 
                    password, 
                    (async () => { 
                        var (newPassword, _) = await passwordSource.GetPasswordAndUserDataDirForAsync(autoLoginTenant.AccountEmail, autoLoginTenant.TenantId, true); 
                        return newPassword; 
                    }),                    
                    autoLoginTenant.TenantId, 
                    false);
                if (result == LoginStage.Teams)
                {
                    logger.Information("Auto-login for {AutoLoginAccount} was successful!", autoLoginTenant);
                    var path = Path.Combine(userDataDirPath, "Default", "Local Storage", "leveldb");
                    var tokenPathes = new TeamsTokenPathesCustom(this.configuration, new List<string>() {path});
                    await CaptureTokensFromLevelDbLdbFilesAsync(tokenPathes, cancellationToken);
                    await CaptureTokensFromLevelDbLogFilesAsync(tokenPathes, cancellationToken);

                } else 
                {
                    logger.Warning("Auto-login for {AutoLoginAccount} failed, result stage: {Stage}", autoLoginTenant, result);
                }
            }            
        }
    }
}
