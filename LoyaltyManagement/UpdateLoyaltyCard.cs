using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace LoyaltyManagement
{
    public class UpdateLoyaltyCard : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                tracing.Trace("UpdateLoyaltyCard Started");

                if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity)) return;

                Entity target = (Entity)context.InputParameters["Target"];
                if (target.LogicalName != "new_loyaltycard") return;

                var cardId = target.Id;

                // Retrieve the full card
                tracing.Trace("Retrieve the full card");
                var card = service.Retrieve("new_loyaltycard", cardId, new ColumnSet("new_totalpoints", "new_cardtype"));
                var totalPoints = card.GetAttributeValue<decimal>("new_totalpoints");
                var currentCardTypeRef = card.GetAttributeValue<EntityReference>("new_cardtype");
                if (currentCardTypeRef == null) return;

                tracing.Trace($"Current Total Points: {totalPoints}");

                // Step 1: Get all card types ordered by card level
                tracing.Trace("Get all card types ordered by card level");
                var query = new QueryExpression("new_loyaltycardtype")
                {
                    ColumnSet = new ColumnSet("new_minimumpoints", "new_cardlevel", "new_name"),
                    Criteria = new FilterExpression
                    {
                        Conditions = {
                                        new ConditionExpression("new_minimumpoints", ConditionOperator.LessEqual, Convert.ToInt32(totalPoints))
                                    }
                    },
                    Orders = { new OrderExpression("new_cardlevel", OrderType.Ascending) }
                };

                var cardTypes = service.RetrieveMultiple(query).Entities;

                // Step 2: Find highest eligible card type
                tracing.Trace("Find highest eligible card type");
                var sortedTypes = cardTypes.OrderBy(ct => ct.GetAttributeValue<int>("new_cardlevel")).ToList();
                Entity eligibleCardType = null;

                foreach (var type in sortedTypes)
                {
                    var threshold = (decimal)type.GetAttributeValue<int>("new_minimumpoints");
                    tracing.Trace("threshold = " + threshold);
                    tracing.Trace("totalPoints = " + totalPoints);
                    if (totalPoints >= threshold)
                    {
                        eligibleCardType = type;
                    }
                    else break;
                }

                // Step 3: Compare card levels to determine upgrade or downgrade
                tracing.Trace("Compare card levels to determine upgrade/downgrade");
                var currentCard = service.Retrieve("new_loyaltycardtype", currentCardTypeRef.Id, new ColumnSet("new_cardlevel"));
                int currentLevel = currentCard.GetAttributeValue<int>("new_cardlevel");
                int newLevel = eligibleCardType?.GetAttributeValue<int>("new_cardlevel") ?? currentLevel;

                if (newLevel > currentLevel)
                {
                    // Upgrade logic
                    tracing.Trace($"Upgrading card from level {currentLevel} to {newLevel}");

                    // Update card type
                    var updateCard = new Entity("new_loyaltycard", cardId);
                    updateCard["new_cardtype"] = new EntityReference("new_loyaltycardtype", eligibleCardType.Id);
                    service.Update(updateCard);

                    // Add Note in Timeline for Upgrade
                    var note = new Entity("annotation");
                    note["subject"] = "Loyalty Card Upgraded";
                    note["notetext"] = $"Card upgraded to: {eligibleCardType.GetAttributeValue<string>("new_name")} on {DateTime.Now.ToShortDateString()}";
                    note["objectid"] = new EntityReference("new_loyaltycard", cardId);
                    note["objecttypecode"] = "new_loyaltycard";
                    service.Create(note);

                    tracing.Trace("Card upgraded and audit note created.");
                }
                else if (newLevel < currentLevel)
                {
                    // Downgrade logic
                    tracing.Trace($"Downgrading card from level {currentLevel} to {newLevel}");

                    // Update card type
                    var downgradeCard = new Entity("new_loyaltycard", cardId);
                    downgradeCard["new_cardtype"] = new EntityReference("new_loyaltycardtype", eligibleCardType.Id);
                    service.Update(downgradeCard);

                    // Add Note in Timeline for Downgrade
                    var note = new Entity("annotation");
                    note["subject"] = "Loyalty Card Downgraded";
                    note["notetext"] = $"Card downgraded to: {eligibleCardType.GetAttributeValue<string>("new_name")} on {DateTime.Now.ToShortDateString()}";
                    note["objectid"] = new EntityReference("new_loyaltycard", cardId);
                    note["objecttypecode"] = "new_loyaltycard";
                    service.Create(note);

                    tracing.Trace("Card downgraded and audit note created.");
                }
                else
                {
                    tracing.Trace("Card level is unchanged.");
                }
            }
            catch (Exception ex)
            {
                tracing.Trace("Error in LoyaltyCardUpgradePlugin: " + ex.Message);
                throw;
            }
        }
    }
}



