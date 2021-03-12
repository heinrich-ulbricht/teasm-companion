using System.Threading.Tasks;
using Ninject;
using Akavache;
using System.Reactive.Linq;
using TeasmCompanion.Misc;
using System;
using CommandLine;
using Newtonsoft.Json;
using Splat;
using System.Threading;
using System.Reflection;

#nullable enable

namespace TeasmCompanion
{
    partial class Program
    {
        static void InitAkavacheJson(Serilog.ILogger logger)
        {
            var settings = new JsonSerializerSettings();
            settings.Error += (currentObject, errorContext) =>
            {
                logger.Error("Exception in Akavache JSON handling: {@ErrorContext}", errorContext);
            };
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;
            // this is needed to de-serialize interface types; it will include type information where needed
            settings.TypeNameHandling = TypeNameHandling.Auto;
            Locator.CurrentMutable.RegisterConstant(
                settings,
                typeof(JsonSerializerSettings));
        }

        static async Task<(bool, Version?)> DidApplicationFileVersionChangeSinceLastRun(Serilog.ILogger logger)
        {
            if (Version.TryParse(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "", out Version? currentFileVersion) && currentFileVersion != null)
            {
                logger.Information("Current application file version: {Version}", currentFileVersion);
                var previousFileVersion = await BlobCache.UserAccount.GetOrCreateObject($"{Constants.AppName}.fileVersion", () => currentFileVersion);
                if (!previousFileVersion.Equals(currentFileVersion))
                {
                    logger.Information("Detected application file version change.");
                    return (true, currentFileVersion);
                } else
                {
                    return (false, currentFileVersion);
                }
            }
            else
            {
                logger.Information("Cannot determine current application file version. This can lead to problems with cached objects after version updates.");
                return (false, default);
            }
        }

        static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            CommandlineOptions? options = null;
            Parser.Default.ParseArguments<CommandlineOptions?>(args).MapResult(value => options = value, _ => null);

            IKernel kernel = new StandardKernel(new Bindings(options));
            var logger = kernel.Get<Serilog.ILogger>().ForContext<Program>();
            try
            {
                Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
                    e.Cancel = true;
                    logger.Information("SHUTTING DOWN - PLEASE WAIT...");
                    cancellationTokenSource.Cancel();
                };
                logger.Information("> PRESS CTRL+C AT ANY TIME TO SHUT DOWN THE APPLICATION");

                var cacheName = $"Heu.{Constants.AppName.Replace(" ", "")}";
                BlobCache.ApplicationName = cacheName;
                Registrations.Start(cacheName);
                BlobCache.EnsureInitialized();
                InitAkavacheJson(logger);

                var config = kernel.Get<Configuration>();
                var (didFileVersionChange, currentFileVersion) = await DidApplicationFileVersionChangeSinceLastRun(logger);
                if (config.DebugClearLocalCacheOnStart || didFileVersionChange)
                {
                    logger.Debug("Clearing cache...");
                    await BlobCache.UserAccount.InvalidateAll();
                    await BlobCache.UserAccount.Vacuum();
                    logger.Debug("DONE: Clearing cache");
                    await BlobCache.UserAccount.InsertObject($"{Constants.AppName}.fileVersion", currentFileVersion);
                }

                var monitor = kernel.Get<TeamsMonitor>();
                try
                {
                    // note: this blocks until the user cancels by pressing e.g. Ctrl+C
                    await monitor.GoAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    logger.Information("Successfully shut down Teams monitor");

                }
                logger.Information("Shutting down cache...");
                await BlobCache.Shutdown();
                logger.Information("DONE: Shutting down cache");
                logger.Information("SHUTDOWN COMPLETE <");
            }
            catch (Exception e)
            {
                logger.Error(e, "Uncaught exception; logging and terminating");
                throw;
            }
        }
    }
}
