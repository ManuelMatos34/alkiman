using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;

namespace Alkiman.Application.Customers;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;

    public CustomerService(ICustomerRepository repository, ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var customers = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return customers.Select(ToResponse).ToList();
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await GetOwnedOrThrowAsync(id, cancellationToken);
        return ToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            FullName = request.FullName,
            IdentityNumber = request.IdentityNumber,
            Phone = request.Phone,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.Auth0UserId
        };

        await _repository.CreateAsync(customer, cancellationToken);
        return ToResponse(customer);
    }

    public async Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await GetOwnedOrThrowAsync(id, cancellationToken);

        customer.FullName = request.FullName;
        customer.IdentityNumber = request.IdentityNumber;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedBy = _currentLandlord.Auth0UserId;

        await _repository.UpdateAsync(customer, cancellationToken);
        return ToResponse(customer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GetOwnedOrThrowAsync(id, cancellationToken);
        await _repository.DeleteAsync(id, cancellationToken);
    }

    private async Task<Customer> GetOwnedOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var customer = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), id);

        if (customer.LandlordId != landlordId)
            throw new ForbiddenException("El cliente no pertenece al negocio autenticado.");

        return customer;
    }

    private static CustomerResponse ToResponse(Customer customer) =>
        new(customer.Id, customer.FullName, customer.IdentityNumber, customer.Phone, customer.Email, customer.CreatedAt);
}
