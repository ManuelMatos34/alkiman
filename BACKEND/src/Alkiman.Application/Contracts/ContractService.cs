using Alkiman.Application.Assets;
using Alkiman.Application.Categories;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.ContractTemplates;
using Alkiman.Application.Customers;
using Alkiman.Application.Emails;
using Alkiman.Application.Landlords;
using Alkiman.Application.Rentals;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Alkiman.Application.Contracts;

/// <summary>
/// Genera y administra los contratos de las rentas. La generación (GenerateForRentalAsync)
/// es el corazón del requerimiento "cada renta que se genere debe crear un contrato con
/// todos los detalles y firma digital que se le enviará al cliente": se llama automáticamente
/// desde RentalService.CreateAsync (rentas manuales) y PortalService.CheckoutAsync (Portal),
/// nunca se crea un contrato "a mano" vía API.
/// </summary>
public class ContractService : IContractService
{
    private const string DefaultTemplateContent = """
        CONTRATO DE ALQUILER

        Entre {{NombreNegocio}} (el "Arrendador") y {{NombreCliente}}, identificado con {{IdentificacionCliente}} (el "Arrendatario"), se acuerda lo siguiente:

        1. OBJETO: El Arrendador entrega en alquiler al Arrendatario el activo "{{NombreActivo}}" (categoría: {{CategoriaActivo}}).
        2. VIGENCIA: Desde el {{FechaInicio}} hasta el {{FechaFin}}, con modalidad de renta {{TipoRenta}}.
        3. PRECIO: El Arrendatario pagará un total de {{PrecioTotal}} por el período contratado.
        4. DATOS DE CONTACTO DEL ARRENDATARIO: Teléfono: {{TelefonoCliente}} — Correo: {{EmailCliente}}.

        Este contrato fue generado automáticamente el {{FechaHoy}}. El negocio todavía no configuró una plantilla propia para esta categoría de activos; puede crear una desde el mantenimiento de Contratos.
        """;

    private readonly IContractRepository _repository;
    private readonly IContractTemplateRepository _templateRepository;
    private readonly IContractPdfRenderer _pdfRenderer;
    private readonly ILandlordRepository _landlordRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailSender _emailSender;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IConfiguration _configuration;
    private readonly IRentalRepository _rentalRepository;
    private readonly IAssetRepository _assetRepository;

    public ContractService(
        IContractRepository repository,
        IContractTemplateRepository templateRepository,
        IContractPdfRenderer pdfRenderer,
        ILandlordRepository landlordRepository,
        ICustomerRepository customerRepository,
        ICategoryRepository categoryRepository,
        IEmailRepository emailRepository,
        IEmailSender emailSender,
        ICurrentLandlordService currentLandlord,
        IConfiguration configuration,
        IRentalRepository rentalRepository,
        IAssetRepository assetRepository)
    {
        _repository = repository;
        _templateRepository = templateRepository;
        _pdfRenderer = pdfRenderer;
        _landlordRepository = landlordRepository;
        _customerRepository = customerRepository;
        _categoryRepository = categoryRepository;
        _emailRepository = emailRepository;
        _emailSender = emailSender;
        _currentLandlord = currentLandlord;
        _configuration = configuration;
        _rentalRepository = rentalRepository;
        _assetRepository = assetRepository;
    }

    public async Task<Contract> GenerateForRentalAsync(
        Rental rental,
        Asset asset,
        Customer customer,
        Guid landlordId,
        string createdBy,
        string? signatureImageBase64,
        CancellationToken cancellationToken = default)
    {
        var landlord = await _landlordRepository.GetByIdAsync(landlordId, cancellationToken);
        var businessName = landlord?.BusinessName ?? "Alkiman";

        var activeTemplate = await _templateRepository.GetActiveByCategoryAsync(landlordId, asset.CategoryId, cancellationToken);
        var templateContent = activeTemplate?.Content ?? DefaultTemplateContent;

        var category = await _categoryRepository.GetByIdAsync(asset.CategoryId, cancellationToken);

        var now = DateTime.UtcNow;
        var contentSnapshot = MergeContent(templateContent, businessName, category?.Name ?? "N/A", asset, customer, rental);
        var isSigned = !string.IsNullOrWhiteSpace(signatureImageBase64);

        var pdfBytes = _pdfRenderer.Render(new ContractPdfModel(
            businessName,
            customer.FullName,
            contentSnapshot,
            now,
            signatureImageBase64,
            isSigned ? now : null,
            landlord?.SignatureBase64));

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            RentalId = rental.Id,
            CustomerId = customer.Id,
            ContractTemplateId = activeTemplate?.Id,
            ContentSnapshot = contentSnapshot,
            PdfContent = pdfBytes,
            SignatureImageBase64 = signatureImageBase64,
            Status = isSigned ? ContractStatus.Signed : ContractStatus.Pending,
            SignedAt = isSigned ? now : null,
            EmailSent = false,
            CreatedAt = now,
            CreatedBy = createdBy
        };

