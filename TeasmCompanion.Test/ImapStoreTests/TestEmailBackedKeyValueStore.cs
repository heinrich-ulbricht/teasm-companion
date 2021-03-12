using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;
using TeasmCompanion.Stores.Imap;

#nullable enable

namespace TeasmCompanion.Test.ImapStoreTests
{
    [TestClass]
    public class TestEmailBackedKeyValueStore : TestBase
    {
        private class TestData
        {
            public string Data { get; set; } = "";
        }


        [TestMethod]
        public void TestWriteAndReadValue()
        {
            var store = new EmailBackedKeyValueStore(null, "store key", "message subject");
            var testObject = new TestData() { Data = "the data" };
            store.Set("my key", testObject);

            var readValue = store.GetOrCreateEmpty<TestData>("my key");
            Assert.AreEqual("the data", readValue.AsObject?.Data);
        }

        [TestMethod]
        public void TestWriteValueMultipleTimes()
        {
            var store = new EmailBackedKeyValueStore(null, "store key", "message subject");
            var testObject = new TestData() { Data = "the data" };
            store.Set("my key", testObject);

            var testObject2 = new TestData() { Data = "replaced data" };
            store.Set("my key", testObject2);

            var readValue = store.GetOrCreateEmpty<TestData>("my key");
            Assert.AreEqual("replaced data", readValue.AsObject?.Data);
        }

        [TestMethod]
        public void TestForceNullDefaultValue()
        {
            var store = new EmailBackedKeyValueStore(null, "store key", "message subject");
            var testObject = new TestData() { Data = "the data" };
            store.Set("my key", testObject);

            var readValue = store.GetOrCreateEmpty<TestData?>("unknown key", () => default);
            Assert.IsNull(readValue.AsObject);
            Assert.AreEqual("null", readValue.AsString);
        }

        [TestMethod]
        public void TestComplexValue()
        {
            var store = new EmailBackedKeyValueStore(null, "store key", "message subject");
            var testObject = new ProcessedTeamsUser(new TeamsDataContext((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000", new ProcessedTenant(new TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users.Tenant() { tenantName = "Test Tenant" }, DateTime.Now)), (TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000") { State = ProcessedTeamsUser.TeamsUserState.MissingFromTenant };
            store.Set("my key", testObject);

            var readValue = store.GetOrCreateEmpty<ProcessedTeamsUser>("my key");

            Assert.AreEqual(ProcessedTeamsUser.TeamsUserState.MissingFromTenant, readValue.AsObject?.State);
            Assert.AreEqual("8:orgid:00000000-0000-beef-0000-000000000000", readValue.AsObject?.DataContext.MainUserId.Mri);
            Assert.AreEqual("00000000-0000-beef-0000-000000000000", readValue.AsObject?.DataContext.MainUserId.Id);
            Assert.AreEqual(ParticipantKind.User, readValue.AsObject?.DataContext.MainUserId.Kind);
            Assert.AreEqual("Test Tenant", readValue.AsObject?.DataContext.Tenant.TenantName);
        }

        [TestMethod]
        public void TestDefaultValues()
        {
            var store = new EmailBackedKeyValueStore(null, "store key", "message subject");
            var objectValue = store.GetOrCreateEmpty<TestData>("unknown key");
            Assert.IsNotNull(objectValue.AsObject);
            Assert.AreEqual(@"{""Data"":""""}", objectValue.AsString);

            var intValue = store.GetOrCreateEmpty<int>("unknown key");
            Assert.AreEqual(@"0", intValue.AsString);

            var stringValue = store.GetOrCreateEmpty<string>("unknown key");
            Assert.AreEqual(@"""""", stringValue.AsString);

            var listValue = store.GetOrCreateEmpty<List<int>>("unknown key");
            Assert.IsNotNull(listValue.AsObject);
            Assert.AreEqual(@"[]", listValue.AsString);
        }

        [TestMethod]
        public void TestTextAndHtmlContent()
        {
            var store = new EmailBackedKeyValueStore(null, "store key", "message subject");
            var testObject = new ProcessedTeamsUser(new TeamsDataContext((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000", new ProcessedTenant(new TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users.Tenant() { tenantName = "Test Tenant" }, DateTime.Now)), (TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000") { State = ProcessedTeamsUser.TeamsUserState.MissingFromTenant };
            store.Set("my key", testObject);

            store.SetTextContent("i am a text");
            store.SetHtmlContent("i am HTML");

            Assert.AreEqual("i am a text", store.GetTextContent());
            Assert.AreEqual("i am HTML", store.GetHtmlContent());

            var attachments = new MimeKit.AttachmentCollection();
            using (var stream = new MemoryStream())
            {
                stream.Write(Encoding.UTF8.GetBytes("i am an attachment"));
                stream.Position = 0;
                attachments.Add("test.txt", stream);
            }
            store.AddAttachments(attachments);
            Assert.AreEqual(1, store.MessageAndId.Message?.Attachments.Count());

            using (var debugStream = new MemoryStream())
            {
                store.MessageAndId.Message?.WriteTo(debugStream);
                var s = Encoding.ASCII.GetString(debugStream.ToArray());
            }

            var readValue = store.GetOrCreateEmpty<ProcessedTeamsUser>("my key");
            Assert.AreEqual("8:orgid:00000000-0000-beef-0000-000000000000", readValue.AsObject?.DataContext.MainUserId.Mri);
        }
    }
}
