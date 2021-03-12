using CommandLine;

#nullable enable

namespace TeasmCompanion
{
    public class CommandlineOptions
    {
        [Option('p', "profile", Required = false, HelpText = "Choose profile. Determines the infix for config.js, like \"config.profile.js\".")]
        public string? Profile { get; set; }
    }
}
