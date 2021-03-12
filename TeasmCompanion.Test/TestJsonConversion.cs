using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Numerics;
using System.Xml;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;
using TeasmCompanion.Stores;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.v2.users.me.endpoints;

namespace TeasmCompanion.Test
{
    [TestClass]
    public class TestJsonConversion : TestBase
    {
        [TestMethod]
        public void TestConversionOfDynamicPropsWhilePreservingDefaultBehaviour()
        {
            var json =
@"
{
    ""next"": ""https://northeurope-prod-4.notifications.teams.microsoft.com/users/8:orgid:00000000-0000-beef-0000-000000000000/endpoints/00000000-0000-beef-0000-000000000000/events/poll?cursor=111&lsid=id&lsv=id==&epfs=srt&sca=0"",
    ""eventMessages"": [
        {
            ""time"": ""2021-01-23T23:29:43.7594551Z"",
            ""type"": ""EventMessage"",
            ""resourceType"": ""ThreadUpdate"",
            ""resource"": {
                ""id"": ""19:11111111111111111111111111111111@thread.skype"",
                ""messages"": ""https://notifications.skype.net/v1/users/ME/conversations/19:11111111111111111111111111111111@thread.skype/messages"",
                ""version"": 1111111111111,
                ""type"": ""Thread"",
                ""properties"": {
                    ""links"":""[{\""@type\"":\""http://schema.skype.com/HyperLink\"",\""itemid\"":\""0\"",\""url\"":\""https://wiki.contoso.de/display/contoso/topic\"",\""previewenabled\"":true,\""preview\"":{\""previewurl\"":\""\"",\""isLinkUnsafe\"":false}}]"",
                    ""containsExternalEntitiesListeningAll"": ""False"",
                    ""privacy"": ""public"",
                    ""creator"": ""8:orgid:00000000-0000-beef-0000-000000000000"",
                    ""tenantid"": ""00000000-0000-beef-0000-000000000000"",
                    ""topic"": ""Campaign"",
                    ""topicThreadVersion"": ""v5"",
                    ""tab::00000000-0000-beef-0000-000000000000"": ""{\""name\"":\""Wiki\"",\""id\"":\""tab::00000000-0000-beef-0000-000000000000\"",\""definitionId\"":\""com.microsoft.teamspace.tab.wiki\"",\""type\"":\""tab:\"",\""settings\"":{\""subtype\"":\""wiki-tab\"",\""wikiTabId\"":1,\""wikiDefaultTab\"":true,\""hasContent\"":false},\""directive\"":\""extension-tab\"",\""order\"":\""10000\"",\""resourceId\"":\""00000000-0000-beef-0000-000000000000\""}"",
                    ""topicThreadTopic"": ""Campaign"",
                    ""createRelatedMessagesIndex"": ""true"",
                    ""threadType"": ""topic"",
                    ""sharepointChannelDocsFolder"": ""Kampagnen"",
                    ""tab::00000000-0000-beef-0000-000000000001"": ""{\""name\"":\""Campaign\"",\""id\"":\""tab::00000000-0000-beef-0000-000000000001\"",\""definitionId\"":\""com.microsoft.teamspace.tab.web\"",\""type\"":\""tab:\"",\""settings\"":{\""url\"":\""https://wiki.contoso.de/display/TT/topic\"",\""websiteUrl\"":\""https://wiki.contoso.de/display/TT/topic\"",\""subtype\"":\""webpage\"",\""dateAdded\"":\""2020-01-01T01:01:01.111Z\""},\""directive\"":\""extension-tab\"",\""order\"":\""10000100100\"",\""resourceId\"":\""00000000-0000-beef-0000-000000000000\"",\""replyChainId\"":\""1111111111111\""}"",
                    ""spaceId"": ""19:11111111111111111111111111111111@thread.skype"",
                    ""tab::00000000-0000-beef-0000-000000000002"": ""{\""name\"":\""Campaign\"",\""id\"":\""tab::00000000-0000-beef-0000-000000000002\"",\""definitionId\"":\""com.microsoft.teamspace.tab.planner\"",\""type\"":\""tab:\"",\""settings\"":{\""name\"":\""Campaign\"",\""url\"":\""https://tasks.teams.microsoft.com/teamsui/{tid}/Home/PlannerFrame?page=7&auth_pvr=OrgId&auth_upn={userPrincipalName}&groupId={groupId}&planId=id-xD&channelId={channelId}&entityId={entityId}&tid={tid}&userObjectId={userObjectId}&subEntityId={subEntityId}&sessionId={sessionId}&theme={theme}&mkt={locale}&ringId={ringId}&PlannerRouteHint={tid}&tabVersion=20200228.1_s\"",\""websiteUrl\"":\""https://tasks.office.com/00000000-0000-beef-0000-000000000000/Home/PlanViews/id-xD?Type=PlanLink&Channel=TeamsTab\"",\""removeUrl\"":\""https://tasks.teams.microsoft.com/teamsui/{tid}/Home/PlannerFrame?page=13&auth_pvr=OrgId&auth_upn={userPrincipalName}&groupId={groupId}&planId=id-xD&channelId={channelId}&entityId={entityId}&tid={tid}&userObjectId={userObjectId}&subEntityId={subEntityId}&sessionId={sessionId}&theme={theme}&mkt={locale}&ringId={ringId}&PlannerRouteHint={tid}&tabVersion=20200228.1_s\"",\""entityId\"":\""tt.c_19:id\"",\""subtype\"":\""extension\"",\""dateAdded\"":\""2019-11-11T11:11:11.111Z\""},\""directive\"":\""extension-tab\"",\""order\"":\""10000100\"",\""resourceId\"":\""00000000-0000-beef-0000-000000000000\"",\""replyChainId\"":\""1111111111111\""}"",
                    ""description"": ""Channel about campaigns"",
                    ""channelDocsFolderRelativeUrl"": ""/sites/Sales/Shared Documents/topic"",
                    ""isMigratedThread"": ""true"",
                    ""createdat"": ""1111111111111"",
                    ""historydisclosed"": ""true"",
                    ""isMigrated"": ""true"",
                    ""RootResourceGroupId"": ""92:11111111111111111111111111111111@thread.skype"",
                    ""channelDocsDocumentLibraryId"": ""default"",
                    ""switchWriteEnabled"": ""true"",
                    ""groupId"": ""00000000-0000-beef-0000-000000000000"",
                    ""gapDetectionEnabled"": ""True"",
                    ""integration:integrationid"": ""{\""integrationId\"":\""integrationid\"",\""integrationType\"":\""Incoming\"",\""displayName\"":\""Email Connector\"",\""avatarUrl\"":\""https://statics.teams.microsoft.com/evergreen-assets/mailhookservice/mailicon.png?v=2\"",\""providerGuid\"":null,\""dataSchema\"":\""skype\"",\""templateName\"":null,\""creatorSkypeMri\"":\""8:orgid:00000000-0000-beef-0000-000000000000\""}""
                },
                ""rosterSummary"": {
                    ""memberCount"": 199,
                    ""botCount"": 2,
                    ""readerCount"": 0,
                    ""roleCounts"": {
                        ""User"": 198,
                        ""Admin"": 1
                    },
                    ""externalMemberCount"": 0
                },
                ""rosterVersion"": 1111111111111
            }
        }
    ]
}
";
            var settings = new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Error };
            var result = JsonConvert.DeserializeObject<GET_Endpoint_ResponseBody>(json, settings);

            var tabInfos = result.eventMessages[0]?.resource?.properties?.tabInfos;
            var integrationInfos = result.eventMessages[0]?.resource?.properties?.integrationInfos;
            Assert.AreEqual(3, tabInfos?.Count);
            Assert.IsNotNull(tabInfos?[0].id);
            Assert.AreEqual(1, integrationInfos?.Count);
            Assert.AreEqual("integrationid", integrationInfos?[0].integrationId);
            Assert.AreEqual(1, result.eventMessages[0].resource?.properties?.links.Count);

        }

        [TestMethod]
        public void TestConversionWithNullFiles()
        {
            var json =
@"
{
""next"": ""https://northeurope-prod-4.notifications.teams.microsoft.com/users/8:orgid:00000000-0000-beef-0000-000000000000/endpoints/00000000-0000-beef-0000-000000000000/events/poll?cursor=1111111111&lsid=id&lsv=id==&epfs=srt&sca=0"",
""eventMessages"": [
    {
        ""type"": ""EventMessage"",
        ""resourceType"": ""ThreadUpdate"",
        ""resource"": {
            ""id"": ""19:11111111111111111111111111111111@thread.skype"",
            ""version"": 1111111111111,
            ""type"": ""Thread"",
            ""properties"": {
                ""files"": ""null"",
            },
            ""rosterVersion"": 1111111111111
        }
    }
]
}
";
            var settings = new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Error };
            var result = JsonConvert.DeserializeObject<GET_Endpoint_ResponseBody>(json, settings);

            var files = result.eventMessages[0]?.resource?.properties?.files;
            Assert.AreEqual(0, files.Count);
        }

        [TestMethod]
        public void SerializeTeamsChatIndexEntry()
        {
            var o = new TeamsChatIndexEntry()
            {
                ChatId = "chat id",
                FolderName = "folder name"
            };
            var json = JsonConvert.SerializeObject(o);

            // detect any structural change, like missing or too much properties (happend...)
            Assert.AreEqual(73, json.Length);
        }

        [TestMethod]
        public void DeserializeXmlContent()
        {
            var xml = @"
                <deletemember>
                    <eventtime>0</eventtime>
                    <initiator>8:orgid:00000000-0000-beef-0000-000000000001</initiator>
                    <detailedinitiatorinfo>
                        <friendlyName>Some User</friendlyName>
                    </detailedinitiatorinfo>
                    <target>8:orgid:00000000-0000-beef-0000-000000000000</target>
                    <detailedtargetinfo>
                        <id>8:orgid:00000000-0000-beef-0000-000000000000</id>
                        <friendlyName>Some Other User</friendlyName>
                    </detailedtargetinfo>
                    <target>8:orgid:00000000-0000-beef-0000-000000000001</target>
                    <detailedtargetinfo>
                        <id>8:orgid:00000000-0000-beef-0000-000000000001</id>
                        <friendlyName>Some User</friendlyName>
                    </detailedtargetinfo>
                </deletemember>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml($"<root xmlns:json='http://james.newtonking.com/projects/json'>{xml}</root>");
            var json = JsonConvert.SerializeXmlNode(doc);
            //var data = JsonUtils.DeserializeObject<ThreadActivityAddMemberWrapper>(logger, json);

        }

        [TestMethod]
        public void TestProcessedTeamsUserSerialization()
        {
            var user = new ProcessedTeamsUser(new TeamsDataContext((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000", new ProcessedTenant(new TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users.Tenant() { tenantName = "Test Tenant" }, DateTime.Now)), (TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000") { State = ProcessedTeamsUser.TeamsUserState.MissingFromTenant };
            var dt = DateTime.UtcNow;
            user.RegisterAlternateDisplayName("alternate display name", dt);
            var json = JsonConvert.SerializeObject(user);
            var deserializedUser = JsonConvert.DeserializeObject<ProcessedTeamsUser>(json);

            Assert.AreEqual("alternate display name", deserializedUser.DisplayName);
        }

        [TestMethod]
        public void TestBigInteger()
        {
            var json = @"{
    ""activityId"": ""19999999999999999999""
}";
            var activity = JsonConvert.DeserializeObject<Activity>(json);
            Assert.AreEqual(BigInteger.Parse("19999999999999999999"), activity.activityId);
        }
    }
}
