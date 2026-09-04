using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Landlords;
using Alkiman.Application.Roles;
using Alkiman.Application.Users;
using Alkiman.Domain.Entities;

namespace Alkiman.Application.Me;

public class MeService : IMeService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILandlordRepository _landlordRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public MeService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILandlordRepository landlordRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _landlordRepository = landlordRepository;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<MeResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return await ToResponseAsync(user, cancellationToken);
    }

    public async Task<MeResponse> UpdateAsync(UpdateMeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppValidationException("El nombre completo es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new AppValidationException("El email es obligatorio.");

        var user = await GetCurrentUserAsync(cancellationToken);

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existing is not null && existing.Id != user.Id)
                throw new AppValidationException("Ya existe un usuario con ese email.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId.ToString();

        await _userRepository.UpdateAsync(user, cancellationToken);
        return await ToResponseAsync(user, cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangeMyPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NewPassword.Length < 8)
            throw new AppValidationException("La contraseña nueva debe tener al menos 8 caracteres.");

        var user = await GetCurrentUserAsync(cancellationToken);
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new AppValidationException("La contraseña actual es incorrecta.");

        await _userRepository.UpdatePasswordAsync(user.Id, _passwordHasher.Hash(request.NewPassword), cancellationToken);
        // Cambiar la propia contraseña satisface cualquier exigencia pendiente de cambiarla (ver MustChangePassword).
        await _userRepository.SetMustChangePasswordAsync(user.Id, false, cancellationToken);
    }

    public async Task<MeResponse> UpdateTwoFactorAsync(UpdateMyTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);

        user.TwoFactorEnabled = request.Enabled;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId.ToString();

        await _userRepository.UpdateAsync(user, cancellationToken);
        return await ToResponseAsync(user, cancellationToken);
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);

    private async Task<MeResponse> ToResponseAsync(User user, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        var permissions = role is null
            ? Array.Empty<string>()
            : await _roleRepository.GetPermissionCodesAsync(role.Id, cancellationToken);
        var landlord = await _landlordRepository.GetByIdAsync(user.LandlordId, cancellationToken);

        return new MeResponse(
            user.Id,
            user.FullName,
            user.Email,
            role?.Name ?? "—",
            user.IsOwner,
            permissions,
            user.TwoFactorEnabled,
            user.LandlordId,
            landlord?.BusinessName ?? "",
            user.CreatedAt);
    }
}
