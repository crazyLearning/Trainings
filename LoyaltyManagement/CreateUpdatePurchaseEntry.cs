using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace LoyaltyManagement
{
    public class CreateUpdatePurchaseEntry : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            try
            {
                tracing.Trace("Starting execution of CreateUpdatePurchaseEntry Plugin");
                if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity)) return;

                var purchase = (Entity)context.InputParameters["Target"];
                if (purchase.LogicalName != "new_purchaseentry") return;

                var purchaseId = purchase.Id;

                // Retrieve full Purchase Entry if needed
                tracing.Trace("Retrieve full Purchase Entry if needed");
                purchase = service.Retrieve("new_purchaseentry", purchaseId, new ColumnSet("new_product", "new_cardused", "new_purchaseprice"));

                var productRef = purchase.GetAttributeValue<EntityReference>("new_product");
                var cardRef = purchase.GetAttributeValue<EntityReference>("new_cardused");
                var purchasePrice = purchase.GetAttributeValue<Money>("new_purchaseprice")?.Value ?? 0;
                var currencyRef = purchase.GetAttributeValue<EntityReference>("transactioncurrencyid");
                tracing.Trace("cardTypeRef.Id = " + productRef.Id);
                tracing.Trace("currencyRef.Id = " + currencyRef.Id);

                if (productRef == null || cardRef == null || currencyRef == null || purchasePrice <= 0) return;

                // Step 1: Get Product Category
                tracing.Trace("Get Product Category");
                var product = service.Retrieve("product", productRef.Id, new ColumnSet("parentproductid"));
                var categoryRef = product.GetAttributeValue<EntityReference>("parentproductid");
                if (categoryRef == null) return;

                // Step 2: Get User Loyalty Card → Card Type
                tracing.Trace("Get User Loyalty Card → Card Type");
                var userCard = service.Retrieve("new_loyaltycard", cardRef.Id, new ColumnSet("new_cardtype", "new_totalpoints"));
                var cardTypeRef = userCard.GetAttributeValue<EntityReference>("new_cardtype");
                if (cardTypeRef == null) return;

                tracing.Trace("cardTypeRef.Id = " + cardTypeRef.Id);
                tracing.Trace("categoryRef.Id = " + categoryRef.Id);
                // Step 3: Get Loyalty Program Config record for (CardType + Category)
                tracing.Trace("Get Loyalty Program Config record for (CardType + Category)");
                var query = new QueryExpression("new_loyaltyprogramconfiguration")
                {
                    ColumnSet = new ColumnSet("new_minspendamount", "new_pointsearnedperunit"),
                    Criteria = {
                    Conditions = {
                    new ConditionExpression("new_loyaltycardtype", ConditionOperator.Equal, cardTypeRef.Id),
                    new ConditionExpression("new_productcategory", ConditionOperator.Equal, categoryRef.Id),
                    new ConditionExpression("new_currency", ConditionOperator.Equal, currencyRef.Id)

                }
            }
                };

                var config = service.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (config == null) return;

                int minSpend = Convert.ToInt32(config.GetAttributeValue<int>("new_minspendamount"));
                decimal pointsPerUnit = config.GetAttributeValue<decimal>("new_pointsearnedperunit");

                // Step 4: Calculate Points Earned
                tracing.Trace("Calculate Points Earned");
                decimal pointsEarned = (purchasePrice / minSpend) * pointsPerUnit;
                tracing.Trace("Points Earned = " + pointsEarned);

                // Step 5: Update Points Earned in Purchase Entry
                tracing.Trace("Update Points Earned in Purchase Entry");
                var updatePurchase = new Entity("new_purchaseentry", purchaseId);
                updatePurchase["new_pointsearned"] = pointsEarned;
                service.Update(updatePurchase);

                // Step 6: Update Points Accumulated in User Loyalty Card
                tracing.Trace("Update Points Accumulated in User Loyalty Card");
                decimal oldPoints = userCard.GetAttributeValue<decimal>("new_totalpoints");
                decimal updatedPoints = oldPoints + pointsEarned;

                var updateCard = new Entity("new_loyaltycard", cardRef.Id);
                updateCard["new_totalpoints"] = updatedPoints;
                service.Update(updateCard);
            }
            catch (Exception ex)
            {
                tracing.Trace("Exception Occurred " + ex.Message);
                throw (ex);
            }
        }
    }

}
