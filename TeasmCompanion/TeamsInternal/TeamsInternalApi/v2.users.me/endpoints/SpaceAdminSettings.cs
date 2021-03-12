namespace TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints
{
    public class Spaceadminsettings
    {
        public Usersettings userSettings { get; set; }
        public Guestsettings guestSettings { get; set; }
    }

    public class Usersettings
    {
        public bool giphyEnabled { get; set; }
        public bool stickersEnabled { get; set; }
        public int giphyRating { get; set; }
        public bool memesEnabled { get; set; }
        public bool teamMention { get; set; }
        public bool channelMention { get; set; }
        public bool customMemesEnabled { get; set; }
        public bool editEnabled { get; set; }
        public bool deleteEnabled { get; set; }
        public bool adminDeleteEnabled { get; set; }
        public bool installApp { get; set; }
        public bool uninstallApp { get; set; }
        public bool uploadCustomApp { get; set; }
        public bool createTopic { get; set; }
        public bool updateTopic { get; set; }
        public bool deleteTopic { get; set; }
        public bool createTab { get; set; }
        public bool deleteTab { get; set; }
        public bool createIntegration { get; set; }
        public bool updateIntegration { get; set; }
        public bool deleteIntegration { get; set; }
        public bool createPrivateSpace { get; set; }
        public bool teamMemesEnabled { get; set; }
        public bool messageThreading { get; set; }
        public int generalChannelSetting { get; set; }
        public bool addDisplayContent { get; set; }
        public bool removeDisplayContent { get; set; }
    }

    public class Guestsettings
    {
        public bool installApp { get; set; }
        public bool uninstallApp { get; set; }
        public bool uploadCustomApp { get; set; }
        public bool createTopic { get; set; }
        public bool updateTopic { get; set; }
        public bool deleteTopic { get; set; }
        public bool createTab { get; set; }
        public bool deleteTab { get; set; }
        public bool createIntegration { get; set; }
        public bool updateIntegration { get; set; }
        public bool deleteIntegration { get; set; }
        public bool createPrivateSpace { get; set; }
    }
}
