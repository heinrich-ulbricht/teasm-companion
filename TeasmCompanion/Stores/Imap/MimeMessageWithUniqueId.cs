using MailKit;
using MimeKit;

#nullable enable

namespace TeasmCompanion.Stores.Imap
{
    public struct MimeMessageWithUniqueId
    {
        public MimeMessage? Message { get; set; }
        public UniqueId UniqueId { get; set; }
        public string? MessageId => Message?.MessageId;
    }
}
