namespace Alkiman.Application.Customers;

public record CustomerResponse(
    Guid Id,
    string FullName,
    string? IdentityNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? Country,
    DateTime CreatedAt);

public record CreateCustomerRequest(string FullName, string IdentityNumber, string? Phone, string? Email);

public record UpdateCustomerRequest(string FullName, string IdentityNumber, string? Phone, string? Email);
