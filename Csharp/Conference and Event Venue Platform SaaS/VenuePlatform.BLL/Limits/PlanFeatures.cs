using VenuePlatform.Contracts.Billing;

namespace VenuePlatform.BLL.Limits;

public static class PlanFeatures
{
    public static bool AllowsReportZeroFill(CompanyPlan plan) => plan == CompanyPlan.Pro;
    public static bool AllowsInvoicePdfAttachment(CompanyPlan plan) => plan == CompanyPlan.Pro;
}
