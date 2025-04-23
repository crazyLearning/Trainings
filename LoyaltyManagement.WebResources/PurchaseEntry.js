
async function onProductChange(executionContext) {
    var formContext = executionContext.getFormContext();

    var productLookup = formContext.getAttribute("new_product").getValue();
    var currencyLookup = formContext.getAttribute("transactioncurrencyid").getValue();

    // 🧼 If product is cleared, reset dependent fields and return
    if (!productLookup) {
        formContext.getAttribute("new_productcategory").setValue(null);
        formContext.getAttribute("new_purchaseprice").setValue(null);
        formContext.getAttribute("new_pointsearned").setValue(null);
        return;
    }

    // If currency is not selected yet, also return
    if (!currencyLookup) return;

    var productId = productLookup[0].id.replace("{", "").replace("}", "");
    var currencyId = currencyLookup[0].id.replace("{", "").replace("}", "");

    try {
        // 1. Get Product record (Category field)
        Xrm.WebApi.retrieveRecord("product", productId).then(function (product) {
            if (product._parentproductid_value) {
                var categoryRef = [{
                    id: product._parentproductid_value,
                    name: product["_parentproductid_value@OData.Community.Display.V1.FormattedValue"],
                    entityType: "product"
                }];
                formContext.getAttribute("new_productcategory").setValue(categoryRef);
            } else {
                formContext.getAttribute("new_productcategory").setValue(null);
            }
        });

        // 2. Get Price List Item for selected product + currency
        var fetchXml = `
            <fetch top='1'>
              <entity name='productpricelevel'>
                <attribute name='amount' />
                <filter type='and'>
                  <condition attribute='productid' operator='eq' value='${productId}' />
                  <condition attribute='transactioncurrencyid' operator='eq' value='${currencyId}' />
                </filter>
              </entity>
            </fetch>`;

        var encodedFetch = "?fetchXml=" + encodeURIComponent(fetchXml);

        Xrm.WebApi.retrieveMultipleRecords("productpricelevel", encodedFetch).then(function (result) {
            if (result.entities.length > 0) {
                var price = result.entities[0].amount;
                formContext.getAttribute("new_purchaseprice").setValue(price);
            } else {
                formContext.getAttribute("new_purchaseprice").setValue(null);
            }
        });
    } catch (error) {
        console.error("Error fetching product data:", error.message);
    }
}
