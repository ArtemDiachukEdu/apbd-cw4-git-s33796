using LegacyRenewalApp.Interfaces;

namespace LegacyRenewalApp.Services;

public class PlanRepositoryWrapper : ISubscriptionPlanRepository
{
    private readonly SubscriptionPlanRepository _repository = new();

    public SubscriptionPlan GetByCode(string code)
    {
        return _repository.GetByCode(code);
    }
}