using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TeasmCompanion.Registries;

namespace TeasmCompanion.Test
{
    [TestClass]
    public class TestParticipantIds : TestBase
    {
        [TestMethod]
        public void TestParticipantCreation()
        {
            TeamsParticipant participant;

            participant = new TeamsParticipant("28:00000000-0000-beef-0000-000000000000");
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("00000000-0000-beef-0000-000000000000", participant.Id);
            Assert.AreEqual(ParticipantKind.AppOrBot, participant.Kind);
            participant = JsonConvert.DeserializeObject<TeamsParticipant>(JsonConvert.SerializeObject(participant));
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("00000000-0000-beef-0000-000000000000", participant.Id);
            Assert.AreEqual(ParticipantKind.AppOrBot, participant.Kind);

            participant = new TeamsParticipant("8:orgid:00000000-0000-beef-0000-000000000000");
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("00000000-0000-beef-0000-000000000000", participant.Id);
            Assert.AreEqual(ParticipantKind.User, participant.Kind);
            participant = JsonConvert.DeserializeObject<TeamsParticipant>(JsonConvert.SerializeObject(participant));
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("00000000-0000-beef-0000-000000000000", participant.Id);
            Assert.AreEqual(ParticipantKind.User, participant.Kind);

            participant = new TeamsParticipant("00000000-0000-beef-0000-000000000000");
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("00000000-0000-beef-0000-000000000000", participant.Id);
            Assert.AreEqual(ParticipantKind.Unknown, participant.Kind);
            participant = JsonConvert.DeserializeObject<TeamsParticipant>(JsonConvert.SerializeObject(participant));
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("00000000-0000-beef-0000-000000000000", participant.Id);
            Assert.AreEqual(ParticipantKind.Unknown, participant.Kind);

            participant = new TeamsParticipant("https://emea.ng.msg.teams.microsoft.com/v1/users/ME/contacts/28:0af95b67-5890-4306-9c1c-a8591cead09e");
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("0af95b67-5890-4306-9c1c-a8591cead09e", participant.Id);
            Assert.AreEqual(ParticipantKind.AppOrBot, participant.Kind);
            participant = JsonConvert.DeserializeObject<TeamsParticipant>(JsonConvert.SerializeObject(participant));
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("0af95b67-5890-4306-9c1c-a8591cead09e", participant.Id);
            Assert.AreEqual(ParticipantKind.AppOrBot, participant.Kind);

            participant = (TeamsParticipant)"19:00000000-0000-beef-0000-000000000000_00000000-0000-beef-0000-000000000000@unq.gbl.spaces";
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("19:00000000-0000-beef-0000-000000000000_00000000-0000-beef-0000-000000000000@unq.gbl.spaces", participant.Id);
            Assert.AreEqual(ParticipantKind.TeamsChat, participant.Kind);
            participant = JsonConvert.DeserializeObject<TeamsParticipant>(JsonConvert.SerializeObject(participant));
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("19:00000000-0000-beef-0000-000000000000_00000000-0000-beef-0000-000000000000@unq.gbl.spaces", participant.Id);
            Assert.AreEqual(ParticipantKind.TeamsChat, participant.Kind);

            participant = new TeamsParticipant("https://notifications.skype.net/v1/users/ME/contacts/28:integration:t0ri00alov");
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("t0ri00alov", participant.Id);
            Assert.AreEqual(ParticipantKind.AppOrBot, participant.Kind);
            participant = JsonConvert.DeserializeObject<TeamsParticipant>(JsonConvert.SerializeObject(participant));
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("t0ri00alov", participant.Id);
            Assert.AreEqual(ParticipantKind.AppOrBot, participant.Kind);

            participant = new TeamsParticipant("8:teamsvisitor:a111a1a111aa111a1aa1aa1a11aaaaa1");
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("a111a1a111aa111a1aa1aa1a11aaaaa1", participant.Id);
            Assert.AreEqual(ParticipantKind.User, participant.Kind);
            participant = JsonConvert.DeserializeObject<TeamsParticipant>(JsonConvert.SerializeObject(participant));
            Assert.IsTrue(participant.IsValid);
            Assert.AreEqual("a111a1a111aa111a1aa1aa1a11aaaaa1", participant.Id);
            Assert.AreEqual(ParticipantKind.User, participant.Kind);

            participant = (TeamsParticipant)null;
            Assert.IsFalse(participant.IsValid);
            Assert.IsNull(participant.Id);
            Assert.AreEqual(ParticipantKind.Unknown, participant.Kind);
            participant = JsonConvert.DeserializeObject<TeamsParticipant>(JsonConvert.SerializeObject(participant));
            Assert.IsFalse(participant.IsValid);
            Assert.IsNull(participant.Id);
            Assert.AreEqual(ParticipantKind.Unknown, participant.Kind);
        }

