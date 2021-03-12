using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;

namespace TeasmCompanion.Test
{
    [TestClass]
    public class TestProcessdTeamsUser
    {
        private TeamsDataContext fakeContext;

        [TestInitialize]
        public void Initialize()
        {
            fakeContext = new TeamsDataContext((TeamsParticipant)"00000000-0000-beef-0000-000000000000", new ProcessedTenant(new Tenant() { tenantId = "00000000-0000-feeb-0000-000000000000", userId = "00000000-0000-beef-0000-000000000000", tenantName = "Fake Tenant" }, DateTime.UtcNow));
        }

        [TestMethod]
        public void TestIgnoreAlternativeName()
        {
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid", new TeamsUser() { displayName = "Name 1" }, ProcessedTeamsUser.TeamsUserState.MissingFromTenant);
            Assert.IsTrue(user.HasDisplayName);
            Assert.AreEqual("Name 1", user.DisplayName);
            user.RegisterAlternateDisplayName("Name 2", new DateTime(1000));
            Assert.IsTrue(user.HasDisplayName);
            Assert.AreEqual("Name 1", user.DisplayName);
        }

        [TestMethod]
        public void TestUseAlternativeName()
        {
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid", new TeamsUser(), ProcessedTeamsUser.TeamsUserState.MissingFromTenant);
            Assert.IsFalse(user.HasDisplayName);
            user.RegisterAlternateDisplayName("Name 1", new DateTime(1000));
            Assert.IsTrue(user.HasDisplayName);
            Assert.AreEqual("Name 1", user.DisplayName);
        }

        [TestMethod]
        public void TestAlternativeNameOrder()
        {
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid", new TeamsUser(), ProcessedTeamsUser.TeamsUserState.MissingFromTenant);
            Assert.IsFalse(user.HasDisplayName);
            user.RegisterAlternateDisplayName("Name 200", new DateTime(200));
            user.RegisterAlternateDisplayName("Name 1000", new DateTime(1000));
            user.RegisterAlternateDisplayName("Name 500", new DateTime(500));
            Assert.IsTrue(user.HasDisplayName);
            Assert.AreEqual("Name 1000", user.DisplayName);
            user.RegisterAlternateDisplayName("Name 2000", new DateTime(2000));
            Assert.IsTrue(user.HasDisplayName);
            Assert.AreEqual("Name 2000", user.DisplayName);
        }

        [TestMethod]
        public void TestDuplicateGivesNoError()
        {
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid", new TeamsUser(), ProcessedTeamsUser.TeamsUserState.MissingFromTenant);
            Assert.IsFalse(user.HasDisplayName);
            user.RegisterAlternateDisplayName("Name 200", new DateTime(200));
            user.RegisterAlternateDisplayName("Name 200", new DateTime(200));
            Assert.IsTrue(user.HasDisplayName);
            Assert.AreEqual("Name 200", user.DisplayName);
        }

        [TestMethod]
        public void TestDuplicateNameNotRecordedTwice()
        {
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid", new TeamsUser(), ProcessedTeamsUser.TeamsUserState.MissingFromTenant);
            Assert.IsFalse(user.HasDisplayName);
            user.RegisterAlternateDisplayName("Name Picard", new DateTime(200));
            user.RegisterAlternateDisplayName("Name Picard", new DateTime(1000));
            var json = JsonConvert.SerializeObject(user);

            Assert.IsTrue(json.IndexOf("Name Picard") == json.LastIndexOf("Name Picard"), "Found name more than once; that must not happen");

            user.RegisterAlternateDisplayName("Name Picard", new DateTime(100));
            json = JsonConvert.SerializeObject(user);

            Assert.IsTrue(json.IndexOf("Name Picard") == json.LastIndexOf("Name Picard"), "Found name more than once; that must not happen");

        }

        [TestMethod]
        public void DuplicateDoesNotDestroyOrder()
        {
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid", new TeamsUser(), ProcessedTeamsUser.TeamsUserState.MissingFromTenant);
            Assert.IsFalse(user.HasDisplayName);
            user.RegisterAlternateDisplayName("Name", new DateTime(1000));
            user.RegisterAlternateDisplayName("Name", new DateTime(200));
            user.RegisterAlternateDisplayName("Name 500", new DateTime(500));
            Assert.IsTrue(user.HasDisplayName);
            Assert.AreEqual("Name", user.DisplayName);
        }

        [TestMethod]
        public void TestIgnoreMalformedMri()
        {
            var user = new ProcessedTeamsUser(fakeContext, (TeamsParticipant)"test.fakeuserid", new TeamsUser(), ProcessedTeamsUser.TeamsUserState.MissingFromTenant);
            Assert.IsFalse(user.HasDisplayName);
            user.RegisterAlternateDisplayName("This is expected", new DateTime(100));
            user.RegisterAlternateDisplayName("orgid:00000000-0000-beef-0000-000000000000", new DateTime(500));
            Assert.IsTrue(user.HasDisplayName);
            Assert.AreEqual("This is expected", user.DisplayName);
        }        
    }
}
