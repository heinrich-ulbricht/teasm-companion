namespace TeasmCompanion.MessageTypes
{
    public class ThreadActivityTopicUpdateWrapper
    {
        public ThreadActivityTopicUpdate root { get; set; }
    }

    public class ThreadActivityTopicUpdate
    {
        public TopicUpdate topicupdate { get; set; }
    }

    public class TopicUpdate
    {
        public long eventtime { get; set; }
        public string initiator { get; set; }
        public string value { get; set; }
    }
}
