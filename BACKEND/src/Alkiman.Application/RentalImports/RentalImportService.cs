using System.Globalization;
using Alkiman.Application.Assets;
using Alkiman.Application.Categories;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Customers;
using Alkiman.Application.Rentals;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.RentalImports;

/// <summary>
/// Mantenimiento para migrar alquileres ya existentes (llevados en otro sistema o manualmente)
/// hacia Alkiman en un solo lote. Cada fila se procesa de forma independiente: si una falla, no
/// aborta el resto (mismo criterio que <see cref="Alkiman.Application.Emails.EmailService.SendMassAsync"/>).
/// Cliente, categoría y activo se matchean por identificación/nombre dentro del landlord actual
/// y, si no existen, se crean sobre la marcha para no obligar a precargarlos antes de importar.
/// </summary>
public class RentalImportService : IRentalImportService
{
    private readonly IRentalRepository _rentalRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentLandlordService _currentLandlord;

    public RentalImportService(
        IRentalRepository rentalRepository,
        IAssetRepository assetRepository,
        ICustomerRepository customerRepository,
        ICategoryRepository categoryRepository,
        ICurrentLandlordService currentLandlord)
    {
        _rentalRepository = rentalRepository;
        _assetRepository = assetRepository;
        _customerRepository = customerRepository;
        _categoryRepository = categoryRepository;
        _currentLandlord = currentLandlord;
    }

    public async Task<ImportRentalsResponse> ImportAsync(ImportRentalsRequest request, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var userId = _currentLandlord.UserId;

        // Los clientes creados desde el Portal de Rentas pueden no tener identificación
        // (no se pide en esa pasarela); se excluyen del matcheo por identidad, que es
        // el criterio de esta importación.
        var customersByIdentity = (await _customerRepository.GetAllByLandlordAsync(landlordId, cancellationToken))
            .Where(c => !string.IsNullOrWhiteSpace(c.IdentityNumber))
            .GroupBy(c => c.IdentityNumber!.Trim())
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var assetsByName = (await _assetRepository.GetAllByLandlordAsync(landlordId, cancellationToken))
            .GroupBy(a => a.Name.Trim())
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var categoriesByName = (await _categoryRepository.GetAllByLandlordAsync(landlordId, cancellationToken))
            .GroupBy(c => c.Name.Trim())
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var results = new List<RentalImportRowResult>();
        var rowNumber = 0;

        foreach (var row in request.Rows)
        {
            rowNumber++;
            try
            {
                results.Add(await ProcessRowAsync(
                    row, rowNumber, landlordId, userId,
                    customersByIdentity, assetsByName, categoriesByName,
                    cancellationToken));
            }
            catch (Exception ex)
            {
                results.Add(new RentalImportRowResult(rowNumber, false, $"Error inesperado: {ex.Message}", null, false, false, false));
            }
        }

        return new ImportRentalsResponse(results.Count, results.Count(r => r.Success), results.Count(r => !r.Success), results);
    }

