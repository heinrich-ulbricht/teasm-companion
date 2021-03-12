using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ninject;
using Ninject.MockingKernel.FakeItEasy;
using Serilog;
using System;
using System.Threading.Tasks;
using TeasmCompanion.Interfaces;
using TeasmCompanion.ProcessedTeamsObjects;
using TeasmCompanion.Registries;
using TeasmCompanion.TeamsInternal.TeamsInternalApi.api.mt.emea.beta.users;

namespace TeasmCompanion.Test.UserRegistryTests
{
    [TestClass]
    public class TestUserRegistry
    {
        [TestMethod]
        public async Task TestMriReplacement()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<ITeamsUserStore>().ToConstant(A.Fake<ITeamsUserStore>()).InSingletonScope();
            kernel.Rebind<ITeamsUserRegistry>().To<TeamsUserRegistry>().InSingletonScope();
            var userRegistry = kernel.Get<ITeamsUserRegistry>();

            var fakeContext = new TeamsDataContext((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000", new ProcessedTenant(new Tenant() { tenantId = "Tenant ID", userId = "8:orgid:00000000-0000-beef-0000-000000000000", tenantName = "Fake User" }, DateTime.UtcNow));

            var userId = (TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000";
            await userRegistry.RecognizeUserIdAsync(fakeContext, userId);
            await userRegistry.RegisterDisplayNameForUserIdAsync(fakeContext, userId, "User Name", DateTime.UtcNow);

            var user = await userRegistry.GetUserByIdAsync(fakeContext, userId, false);
            Assert.AreEqual("User Name", user.DisplayName);

            string s;
            s = await userRegistry.ReplaceUserIdsWithDisplayNamesAsync(fakeContext, "8:orgid:00000000-0000-beef-0000-000000000000");
            Assert.AreEqual("User Name", s);
            s = await userRegistry.ReplaceUserIdsWithDisplayNamesAsync(fakeContext, "00000000-0000-beef-0000-000000000000");
            Assert.AreEqual("User Name", s);
            s = await userRegistry.ReplaceUserIdsWithDisplayNamesAsync(fakeContext, "00000000-0000-beef-0000-000000000000, 00000000-0000-beef-0000-000000000000");
            Assert.AreEqual("User Name, User Name", s);
            s = await userRegistry.ReplaceUserIdsWithDisplayNamesAsync(fakeContext, "8:00000000-0000-beef-0000-000000000000");
            Assert.AreEqual("User Name", s);
        }

        [TestMethod]
        public async Task TestBotAndChatReplacement()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<ITeamsUserStore>().ToConstant(A.Fake<ITeamsUserStore>()).InSingletonScope();
            kernel.Rebind<ITeamsUserRegistry>().To<TeamsUserRegistry>().InSingletonScope();
            var userRegistry = kernel.Get<ITeamsUserRegistry>();

            var fakeContext = new TeamsDataContext((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000", new ProcessedTenant(new Tenant() { tenantId = "Tenant ID", userId = "8:orgid:00000000-0000-beef-0000-000000000000", tenantName = "Fake User" }, DateTime.UtcNow));

            await userRegistry.RecognizeUserIdAsync(fakeContext, (TeamsParticipant)"817c2506-de4a-4795-971e-371ea75a03ed"); // polly
            await userRegistry.RecognizeUserIdAsync(fakeContext, (TeamsParticipant)"19:00000000-0000-beef-0000-000000000000_00000000-0000-beef-0000-000000000000@unq.gbl.spaces"); // a chat

            string s;
            s = await userRegistry.ReplaceUserIdsWithDisplayNamesAsync(fakeContext, "817c2506-de4a-4795-971e-371ea75a03ed, 19:00000000-0000-beef-0000-000000000000_00000000-0000-beef-0000-000000000000@unq.gbl.spaces");
            Assert.AreEqual("Polly, Microsoft Teams Chat", s);
        }
    }
}
