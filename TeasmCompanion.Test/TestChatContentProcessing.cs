using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.csa.api.v1.teams.users;

namespace TeasmCompanion.Test
{
    [TestClass]
    public class TestChatContentProcessing : TestBase
    {
        [TestMethod]
        public void TestChatComparerSimple()
        {
            var chatJson =
                    @"
                        {
                            ""id"": ""19:00000000-0000-beef-0000-000000000001_00000000-0000-beef-0000-000000000000@unq.gbl.spaces"",
                            ""members"": [
                                {
                                    ""mri"": ""8:orgid:00000000-0000-beef-0000-000000000000"",
                                    ""role"": ""Admin""
                                }
                            ],
                            ""title"": null,
                            ""LastMessage"": {
                                ""type"": ""Message"",
                                ""messagetype"": ""RichText/Html"",
                                ""content"": ""<div>How about this thing?</div>"",
                                ""from"": ""8:orgid:00000000-0000-beef-0000-000000000001"",
                                ""imdisplayname"": ""Other Person"",
                                ""containerId"": ""19:00000000-0000-beef-0000-000000000001_00000000-0000-beef-0000-000000000000@unq.gbl.spaces""
                            },
                            ""creator"": ""8:orgid:00000000-0000-beef-0000-000000000000"",
                            ""Id"": ""19:00000000-0000-beef-0000-000000000001_00000000-0000-beef-0000-000000000000@unq.gbl.spaces""
                        }
                    ";
            var chat = JsonConvert.DeserializeObject<Chat>(chatJson);


            //using var kernel = new FakeItEasyMockingKernel();
            //var fac = kernel.Get<TeamsTenant>();
            //var remoteLock = kernel.Get<ImapBackedRemoteLock>();

            //TBD: implement test
        }
    }
}
