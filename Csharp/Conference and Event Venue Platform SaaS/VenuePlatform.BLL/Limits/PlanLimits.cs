namespace VenuePlatform.BLL.Limits;

using VenuePlatform.Contracts.Billing;

public static class PlanLimits
{
    public static int MaxSpaces(CompanyPlan plan)
    {
        return plan switch
        {
            CompanyPlan.Free => 5,
            CompanyPlan.Pro => 100,
            _ => 5
        };
    }

    public static int MaxBookingsPerMonth(CompanyPlan plan)
    {
        return plan switch
        {
            CompanyPlan.Free => 50,
            CompanyPlan.Pro => 10_000,
            _ => 50
        };
    }
}