//using Microsoft.Xrm.Sdk.Query;
//using Microsoft.Xrm.Sdk;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace LoyaltyManagement
//{
//    public class UpdateLoyaltyCard : IPlugin
//    {
//        public void Execute(IServiceProvider serviceProvider)
//        {
//            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
//            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
//            var service = serviceFactory.CreateOrganizationService(context.UserId);
//            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

//            try
//            {
//                tracing.Trace("UpdateLoyaltyCard Started");

//                if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity)) return;

//                Entity target = (Entity)context.InputParameters["Target"];
//                if (target.LogicalName != "new_loyaltycard") return;

//                var cardId = target.Id;

//                // Retrieve the full card
//                tracing.Trace("Retrieve the full card");
//                var card = service.Retrieve("new_loyaltycard", cardId, new ColumnSet("new_totalpoints", "new_cardtype"));
//                var totalPoints = card.GetAttributeValue<decimal>("new_totalpoints");
//                var currentCardTypeRef = card.GetAttributeValue<EntityReference>("new_cardtype");
//                if (currentCardTypeRef == null) return;

//                tracing.Trace($"Current Total Points: {totalPoints}");

//                // Step 1: Get all card types ordered by card level
//                tracing.Trace("Get all card types ordered by card level");
//                var query = new QueryExpression("new_loyaltycardtype")
//                {
//                    ColumnSet = new ColumnSet("new_minimumpoints", "new_cardlevel", "new_name"),
//                    Criteria = new FilterExpression
//                    {
//                        Conditions = {
//                                        new ConditionExpression("new_minimumpoints", ConditionOperator.LessEqual, Convert.ToInt32(totalPoints))
//                                    }
//                    },
//                    Orders = { new OrderExpression("new_cardlevel", OrderType.Ascending) }
//                };

//                var cardTypes = service.RetrieveMultiple(query).Entities;

//                // Step 2: Find highest eligible card type
//                tracing.Trace("Find highest eligible card type");
//                var sortedTypes = cardTypes.OrderBy(ct => ct.GetAttributeValue<int>("new_cardlevel")).ToList();
//                Entity eligibleCardType = null;

//                foreach (var type in sortedTypes)
//                {
//                    var threshold = (decimal)type.GetAttributeValue<int>("new_minimumpoints");
//                    tracing.Trace("threshold = " + threshold);
//                    tracing.Trace("totalPoints = " + totalPoints);
//                    if (totalPoints >= threshold)
//                    {
//                        eligibleCardType = type;
//                    }
//                    else break;
//                }

//                if (eligibleCardType == null) return;

//                // Step 3: Compare card levels to determine upgrade
//                tracing.Trace("Compare card levels to determine upgrade");
//                var currentCard = service.Retrieve("new_loyaltycardtype", currentCardTypeRef.Id, new ColumnSet("new_cardlevel"));
//                int currentLevel = currentCard.GetAttributeValue<int>("new_cardlevel");
//                int newLevel = eligibleCardType.GetAttributeValue<int>("new_cardlevel");

//                if (newLevel > currentLevel)
//                {
//                    tracing.Trace($"Upgrading card from level {currentLevel} to {newLevel}");

//                    // Update card type
//                    var updateCard = new Entity("new_loyaltycard", cardId);
//                    updateCard["new_cardtype"] = new EntityReference("new_loyaltycardtype", eligibleCardType.Id);
//                    service.Update(updateCard);

//                    // Add Note in Timeline
//                    var note = new Entity("annotation");
//                    note["subject"] = "Loyalty Card Upgraded";
//                    note["notetext"] = $"Card upgraded to: {eligibleCardType.GetAttributeValue<string>("new_name")} on {DateTime.Now.ToShortDateString()}";
//                    note["objectid"] = new EntityReference("new_loyaltycard", cardId);
//                    note["objecttypecode"] = "new_loyaltycard";
//                    service.Create(note);

//                    tracing.Trace("Card upgraded and audit note created.");
//                }
//                else
//                {
//                    tracing.Trace("Card not upgraded.");
//                }
//            }
//            catch (Exception ex)
//            {
//                tracing.Trace("Error in LoyaltyCardUpgradePlugin: " + ex.Message);
//                throw;
//            }
//        }
//    }

//}
