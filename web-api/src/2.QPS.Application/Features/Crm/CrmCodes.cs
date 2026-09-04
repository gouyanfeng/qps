namespace QPS.Application.Features.Crm;

public static class CrmCodes
{
    public const string HerbBaseEntityType = "CRM_HERB_BASE";
    public const string HerbBaseSubjectEntityType = "CRM_HERB_BASE_SUBJECT";
    public const string VendorEntityType = "CRM_VENDOR";
    public const string HerbProductDictionaryCode = "CRM_HERB_PRODUCT";

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
