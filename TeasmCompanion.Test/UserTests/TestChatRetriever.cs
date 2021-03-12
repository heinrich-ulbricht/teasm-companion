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
using TeasmCompanion.TeamsMonitors;

namespace TeasmCompanion.Test.UserRegistryTests
{
    [TestClass]
    public class TestChatRetriever
    {
        [TestMethod]
        public async Task TestResolveParticipantPlaceholders()
        {
            using var kernel = new FakeItEasyMockingKernel();
            kernel.Rebind<ILogger>().ToConstant(Log.Logger);
            kernel.Rebind<ITeamsUserStore>().ToConstant(A.Fake<ITeamsUserStore>()).InSingletonScope();
            kernel.Rebind<ITeamsUserRegistry>().To<TeamsUserRegistry>().InSingletonScope();
            var userRegistry = kernel.Get<ITeamsUserRegistry>();
            var logger = kernel.Get<ILogger>();

            var fakeContext = new TeamsDataContext((TeamsParticipant)"8:orgid:00000000-0000-beef-0000-000000000000", new ProcessedTenant(new Tenant() { tenantId = "Tenant ID", userId = "8:orgid:00000000-0000-beef-0000-000000000000", tenantName = "Fake User" }, DateTime.UtcNow));
            await userRegistry.RegisterDisplayNameForUserIdAsync(fakeContext, (TeamsParticipant)"12300000-0000-beef-0000-000000000000", "Test Name", DateTime.UtcNow);
            var message = A.Fake<IMutableChatMessage>();
            message.MessageSubject = "User {{12300000-0000-beef-0000-000000000000}} added: User {{12300000-0000-beef-0000-000000000000}}, Heinrich Ulbricht";
            await TeamsChatRetriever.ResolveParticipantPlaceholders(logger, userRegistry, fakeContext, message);
            Assert.AreEqual("Test Name added: Test Name, Heinrich Ulbricht", message.MessageSubject);
        }
    }
}
