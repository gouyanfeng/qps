using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmContacts;

internal readonly record struct CrmContactTarget(string EntityType, Guid EntityId)
{
    public bool IsHerbBaseSubject => EntityType == CrmCodes.HerbBaseSubjectEntityType;

    public bool IsVendor => EntityType == CrmCodes.VendorEntityType;

    public void EnsureSupported()
    {
        if (!IsHerbBaseSubject && !IsVendor)
        {
            throw new BusinessException(400, "不支持的联系人对象类型");
        }
    }
}
