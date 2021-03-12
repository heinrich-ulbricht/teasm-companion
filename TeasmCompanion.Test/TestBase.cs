using Serilog;
using System.Text;

namespace TeasmCompanion.Test
{
    public class TestBase
    {
        static TestBase()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Debug()
            .WriteTo.File(@"testresult.log", encoding: Encoding.UTF8)
            .Enrich.FromLogContext()
            .CreateLogger();
        }

        public static Configuration GetConfig()
        {
            Configuration config;
            config = new Configuration()
            {
                // configuration for your local test server
                ImapHostName = "localhost",
                ImapPort = 10143,
                ImapPassword = "user@localdomain.local",
                ImapUserName = "user@localdomain.local",
                DebugDisableEmailServerCertificateCheck = true
            };
            return config;
        }
    }
}
