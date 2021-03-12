using Newtonsoft.Json;
using Ninject.Extensions.Factory;
using Ninject.Modules;
using Serilog;
using System;
using System.IO;
using System.Text;
using TeasmCompanion.Interfaces;
using TeasmCompanion.Stores;
using TeasmCompanion.Stores.Imap;
using TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor;
using TeasmCompanion.TeamsMonitors;
using TeasmCompanion.TeamsTokenRetrieval;

#nullable enable

namespace TeasmCompanion
{
    public class Bindings : NinjectModule
    {
        CommandlineOptions? options { get; }

        public Bindings(CommandlineOptions? options) : base()
        {
            this.options = options;
        }

        public override void Load()
        {
            Configuration config;
            string jsonFilename = "config.json";
            try
            {
                if (options?.Profile != null)
                {
                    jsonFilename = $"config.{options.Profile}.json";
                }
                var json = File.ReadAllText(jsonFilename, Encoding.UTF8);
                config = JsonConvert.DeserializeObject<Configuration>(json);
            } catch (Exception e)
            {
                Console.WriteLine($"Could not read configuration from '{jsonFilename}', using default values. See README for details on how to set up a config file. The error was: {e.Message}");
                config = new Configuration();
            }

            var logger = GetLoggerConfiguration(config.LogLevel).CreateLogger();
            logger.Information("Loading configuration from [{JsonFileName}]", jsonFilename);

            Bind<ILogger>().ToConstant(logger);
            Bind<Configuration>().ToConstant(config);
            Bind<TeamsTokenRetriever>().ToSelf().InSingletonScope();
            Bind<TeamsGlobalApiAccessor>().ToSelf().InSingletonScope();
            Bind<TeamsUserTenantsRetriever>().ToSelf().InSingletonScope();
            Bind<TeamsLongPollingRegistry>().ToSelf().InSingletonScope();
            Bind<TeamsLongPollingApiAccessor>().ToSelf().InSingletonScope();
            Bind<ImapConnectionFactory>().ToSelf();
            Bind<TeamsUserRegistry>().ToSelf().InSingletonScope();
            Bind<ITeamsUserRegistry>().To<TeamsUserRegistry>().InSingletonScope();
            Bind<ITeamsUserStore>().To<ImapStore>().InSingletonScope();
            Bind<ITeamsChatRegistry>().To<TeamsChatRegistry>().InSingletonScope();
            Bind<ITeamsChatStore>().To<ImapStore>().InSingletonScope();
            Bind<IProcessedMessageFactory>().ToFactory();
            Bind<IProcessedNotificationMessageFactory>().ToFactory();
        }

        private LoggerConfiguration GetLoggerConfiguration(string loglevel)
        {
            LoggerConfiguration logconf;
            switch (loglevel)
            {
                case "Debug":
                    logconf = LoggerConfigurations.Debug();
                    break;

                case "Information":
                    logconf = LoggerConfigurations.Information();
                    break;

                default:
                    logconf = LoggerConfigurations.Debug();
                    break;
            }

            return logconf;
        }
    }
}
