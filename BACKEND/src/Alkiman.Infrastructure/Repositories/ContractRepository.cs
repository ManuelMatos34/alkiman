using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Contracts;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ContractRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, RentalId, CustomerId, ContractTemplateId, ContentSnapshot, PdfContent,
        SignatureImageBase64, Status, SignedAt, EmailSent, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<Contract>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.COM_Contracts
            WHERE LandlordId = @LandlordId
            ORDER BY CreatedAt DESC
            """;
        var result = await connection.QueryAsync<Contract>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<Contract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.COM_Contracts
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Contract>(sql, new { Id = id });
    }

    public async Task<Contract?> GetByRentalIdAsync(Guid rentalId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.COM_Contracts
            WHERE RentalId = @RentalId
            """;
        return await connection.QuerySingleOrDefaultAsync<Contract>(sql, new { RentalId = rentalId });
    }

    public async Task<Guid> CreateAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.COM_Contracts
                (Id, LandlordId, RentalId, CustomerId, ContractTemplateId, ContentSnapshot, PdfContent,
                 SignatureImageBase64, Status, SignedAt, EmailSent, CreatedAt, CreatedBy)
            VALUES
                (@Id, @LandlordId, @RentalId, @CustomerId, @ContractTemplateId, @ContentSnapshot, @PdfContent,
                 @SignatureImageBase64, @Status, @SignedAt, @EmailSent, @CreatedAt, @CreatedBy)
            """;
        // Nota (ver RentalRepository): convertimos el enum a texto explícitamente porque
        // Dapper lo pasaría como int y violaría el CHECK constraint de la columna Status.
        await connection.ExecuteAsync(sql, new
        {
            contract.Id,
            contract.LandlordId,
            contract.RentalId,
            contract.CustomerId,
            contract.ContractTemplateId,
            contract.ContentSnapshot,
            contract.PdfContent,
            contract.SignatureImageBase64,
            Status = contract.Status.ToString(),
            contract.SignedAt,
            contract.EmailSent,
            contract.CreatedAt,
            contract.CreatedBy
        });
        return contract.Id;
    }

    public async Task UpdateAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.COM_Contracts
            SET SignatureImageBase64 = @SignatureImageBase64,
                PdfContent = @PdfContent,
                Status = @Status,
                SignedAt = @SignedAt,
                EmailSent = @EmailSent,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, new
        {
            contract.Id,
            contract.SignatureImageBase64,
            contract.PdfContent,
            Status = contract.Status.ToString(),
            contract.SignedAt,
            contract.EmailSent,
            contract.UpdatedAt,
            contract.UpdatedBy
        });
    }
}
