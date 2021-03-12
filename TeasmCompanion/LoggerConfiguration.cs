using Collector.Serilog.Enrichers.SensitiveInformation;
using Serilog;
using System.IO;
using System.Text;
using TeasmCompanion.TeamsTokenRetrieval;

namespace TeasmCompanion
{
    public class LoggerConfigurations
    {
        public static LoggerConfiguration Debug()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Destructure.HasSensitiveProperties<TeamsTokenInfo>(
                    myclass => myclass.AuthHeader,
                    myclass => myclass.TokenString)
                .Enrich.With(new SensitiveInformationEnricher())
                .WriteTo.Console(outputTemplate: "[{ThreadId:000} {Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} | {SourceContext}{NewLine}{Exception}")
                .WriteTo.File(Path.Combine("logs", "teasmcompanion.debug.log"), encoding: Encoding.UTF8, outputTemplate: "[{ThreadId:000} {Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} | {SourceContext}{NewLine}{Exception}", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
        }

        public static LoggerConfiguration Information()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Destructure.HasSensitiveProperties<TeamsTokenInfo>(
                    myclass => myclass.AuthHeader,
                    myclass => myclass.TokenString)
                .Enrich.With(new SensitiveInformationEnricher())
                .WriteTo.Console(outputTemplate: "[{ThreadId:000} {Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} | {SourceContext}{NewLine}{Exception}")
                .WriteTo.File(Path.Combine("logs", "teasmcompanion.information.log"), encoding: Encoding.UTF8, outputTemplate: "[{ThreadId:000} {Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} | {SourceContext}{NewLine}{Exception}", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
        }
    }
}
