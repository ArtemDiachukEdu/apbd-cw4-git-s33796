namespace LegacyRenewalApp.Interfaces;

public interface IBillingGatewayWrapper{
    void SaveInvoice(RenewalInvoice invoice);
    void SendEmail(string email, string subject, string body);
}