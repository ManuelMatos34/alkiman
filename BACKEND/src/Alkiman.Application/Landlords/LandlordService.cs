using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;

namespace Alkiman.Application.Landlords;

public class LandlordService : ILandlordService
{
    private static readonly string[] ValidThemeModes = ["light", "dark"];

    /// <summary>
    /// Colores de acento aceptados. Es el espejo de ACCENT_COLORS en
    /// FRONTEND/src/domain/types/landlord.ts, donde está documentada la lista
    /// completa de lugares a tocar para sumar uno.
    ///
    /// Está duplicado a propósito: el back no le cree al cliente. La columna
    /// CFG_Landlords.AccentColor es un NVARCHAR(20) sin CHECK, así que esta lista
    /// es lo único que impide que por API entre un valor que después ningún CSS
    /// resuelve, y el negocio quede con el tema por defecto y sin explicación.
    /// </summary>
    private static readonly string[] ValidAccentColors =
        ["blue", "sky", "cyan", "teal", "green", "orange", "red", "rose", "pink", "fuchsia", "violet", "slate"];

    private readonly ILandlordRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;

    public LandlordService(ILandlordRepository repository, ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
    }

    public async Task<LandlordResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var landlord = await GetCurrentLandlordAsync(cancellationToken);
        return ToResponse(landlord);
    }

    public async Task<LandlordResponse> UpdateCurrentAsync(UpdateLandlordRequest request, CancellationToken cancellationToken = default)
    {
        var landlord = await GetCurrentLandlordAsync(cancellationToken);

        landlord.BusinessName = request.BusinessName;
        landlord.CountryId = request.CountryId;
        landlord.StateId = request.StateId;
        landlord.CityId = request.CityId;
        landlord.Address = request.Address;
        landlord.Phone1 = request.Phone1;
        landlord.Phone2 = request.Phone2;
        landlord.TaxId = request.TaxId;
        landlord.UpdatedAt = DateTime.UtcNow;
        landlord.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(landlord, cancellationToken);
        return ToResponse(landlord);
    }

    public async Task<LandlordResponse> UpdateAppearanceAsync(UpdateAppearanceRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AppName) || request.AppName.Length > 100)
            throw new AppValidationException("El nombre de la aplicación es inválido.");

        if (!ValidThemeModes.Contains(request.ThemeMode))
            throw new AppValidationException("El modo de tema es inválido.");

        if (!ValidAccentColors.Contains(request.AccentColor))
            throw new AppValidationException("El color de acento es inválido.");

        var landlord = await GetCurrentLandlordAsync(cancellationToken);

        landlord.AppName = request.AppName;
        landlord.ThemeMode = request.ThemeMode;
        landlord.AccentColor = request.AccentColor;
        landlord.UpdatedAt = DateTime.UtcNow;
        landlord.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(landlord, cancellationToken);
        return ToResponse(landlord);
    }

    public async Task<LandlordResponse> UpdateSignatureAsync(UpdateLandlordSignatureRequest request, CancellationToken cancellationToken = default)
    {
        var landlord = await GetCurrentLandlordAsync(cancellationToken);
        landlord.SignatureBase64 = request.SignatureBase64;
        landlord.UpdatedAt = DateTime.UtcNow;
        landlord.UpdatedBy = _currentLandlord.UserId;
        await _repository.UpdateAsync(landlord, cancellationToken);
        return ToResponse(landlord);
    }

    private async Task<Landlord> GetCurrentLandlordAsync(CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        return await _repository.GetByIdAsync(landlordId, cancellationToken)
            ?? throw new NotFoundException(nameof(Landlord), landlordId);
    }

    private static LandlordResponse ToResponse(Landlord landlord) =>
        new(
            landlord.Id,
            landlord.BusinessName,
            landlord.AppName,
            landlord.ThemeMode,
            landlord.AccentColor,
            landlord.CountryId,
            landlord.StateId,
            landlord.CityId,
            landlord.Address,
            landlord.Phone1,
            landlord.Phone2,
            landlord.TaxId,
            landlord.CreatedAt,
            landlord.SignatureBase64);
}
