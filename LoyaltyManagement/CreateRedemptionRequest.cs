using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoyaltyManagement
{
    public class CreateRedemptionRequest : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity)) return;

            var redemption = (Entity)context.InputParameters["Target"];
            if (redemption.LogicalName != "new_redemptionrequest") return;

            try
            {
                tracing.Trace("Start Redemption Plugin");

                // 1. Get Reward and Contact
                var rewardRef = redemption.GetAttributeValue<EntityReference>("new_reward");
                var contactRef = redemption.GetAttributeValue<EntityReference>("new_contact");
                if (rewardRef == null || contactRef == null) return;

                tracing.Trace($"Reward ID: {rewardRef.Id}, Contact ID: {contactRef.Id}");

                // 2. Retrieve Loyalty Card for Contact
                var query = new QueryExpression("new_loyaltycard")
                {
                    ColumnSet = new ColumnSet("new_totalpoints"),
                    Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("new_customer", ConditionOperator.Equal, contactRef.Id)
                }
            }
                };

                var cards = service.RetrieveMultiple(query).Entities;
                if (!cards.Any()) throw new InvalidPluginExecutionException("No loyalty card found for the contact.");

                var card = cards.First();
                var currentPoints = card.GetAttributeValue<decimal>("new_totalpoints");

                // 3. Retrieve Reward info
                var reward = service.Retrieve("new_reward", rewardRef.Id, new ColumnSet("new_pointsrequired"));
                var pointsRequired = (decimal)reward.GetAttributeValue<int>("new_pointsrequired");

                tracing.Trace($"Current Points: {currentPoints}, Points Required: {pointsRequired}");

                // 4. Check if user has enough points
                if (currentPoints < pointsRequired)
                    throw new InvalidPluginExecutionException("Not enough points to redeem this reward.");

                // 5. Deduct points
                card["new_totalpoints"] = currentPoints - pointsRequired;
                service.Update(card);
                tracing.Trace("Points deducted successfully.");

                // 6. Auto-approve the request
                redemption["new_isapproved"] = true; // Auto-approved
                // 7. Store current points snapshot on redemption request
                redemption["new_usercurrentpoints"] = currentPoints;

                service.Update(redemption);
                tracing.Trace("Redemption request marked as auto-approved.");
            }
            catch (Exception ex)
            {
                tracing.Trace("Error in Redemption Plugin: " + ex.Message);
                throw;
            }
        }


    }
}
