using FakeXrmEasy;
using LoyaltyManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LoyaltyManagementUnitTests
{
    [TestClass]
    public class UpdateLoyaltyCardTests
    {
        [TestMethod]
        public void Execute_ShouldUpgradeLoyaltyCard_WhenEligibleForUpgrade()
        {
            // Arrange
            var context = new XrmFakedContext();
            var service = context.GetOrganizationService();

            var cardId = Guid.NewGuid();
            var currentCardTypeId = Guid.NewGuid();
            var newCardTypeId = Guid.NewGuid();

            // Mock current loyalty card
            var currentCard = new Entity("new_loyaltycard")
            {
                Id = cardId,
                ["new_totalpoints"] = 150m,
                ["new_cardtype"] = new EntityReference("new_loyaltycardtype", currentCardTypeId)
            };

            // Mock current card type
            var currentCardType = new Entity("new_loyaltycardtype")
            {
                Id = currentCardTypeId,
                ["new_cardlevel"] = 1
            };

            // Mock eligible card type
            var newCardType = new Entity("new_loyaltycardtype")
            {
                Id = newCardTypeId,
                ["new_minimumpoints"] = 100,
                ["new_cardlevel"] = 2,
                ["new_name"] = "Gold"
            };

            context.Initialize(new[] { currentCard, currentCardType, newCardType });

            // Create a fake plugin execution context
            var pluginContext = context.GetDefaultPluginContext();
            pluginContext.InputParameters["Target"] = new Entity("new_loyaltycard") { Id = cardId };

            // Manually create a mock IServiceProvider
            var serviceProvider = new XrmFakedServiceProvider(context, pluginContext);

            var plugin = new UpdateLoyaltyCard();

            try
            {
                // Act
                plugin.Execute(serviceProvider);
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    Console.WriteLine(loaderException.Message);
                }
                throw;
            }

            // Assert
            var updatedCard = context.CreateQuery("new_loyaltycard").FirstOrDefault();
            Assert.IsNotNull(updatedCard);
            Assert.AreEqual(newCardTypeId, ((EntityReference)updatedCard["new_cardtype"]).Id);
        }
    }

    public class XrmFakedServiceProvider : IServiceProvider
    {
        private readonly XrmFakedContext _context;
        private readonly XrmFakedPluginExecutionContext _pluginContext;

        public XrmFakedServiceProvider(XrmFakedContext context, XrmFakedPluginExecutionContext pluginContext)
        {
            _context = context;
            _pluginContext = pluginContext;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IPluginExecutionContext))
            {
                return _pluginContext;
            }
            if (serviceType == typeof(IOrganizationServiceFactory))
            {
                return new FakeOrganizationServiceFactory(_context);
            }
            if (serviceType == typeof(ITracingService))
            {
                return _context.GetFakeTracingService();
            }
            return null;
        }
    }

    public class FakeOrganizationServiceFactory : IOrganizationServiceFactory
    {
        private readonly XrmFakedContext _context;

        public FakeOrganizationServiceFactory(XrmFakedContext context)
        {
            _context = context;
        }

        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            return _context.GetOrganizationService();
        }
    }
}
