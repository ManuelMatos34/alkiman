using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;

namespace Alkiman.Application.Landlords;

public class LandlordService : ILandlordService
{
    private readonly ILandlordRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;

    public LandlordService(ILandlordRepository repository, ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
    }

    public async Task<LandlordResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var landlord = await _repository.GetByAuth0UserIdAsync(_currentLandlord.Auth0UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Landlord), _currentLandlord.Auth0UserId);

        return ToResponse(landlord);
    }

    public async Task<LandlordResponse> RegisterAsync(RegisterLandlordRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByAuth0UserIdAsync(_currentLandlord.Auth0UserId, cancellationToken);
        if (existing is not null)
            throw new AppValidationException("Ya existe un negocio registrado para este usuario.");

        var landlord = new Landlord
        {
            Id = Guid.NewGuid(),
            Auth0UserId = _currentLandlord.Auth0UserId,
            BusinessName = request.BusinessName,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.Auth0UserId
        };

        await _repository.CreateAsync(landlord, cancellationToken);
        return ToResponse(landlord);
    }

    public async Task<LandlordResponse> UpdateCurrentAsync(UpdateLandlordRequest request, CancellationToken cancellationToken = default)
    {
        var landlord = await _repository.GetByAuth0UserIdAsync(_currentLandlord.Auth0UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Landlord), _currentLandlord.Auth0UserId);

        landlord.BusinessName = request.BusinessName;
        landlord.Email = request.Email;
        landlord.UpdatedAt = DateTime.UtcNow;
        landlord.UpdatedBy = _currentLandlord.Auth0UserId;

        await _repository.UpdateAsync(landlord, cancellationToken);
        return ToResponse(landlord);
    }

    private static LandlordResponse ToResponse(Landlord landlord) =>
        new(landlord.Id, landlord.BusinessName, landlord.Email, landlord.CreatedAt);
}
