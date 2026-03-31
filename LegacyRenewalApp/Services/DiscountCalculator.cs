namespace LegacyRenewalApp.Services;

public class DiscountCalculator
{
    public (decimal Amount, string Notes) CalculateDiscounts(Customer customer, SubscriptionPlan plan, decimal baseAmount, int seatCount, bool useLoyaltyPoints)
    {
        decimal discount = 0m;
        string notes = "";

        // Segment discount
        if (customer.Segment == "Silver") { discount += baseAmount * 0.05m; notes += "silver discount; "; }
        else if (customer.Segment == "Gold") { discount += baseAmount * 0.10m; notes += "gold discount; "; }
        else if (customer.Segment == "Platinum") { discount += baseAmount * 0.15m; notes += "platinum discount; "; }
        else if (customer.Segment == "Education" && plan.IsEducationEligible) { discount += baseAmount * 0.20m; notes += "education discount; "; }

        // Loyalty years
        if (customer.YearsWithCompany >= 5) { discount += baseAmount * 0.07m; notes += "long-term loyalty discount; "; }
        else if (customer.YearsWithCompany >= 2) { discount += baseAmount * 0.03m; notes += "basic loyalty discount; "; }

        // Seat count
        if (seatCount >= 50) discount += baseAmount * 0.12m;
        else if (seatCount >= 20) discount += baseAmount * 0.08m;
        else if (seatCount >= 10) discount += baseAmount * 0.04m;

        // Points
        if (useLoyaltyPoints && customer.LoyaltyPoints > 0)
        {
            int points = Math.Min(customer.LoyaltyPoints, 200);
            discount += points;
            notes += $"loyalty points used: {points}; ";
        }

        return (discount, notes);
    }
}