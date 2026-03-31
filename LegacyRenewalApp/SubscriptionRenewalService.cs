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

    // Конструктор без параметрів для сумісності з LegacyRenewalAppConsumer
    public SubscriptionRenewalService() : this(
        new CustomerRepositoryWrapper(),
        new PlanRepositoryWrapper(),
        new BillingGatewayWrapper(),
        new PricingService(),
        new DiscountCalculator()) { }

    // Конструктор для Dependency Injection (тестування)
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

    public RenewalInvoice CreateRenewalInvoice(int customerId, string planCode, int seatCount, string paymentMethod, bool includePremiumSupport, bool useLoyaltyPoints)
    {
        ValidateInput(customerId, planCode, seatCount, paymentMethod);

        var normalizedPlan = planCode.Trim().ToUpperInvariant();
        var normalizedMethod = paymentMethod.Trim().ToUpperInvariant();

        var customer = _customerRepo.GetById(customerId);
        var plan = _planRepo.GetByCode(normalizedPlan);

        if (!customer.IsActive)
            throw new InvalidOperationException("Inactive customers cannot renew subscriptions");

        decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
        var (discount, notes) = _discounts.CalculateDiscounts(customer, plan, baseAmount, seatCount, useLoyaltyPoints);

        decimal subtotal = Math.Max(baseAmount - discount, 300m);
        if (subtotal == 300m && baseAmount - discount < 300m)
            notes += "minimum discounted subtotal applied; ";

        decimal supportFee = includePremiumSupport ? _pricing.GetSupportFee(normalizedPlan) : 0m;
        if (includePremiumSupport) notes += "premium support included; ";

        decimal paymentFee = (subtotal + supportFee) * _pricing.GetPaymentFeeRate(normalizedMethod);
        if (paymentFee > 0) notes += $"{normalizedMethod.ToLower()} payment fee; ";

        decimal taxRate = _pricing.GetTaxRate(customer.Country);
        decimal taxBase = subtotal + supportFee + paymentFee;
        decimal taxAmount = taxBase * taxRate;

        decimal finalAmount = Math.Max(taxBase + taxAmount, 500m);
        if (finalAmount == 500m) notes += "minimum invoice amount applied; ";

        var invoice = CreateInvoiceObject(customer, normalizedPlan, normalizedMethod, seatCount, baseAmount, discount, supportFee, paymentFee, taxAmount, finalAmount, notes);

        _billingGateway.SaveInvoice(invoice);
        SendNotification(customer, normalizedPlan, invoice);

        return invoice;
    }

    private void ValidateInput(int customerId, string planCode, int seatCount, string paymentMethod)
    {
        if (customerId <= 0) throw new ArgumentException("Customer id must be positive");
        if (string.IsNullOrWhiteSpace(planCode)) throw new ArgumentException("Plan code is required");
        if (seatCount <= 0) throw new ArgumentException("Seat count must be positive");
        if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("Payment method is required");
    }

    private RenewalInvoice CreateInvoiceObject(Customer customer, string planCode, string method, int seats, decimal baseAmt, decimal disc, decimal support, decimal payFee, decimal tax, decimal final, string notes)
    {
        return new RenewalInvoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customer.Id}-{planCode}",
            CustomerName = customer.FullName,
            PlanCode = planCode,
            PaymentMethod = method,
            SeatCount = seats,
            BaseAmount = Math.Round(baseAmt, 2, MidpointRounding.AwayFromZero),
            DiscountAmount = Math.Round(disc, 2, MidpointRounding.AwayFromZero),
            SupportFee = Math.Round(support, 2, MidpointRounding.AwayFromZero),
            PaymentFee = Math.Round(payFee, 2, MidpointRounding.AwayFromZero),
            TaxAmount = Math.Round(tax, 2, MidpointRounding.AwayFromZero),
            FinalAmount = Math.Round(final, 2, MidpointRounding.AwayFromZero),
            Notes = notes.Trim(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private void SendNotification(Customer customer, string planCode, RenewalInvoice invoice)
    {
        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            string subject = "Subscription renewal invoice";
            string body = $"Hello {customer.FullName}, your renewal for plan {planCode} has been prepared. Final amount: {invoice.FinalAmount:F2}.";
            _billingGateway.SendEmail(customer.Email, subject, body);
        }
    }
}