        contract.EmailSent = await TrySendContractEmailAsync(landlordId, customer, asset, pdfBytes, contract.CreatedBy, rental.AccessToken, cancellationToken);

        contract.Id = await _repository.CreateAsync(contract, cancellationToken);
        return contract;
    }

    public async Task<string> PreviewContentAsync(
        Asset asset, string customerFullName, string? customerIdentityNumber, string? customerPhone,
        string? customerEmail, string? customerAddress, DateTime startDate, DateTime endDate,
        decimal totalPrice, Guid landlordId, CancellationToken cancellationToken = default)
    {
        var landlord = await _landlordRepository.GetByIdAsync(landlordId, cancellationToken);
        var businessName = landlord?.BusinessName ?? "Alkiman";
        var activeTemplate = await _templateRepository.GetActiveByCategoryAsync(landlordId, asset.CategoryId, cancellationToken);
        var templateContent = activeTemplate?.Content ?? DefaultTemplateContent;
        var category = await _categoryRepository.GetByIdAsync(asset.CategoryId, cancellationToken);

        // Objetos transitorios SOLO para reutilizar MergeContent: es una vista previa, nada se
        // persiste acá (a diferencia de GenerateForRentalAsync, que sí crea el Contract real).
        var previewCustomer = new Customer
        {
            FullName = customerFullName,
            IdentityNumber = customerIdentityNumber,
            Phone = customerPhone,
            Email = customerEmail,
            Address = customerAddress
        };
        var previewRental = new Rental
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalPrice = totalPrice
        };

        return MergeContent(templateContent, businessName, category?.Name ?? "N/A", asset, previewCustomer, previewRental);
    }

    public async Task<IReadOnlyList<ContractResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var contracts = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return await ToResponsesAsync(contracts, cancellationToken);
    }

    public async Task<ContractResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedOrThrowAsync(id, cancellationToken);
        var responses = await ToResponsesAsync(new[] { contract }, cancellationToken);
        return responses[0];
    }

    public async Task<ContractResponse?> GetByRentalIdAsync(Guid rentalId, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var contract = await _repository.GetByRentalIdAsync(rentalId, cancellationToken);
        if (contract is null || contract.LandlordId != landlordId)
            return null;

        var responses = await ToResponsesAsync(new[] { contract }, cancellationToken);
        return responses[0];
    }

    public async Task<(byte[] Bytes, string FileName)> GetPdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedOrThrowAsync(id, cancellationToken);
        return (contract.PdfContent, $"contrato-{contract.Id}.pdf");
    }

    public async Task<ContractResponse> SignAsync(Guid id, SignContractRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SignatureImageBase64))
            throw new AppValidationException("La firma es obligatoria.");

        var contract = await GetOwnedOrThrowAsync(id, cancellationToken);

        var landlord = await _landlordRepository.GetByIdAsync(contract.LandlordId, cancellationToken);
        var customer = await _customerRepository.GetByIdAsync(contract.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), contract.CustomerId);

        var now = DateTime.UtcNow;
        var pdfBytes = _pdfRenderer.Render(new ContractPdfModel(
            landlord?.BusinessName ?? "Alkiman",
            customer.FullName,
            contract.ContentSnapshot,
            contract.CreatedAt,
            request.SignatureImageBase64,
            now,
            landlord?.SignatureBase64));

        contract.SignatureImageBase64 = request.SignatureImageBase64;
        contract.Status = ContractStatus.Signed;
        contract.SignedAt = now;
        contract.PdfContent = pdfBytes;
        contract.UpdatedAt = now;
        contract.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(contract, cancellationToken);

        var responses = await ToResponsesAsync(new[] { contract }, cancellationToken);
        return responses[0];
    }

    /// <summary>Reenvía el PDF ya generado de un contrato al cliente por correo (a demanda, no solo automáticamente al crearse).</summary>
    public async Task<ContractResponse> ResendEmailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await GetOwnedOrThrowAsync(id, cancellationToken);
        var rental = await _rentalRepository.GetByIdAsync(contract.RentalId, cancellationToken)
            ?? throw new NotFoundException(nameof(Rental), contract.RentalId);
        var asset = await _assetRepository.GetByIdAsync(rental.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), rental.AssetId);
        var customer = await _customerRepository.GetByIdAsync(contract.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), contract.CustomerId);

        var sent = await TrySendContractEmailAsync(contract.LandlordId, customer, asset, contract.PdfContent, _currentLandlord.UserId, rental.AccessToken, cancellationToken);

        contract.EmailSent = sent || contract.EmailSent; // no pisar un envío previo exitoso si este reintento falla
        contract.UpdatedAt = DateTime.UtcNow;
        contract.UpdatedBy = _currentLandlord.UserId;
        await _repository.UpdateAsync(contract, cancellationToken);

        var responses = await ToResponsesAsync(new[] { contract }, cancellationToken);
        return responses[0];
    }

    /// <summary>Envía el PDF del contrato al cliente por correo. Nunca lanza: si falla (o el cliente no tiene correo), el contrato queda igual generado con EmailSent = false.</summary>
    private async Task<bool> TrySendContractEmailAsync(
        Guid landlordId, Customer customer, Asset asset, byte[] pdfBytes, string createdBy, Guid rentalAccessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customer.Email))
            return false;

        var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var rentalLink = $"{baseUrl}/mi-renta/{rentalAccessToken}";

        const string subject = "Tu contrato de alquiler";
        var body = $"Hola {customer.FullName}, adjuntamos el contrato de tu alquiler de \"{asset.Name}\". Gracias por confiar en nosotros."
            + $"\n\nPuedes ver el detalle de tu renta, pedir una prórroga o cancelarla desde este link: {rentalLink}";

        var result = await _emailSender.SendWithAttachmentsAsync(
            customer.Email!,
            customer.FullName,
            subject,
            body,
            new[] { new EmailAttachment($"contrato-{asset.Name}.pdf", "application/pdf", pdfBytes) },
            cancellationToken);

        var message = new EmailMessage
        {
            LandlordId = landlordId,
            CustomerId = customer.Id,
            Type = "Individual",
            RecipientName = customer.FullName,
            RecipientEmail = customer.Email!,
            Subject = subject,
            Body = body,
            Status = result.Success ? "Sent" : "Failed",
            ErrorMessage = result.Success ? null : result.ErrorMessage,
            SentAt = result.Success ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        await _emailRepository.CreateAsync(message, cancellationToken);

        return result.Success;
    }

    private async Task<Contract> GetOwnedOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var contract = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Contract), id);

        if (contract.LandlordId != landlordId)
            throw new ForbiddenException("El contrato no pertenece al negocio autenticado.");

        return contract;
    }

    private async Task<IReadOnlyList<ContractResponse>> ToResponsesAsync(IReadOnlyList<Contract> contracts, CancellationToken cancellationToken)
    {
        var responses = new List<ContractResponse>(contracts.Count);
        foreach (var contract in contracts)
        {
            var customer = await _customerRepository.GetByIdAsync(contract.CustomerId, cancellationToken);
            string? templateName = null;
            if (contract.ContractTemplateId.HasValue)
            {
                var template = await _templateRepository.GetByIdAsync(contract.ContractTemplateId.Value, cancellationToken);
                templateName = template?.Name;
            }

            responses.Add(new ContractResponse(
                contract.Id, contract.RentalId, contract.CustomerId, customer?.FullName ?? "—",
                contract.ContractTemplateId, templateName, contract.Status, contract.SignedAt,
                contract.EmailSent, contract.CreatedAt));
        }
        return responses;
    }

    private static string MergeContent(string template, string businessName, string categoryName, Asset asset, Customer customer, Rental rental)
    {
        var tokens = new Dictionary<string, string>
        {
            ["{{NombreNegocio}}"] = businessName,
            ["{{NombreCliente}}"] = customer.FullName,
            ["{{IdentificacionCliente}}"] = customer.IdentityNumber ?? "N/A",
            ["{{TelefonoCliente}}"] = customer.Phone ?? "N/A",
            ["{{EmailCliente}}"] = customer.Email ?? "N/A",
            ["{{DireccionCliente}}"] = customer.Address ?? "N/A",
            ["{{NombreActivo}}"] = asset.Name,
            ["{{DescripcionActivo}}"] = asset.Description ?? "N/A",
            ["{{CategoriaActivo}}"] = categoryName,
            ["{{FechaInicio}}"] = rental.StartDate.ToString("dd/MM/yyyy"),
            ["{{FechaFin}}"] = rental.EndDate.ToString("dd/MM/yyyy"),
            ["{{TipoRenta}}"] = RentalTypeLabel(asset.RentalType),
            ["{{PrecioBase}}"] = asset.BasePrice.ToString("N2"),
            ["{{PrecioTotal}}"] = rental.TotalPrice.ToString("N2"),
            ["{{FechaHoy}}"] = DateTime.UtcNow.ToString("dd/MM/yyyy"),
        };

        var result = template;
        foreach (var (token, value) in tokens)
            result = result.Replace(token, value);
        return result;
    }

    private static string RentalTypeLabel(RentalTypeOption rentalType) => rentalType switch
    {
        RentalTypeOption.Daily => "Diario",
        RentalTypeOption.Weekly => "Semanal",
        RentalTypeOption.Biweekly => "Quincenal",
        RentalTypeOption.Monthly => "Mensual",
        RentalTypeOption.Annual => "Anual",
        _ => rentalType.ToString()
    };
}
