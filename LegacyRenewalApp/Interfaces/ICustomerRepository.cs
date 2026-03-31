namespace LegacyRenewalApp.Interfaces;

public interface ICustomerRepository{
    Customer GetById(int id);
}