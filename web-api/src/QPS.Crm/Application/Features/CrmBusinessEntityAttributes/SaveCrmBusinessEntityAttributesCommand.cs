using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;

namespace QPS.Crm.Application.Features.CrmBusinessEntityAttributes;

public class SaveCrmBusinessEntityAttributesCommand : IRequest<bool>
{
    public CrmBusinessEntityAttributeSaveRequest Request { get; set; } = null!;
}

public class SaveCrmBusinessEntityAttributesHandler : IRequestHandler<SaveCrmBusinessEntityAttributesCommand, bool>
{
    private readonly ICrmDbContext _dbContext;

    public SaveCrmBusinessEntityAttributesHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(SaveCrmBusinessEntityAttributesCommand request, CancellationToken cancellationToken)
    {
        var values = NormalizeValues(request.Request.Values);
        var oldAttributes = await GetOldAttributes(request.Request, cancellationToken);

        _dbContext.CrmBusinessEntityAttributes.RemoveRange(oldAttributes);
        
        AddNewAttributes(request.Request, values);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static List<string> NormalizeValues(IEnumerable<string> values)
    {
        return values
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<CrmBusinessEntityAttribute>> GetOldAttributes(
        CrmBusinessEntityAttributeSaveRequest request,
        CancellationToken cancellationToken)
    {
        return await _dbContext.CrmBusinessEntityAttributes
            .Where(attribute =>
                attribute.EntityType == request.EntityType &&
                attribute.EntityId == request.EntityId &&
                attribute.AttributeCode == request.AttributeCode)
            .ToListAsync(cancellationToken);
    }

    private void AddNewAttributes(CrmBusinessEntityAttributeSaveRequest request, List<string> values)
    {
        var sortOrder = 1;

        foreach (var value in values)
        {
            _dbContext.CrmBusinessEntityAttributes.Add(new CrmBusinessEntityAttribute(
                request.EntityType,
                request.EntityId,
                request.AttributeCode,
                value,
                sortOrder++));
        }
    }

}
