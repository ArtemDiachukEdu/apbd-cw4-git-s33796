using LegacyRenewalApp.Interfaces;
using LegacyRenewalApp.Services;

namespace LegacyRenewalApp;

public class SubscriptionRenewalService
{
    private readonly ICustomerRepository _customerRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IBillingGatewayWrapper _billingGateway;
    private readonly PricingService _pricing;
    private readonly DiscountCalculator _discounts;

    public SubscriptionRenewalService() : this(
        new CustomerRepositoryWrapper(),
        new PlanRepositoryWrapper(),
        new BillingGatewayWrapper(),
        new PricingService(),
        new DiscountCalculator()) { }

    public SubscriptionRenewalService(
        ICustomerRepository customerRepo,
        ISubscriptionPlanRepository planRepo,
        IBillingGatewayWrapper billingGateway,
        PricingService pricing,
        DiscountCalculator discounts)
    {
        _customerRepo = customerRepo;
        _planRepo = planRepo;
        _billingGateway = billingGateway;
        _pricing = pricing;
        _discounts = discounts;
    }

    public RenewalInvoice CreateRenewalInvoice(int customerId, string planCode, int seatCount, string paymentMethod,
     bool includePremiumSupport, bool useLoyaltyPoints)
    {
        ValidateInput(customerId, planCode, seatCount, paymentMethod);

        var normalizedPlan = planCode.Trim().ToUpperInvariant();
        var normalizedMethod = paymentMethod.Trim().ToUpperInvariant();

        var customer = _customerRepo.GetById(customerId);
        var plan = _planRepo.GetByCode(normalizedPlan);

        if (!customer.IsActive) throw new InvalidOperationException("Inactive customers cannot renew");

        decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
        var (discount, notes) = _discounts.CalculateDiscounts(customer, plan, baseAmount, seatCount, useLoyaltyPoints);

        decimal subtotal = Math.Max(baseAmount - discount, 300m);
        if (subtotal == 300m && baseAmount - discount < 300m) notes += "minimum discounted subtotal applied; ";

        decimal supportFee = includePremiumSupport ? _pricing.GetSupportFee(normalizedPlan) : 0m;
        if (includePremiumSupport) notes += "premium support included; ";

        decimal paymentFee = (subtotal + supportFee) * _pricing.GetPaymentFeeRate(normalizedMethod);

        decimal taxRate = _pricing.GetTaxRate(customer.Country);
        decimal taxAmount = (subtotal + supportFee + paymentFee) * taxRate;
        decimal finalAmount = Math.Max(subtotal + supportFee + paymentFee + taxAmount, 500m);
        if (finalAmount == 500m) notes += "minimum invoice amount applied; ";

        var invoice = CreateInvoiceObject(customer, normalizedPlan, normalizedMethod, seatCount, baseAmount, discount,
         supportFee, paymentFee, taxAmount, finalAmount, notes);

        _billingGateway.SaveInvoice(invoice);
        SendNotification(customer, normalizedPlan, invoice);

        return invoice;
    }

    private void ValidateInput(int customerId, string planCode, int seatCount, string paymentMethod)
    {
        if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
        if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code required");
        if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
        if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method required");
    }

}