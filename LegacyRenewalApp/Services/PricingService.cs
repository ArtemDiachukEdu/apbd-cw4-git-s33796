namespace LegacyRenewalApp.Services;

public class PricingService
{
    public decimal GetTaxRate(string country) => country switch {
        "Poland" => 0.23m,
        "Germany" => 0.19m,
        "Czech Republic" => 0.21m,
        "Norway" => 0.25m,
        _ => 0.20m
    };

    public decimal GetSupportFee(string planCode) => planCode switch {
        "START" => 250m,
        "PRO" => 400m,
        "ENTERPRISE" => 700m,
        _ => 0m
    };

    public decimal GetPaymentFeeRate(string method) => method switch {
        "CARD" => 0.02m,
        "BANK_TRANSFER" => 0.01m,
        "PAYPAL" => 0.035m,
        _ => 0m
    };
}