using LegacyRenewalApp.Interfaces;

namespace LegacyRenewalApp.Services;

public class CustomerRepositoryWrapper : ICustomerRepository
{
    private readonly CustomerRepository _repository = new();

    public Customer GetById(int id)
    {
        return _repository.GetById(id);
    }
}