        [TestMethod]
        public void TestWithAndWithoutPrefix()
        {
            TeamsParticipant p;
            // any id without prefix is not valid
            p = (TeamsParticipant)"name";
            Assert.AreEqual(ParticipantKind.Unknown, p.Kind);
            Assert.IsFalse(p.IsValid);

            // any id with prefix is valid
            p = (TeamsParticipant)"28:integration:name";
            Assert.AreEqual(ParticipantKind.AppOrBot, p.Kind);
            Assert.IsTrue(p.IsValid);

            // guid without prefix is valid
            p = (TeamsParticipant)"00000000-0000-beef-0000-000000000000";
            Assert.AreEqual(ParticipantKind.Unknown, p.Kind);
            Assert.IsTrue(p.IsValid);
        }

        [TestMethod]
        public void TestComparison()
        {
            Assert.IsTrue(((TeamsParticipant)"a").Equals((TeamsParticipant)"a"));
            Assert.IsFalse(((TeamsParticipant)"a").Equals((TeamsParticipant)"b"));
            Assert.IsFalse(((TeamsParticipant)"b").Equals((TeamsParticipant)"a"));
            Assert.IsFalse(((TeamsParticipant)null).Equals((TeamsParticipant)null));

            Assert.IsTrue(((TeamsParticipant)"00000000-0000-beef-0000-000000000000").Equals((TeamsParticipant)"00000000-0000-beef-0000-000000000000"));
            Assert.IsFalse(((TeamsParticipant)"11111111-1111-beef-1111-111111111111").Equals((TeamsParticipant)"00000000-0000-beef-0000-000000000000"));

            Assert.IsTrue(((TeamsParticipant)"8:00000000-0000-beef-0000-000000000000").Equals((TeamsParticipant)"8:00000000-0000-beef-0000-000000000000"));
            Assert.IsFalse(((TeamsParticipant)"8:11111111-1111-beef-1111-111111111111").Equals((TeamsParticipant)"8:00000000-0000-beef-0000-000000000000"));

            Assert.IsTrue(((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000").Equals((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000"));
            Assert.IsFalse(((TeamsParticipant)"8:orgid:11111111-1111-beef-1111-111111111111").Equals((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000"));

            // GUIDs match always and ignore prefix
            Assert.IsTrue(((TeamsParticipant)"8:00000000-0000-beef-0000-000000000000").Equals((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000"));
            Assert.IsTrue(((TeamsParticipant)"00000000-0000-beef-0000-000000000000").Equals((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000"));
            Assert.IsTrue(((TeamsParticipant)"00000000-0000-beef-0000-000000000000").Equals((TeamsParticipant)"28:orgid:00000000-0000-beef-0000-000000000000"));

            // other id formats need prefix
            Assert.IsFalse(((TeamsParticipant)"28:integration:name").Equals((TeamsParticipant)"name"));
            Assert.IsTrue(((TeamsParticipant)"28:integration:name").Equals((TeamsParticipant)"28:integration:name"));
        }
    }
}