    private async Task<RentalImportRowResult> ProcessRowAsync(
        RentalImportRow row,
        int rowNumber,
        Guid landlordId,
        string userId,
        Dictionary<string, Customer> customersByIdentity,
        Dictionary<string, Asset> assetsByName,
        Dictionary<string, Category> categoriesByName,
        CancellationToken cancellationToken)
    {
        RentalImportRowResult Fail(string message) => new(rowNumber, false, message, null, false, false, false);

        var customerFullName = row.CustomerFullName?.Trim();
        var customerIdentity = row.CustomerIdentityNumber?.Trim();
        var assetName = row.AssetName?.Trim();

        if (string.IsNullOrWhiteSpace(customerFullName))
            return Fail("El nombre del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(customerIdentity))
            return Fail("La identificación del cliente es obligatoria.");
        if (string.IsNullOrWhiteSpace(assetName))
            return Fail("El nombre del activo es obligatorio.");

        if (!DateTime.TryParse(row.StartDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            return Fail($"La fecha de inicio '{row.StartDate}' no es una fecha válida.");
        if (!DateTime.TryParse(row.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            return Fail($"La fecha de fin '{row.EndDate}' no es una fecha válida.");
        if (endDate <= startDate)
            return Fail("La fecha de fin debe ser posterior a la fecha de inicio.");

        if (!decimal.TryParse(row.TotalPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var totalPrice))
            return Fail($"El precio total '{row.TotalPrice}' no es un número válido.");
        if (totalPrice < 0)
            return Fail("El precio total no puede ser negativo.");

        var statusText = string.IsNullOrWhiteSpace(row.Status) ? "Active" : row.Status.Trim();
        if (!Enum.TryParse<RentalStatus>(statusText, ignoreCase: true, out var status))
            return Fail($"El estado '{row.Status}' no es válido. Usá Active, Completed u Overdue.");

        var now = DateTime.UtcNow;
        var customerCreated = false;
        var assetCreated = false;
        var categoryCreated = false;

        // --- Cliente: matchea por identificación dentro del landlord; si no existe, se crea. ---
        if (!customersByIdentity.TryGetValue(customerIdentity, out var customer))
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                LandlordId = landlordId,
                FullName = customerFullName,
                IdentityNumber = customerIdentity,
                Phone = string.IsNullOrWhiteSpace(row.CustomerPhone) ? null : row.CustomerPhone.Trim(),
                Email = string.IsNullOrWhiteSpace(row.CustomerEmail) ? null : row.CustomerEmail.Trim(),
                CreatedAt = now,
                CreatedBy = userId
            };

            try
            {
                await _customerRepository.CreateAsync(customer, cancellationToken);
            }
            catch (Exception ex)
            {
                return Fail($"No se pudo crear el cliente '{customerFullName}' (¿identificación duplicada?): {ex.Message}");
            }

            customersByIdentity[customerIdentity] = customer;
            customerCreated = true;
        }

        // --- Activo: matchea por nombre dentro del landlord; si no existe, se crea (requiere categoría). ---
        if (!assetsByName.TryGetValue(assetName, out var asset))
        {
            var categoryName = row.AssetCategoryName?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
                return Fail($"El activo '{assetName}' no existe todavía y no se indicó una categoría (columna AssetCategoryName) para crearlo.");

            if (!categoriesByName.TryGetValue(categoryName, out var category))
            {
                category = new Category
                {
                    LandlordId = landlordId,
                    Name = categoryName,
                    CreatedAt = now,
                    CreatedBy = userId
                };
                category.Id = await _categoryRepository.CreateAsync(category, cancellationToken);
                categoriesByName[categoryName] = category;
                categoryCreated = true;
            }

            var basePrice = 0m;
            if (!string.IsNullOrWhiteSpace(row.AssetBasePrice) &&
                !decimal.TryParse(row.AssetBasePrice, NumberStyles.Number, CultureInfo.InvariantCulture, out basePrice))
                return Fail($"El precio base del activo '{row.AssetBasePrice}' no es un número válido.");
            if (basePrice < 0)
                return Fail("El precio base del activo no puede ser negativo.");

            var stock = 1;
            if (!string.IsNullOrWhiteSpace(row.AssetStock) && !int.TryParse(row.AssetStock, out stock))
                return Fail($"El stock del activo '{row.AssetStock}' no es un número entero válido.");
            if (stock < 0)
                return Fail("El stock del activo no puede ser negativo.");

            var rentalTypeText = string.IsNullOrWhiteSpace(row.AssetRentalType) ? "Monthly" : row.AssetRentalType.Trim();
            if (!Enum.TryParse<RentalTypeOption>(rentalTypeText, ignoreCase: true, out var rentalType))
                return Fail($"El tipo de renta '{row.AssetRentalType}' no es válido. Usá Daily, Weekly, Biweekly, Monthly o Annual.");

            asset = new Asset
            {
                Id = Guid.NewGuid(),
                LandlordId = landlordId,
                CategoryId = category.Id,
                Name = assetName,
                Status = AssetStatus.Available,
                RentalType = rentalType,
                BasePrice = basePrice,
                Stock = stock,
                CreatedAt = now,
                CreatedBy = userId
            };

            try
            {
                await _assetRepository.CreateAsync(asset, cancellationToken);
            }
            catch (Exception ex)
            {
                return Fail($"No se pudo crear el activo '{assetName}': {ex.Message}");
            }

            assetsByName[assetName] = asset;
            assetCreated = true;
        }

        // --- Renta ---
        var rental = new Rental
        {
            Id = Guid.NewGuid(),
            AssetId = asset.Id,
            CustomerId = customer.Id,
            StartDate = startDate,
            EndDate = endDate,
            TotalPrice = totalPrice,
            Status = status,
            CreatedAt = now,
            CreatedBy = userId
        };

        try
        {
            await _rentalRepository.CreateAsync(rental, cancellationToken);
        }
        catch (Exception ex)
        {
            return Fail($"No se pudo crear la renta: {ex.Message}");
        }

        // El activo solo se marca "Rentado" si la renta importada sigue activa; si es histórica
        // (Completed/Overdue) se deja el estado del activo como esté, para no forzarlo por error.
        if (status == RentalStatus.Active && asset.Status != AssetStatus.Rented)
        {
            asset.Status = AssetStatus.Rented;
            asset.UpdatedAt = now;
            asset.UpdatedBy = userId;
            await _assetRepository.UpdateAsync(asset, cancellationToken);
        }

        return new RentalImportRowResult(rowNumber, true, null, rental.Id, customerCreated, assetCreated, categoryCreated);
    }
}
