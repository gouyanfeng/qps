namespace QPS.Crm.Contracts;

public static class CrmCodes
{
    public const string HerbBaseEntityType = "CRM_HERB_BASE";
    public const string HerbBaseSubjectEntityType = "CRM_HERB_BASE_SUBJECT";
    public const string VendorEntityType = "CRM_VENDOR";
    public const string MatchOpportunityEntityType = "CRM_MATCH_OPPORTUNITY";
    public const string HerbProductDictionaryCode = "CRM_HERB_PRODUCT";

    /// <summary>
    /// CrmBusinessEntityAttributes 的属性编码。
    /// 基地品类挂在基地（CRM_HERB_BASE）上，厂商品类挂在厂商（CRM_VENDOR）上。
    /// 列表/详情的品类列统一读这里，不再从 CrmHerbBaseSupplies 反推。
    /// </summary>
    public static class AttributeCodes
    {
        public const string HerbBaseProduct = "CRM_HERB_BASE_PRODUCT";
        public const string VendorProduct = "CRM_VENDOR_PRODUCT";
    }

    public static class Status
    {
        public const string Pending = "待联系";
        public const string Following = "跟进中";
        public const string Interested = "有意向";
        public const string Deal = "已成交";
        public const string Lost = "已流失";
    }

    public static class FollowResult
    {
        public const string Connected = "已接通";
        public const string Interested = "有意向";
    }
}
