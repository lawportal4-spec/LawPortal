using LawPortal.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Infrastructure.Persistence.Seeding;

/// <summary>Seeds the 5 service categories and their ~18 specific services, each tagged with
/// its pricing model, plus the priced variants notarization services need. Our own naming
/// throughout. Only "إنشاء وكالة" prices (750/400 SAR) come from the source documentation —
/// every other notarization price below is an explicit placeholder, since the source never
/// documented them; replace with real figures before launch. Idempotent.</summary>
public static class ServiceCatalogSeeder
{
    public static async Task SeedAsync(LawPortalDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.ServiceCategories.AnyAsync(cancellationToken)) return;

        var consultations = new ServiceCategory
        {
            NameAr = "الاستشارات القانونية", NameEn = "Legal Consultations", Slug = "consultations", IconKey = "scale", SortOrder = 1,
            Services =
            [
                new ServiceCatalogItem { NameAr = "استشارة فورية", NameEn = "Instant Consultation", Slug = "instant-consultation", SortOrder = 1, PricingModel = ServicePricingModel.PerLawyerFixed, DescriptionAr = "تواصل فورًا مع محامٍ عبر مكالمة صوتية أو مرئية، دون الحاجة لحجز موعد مسبق.", DescriptionEn = "Reach a lawyer immediately by voice or video call, no appointment needed." },
                new ServiceCatalogItem { NameAr = "استشارة كتابية", NameEn = "Written Consultation", Slug = "written-consultation", SortOrder = 2, PricingModel = ServicePricingModel.PerLawyerFixed, DescriptionAr = "تواصل كتابيًا مع محامٍ عبر دردشة نصية ومرفقات.", DescriptionEn = "Message a lawyer in a text thread with attachments." },
                new ServiceCatalogItem { NameAr = "استشارة مجدولة", NameEn = "Scheduled Consultation", Slug = "scheduled-consultation", SortOrder = 3, PricingModel = ServicePricingModel.PerLawyerFixed, DescriptionAr = "احجز استشارة مع محامٍ من اختيارك في الوقت الذي يناسبك.", DescriptionEn = "Book a voice/video consultation at a time that suits you." },
            ],
        };

        var judiciary = new ServiceCategory
        {
            NameAr = "القضاء والتنفيذ", NameEn = "Judiciary & Execution", Slug = "judiciary-execution", IconKey = "gavel", SortOrder = 2,
            Services =
            [
                new ServiceCatalogItem { NameAr = "كتابات قانونية", NameEn = "Legal Writing", Slug = "legal-writing", SortOrder = 1, PricingModel = ServicePricingModel.CompetitiveBidding },
                new ServiceCatalogItem { NameAr = "الترافع والتوكيل", NameEn = "Litigation & Representation", Slug = "litigation-representation", SortOrder = 2, PricingModel = ServicePricingModel.CompetitiveBidding },
                new ServiceCatalogItem { NameAr = "حضور جلسة", NameEn = "Hearing Attendance", Slug = "hearing-attendance", SortOrder = 3, PricingModel = ServicePricingModel.CompetitiveBidding },
                new ServiceCatalogItem { NameAr = "دراسة قضية", NameEn = "Case Study", Slug = "case-study", SortOrder = 4, PricingModel = ServicePricingModel.CompetitiveBidding },
                new ServiceCatalogItem { NameAr = "طلبات التنفيذ", NameEn = "Execution Requests", Slug = "execution-requests", SortOrder = 5, PricingModel = ServicePricingModel.CompetitiveBidding },
            ],
        };

        var notarization = new ServiceCategory
        {
            NameAr = "خدمات التوثيق", NameEn = "Notarization Services", Slug = "notarization", IconKey = "document", SortOrder = 3,
            Services =
            [
                new ServiceCatalogItem
                {
                    NameAr = "إنشاء وكالة", NameEn = "Create Power of Attorney", Slug = "create-poa", SortOrder = 1,
                    PricingModel = ServicePricingModel.PredefinedCatalog, RequiresSpecialty = false,
                    Variants =
                    [
                        new ServiceVariant { NameAr = "وكالة شركات", NameEn = "Corporate PoA", BasePrice = 750, RequiresQuantity = true, IncludedQuantity = 2, ExtraUnitPrice = 0, QuantityLabelAr = "عدد أطراف الوكالة", QuantityLabelEn = "Number of parties", MinQuantity = 2, MaxQuantity = 10, SortOrder = 1 },
                        new ServiceVariant { NameAr = "وكالة فردية", NameEn = "Individual PoA", BasePrice = 400, RequiresQuantity = true, IncludedQuantity = 2, ExtraUnitPrice = 0, QuantityLabelAr = "عدد أطراف الوكالة", QuantityLabelEn = "Number of parties", MinQuantity = 2, MaxQuantity = 10, SortOrder = 2 },
                    ],
                },
                new ServiceCatalogItem
                {
                    NameAr = "فسخ وكالة", NameEn = "Cancel Power of Attorney", Slug = "cancel-poa", SortOrder = 2,
                    PricingModel = ServicePricingModel.PredefinedCatalog, RequiresSpecialty = false,
                    Variants =
                    [
                        new ServiceVariant { NameAr = "وكالة شركات", NameEn = "Corporate PoA", BasePrice = 700, SortOrder = 1 },
                        new ServiceVariant { NameAr = "وكالة فردية", NameEn = "Individual PoA", BasePrice = 350, SortOrder = 2 },
                    ],
                },
                new ServiceCatalogItem
                {
                    NameAr = "عقد تأسيس شركة", NameEn = "Company Incorporation", Slug = "company-incorporation", SortOrder = 3,
                    PricingModel = ServicePricingModel.PredefinedCatalog, RequiresSpecialty = false,
                    Variants = [new ServiceVariant { NameAr = "تأسيس شركة", NameEn = "Incorporation", BasePrice = 1200, SortOrder = 1 }],
                },
                new ServiceCatalogItem
                {
                    NameAr = "إفراغ عقاري", NameEn = "Property Transfer", Slug = "property-transfer", SortOrder = 4,
                    PricingModel = ServicePricingModel.PredefinedCatalog, RequiresSpecialty = false,
                    Variants =
                    [
                        new ServiceVariant { NameAr = "سكني", NameEn = "Residential", BasePrice = 900, SortOrder = 1 },
                        new ServiceVariant { NameAr = "تجاري", NameEn = "Commercial", BasePrice = 1500, SortOrder = 2 },
                    ],
                },
                new ServiceCatalogItem
                {
                    NameAr = "الرهون العقارية", NameEn = "Property Mortgage", Slug = "property-mortgage", SortOrder = 5,
                    PricingModel = ServicePricingModel.PredefinedCatalog, RequiresSpecialty = false,
                    Variants =
                    [
                        new ServiceVariant { NameAr = "سكني", NameEn = "Residential", BasePrice = 800, SortOrder = 1 },
                        new ServiceVariant { NameAr = "تجاري", NameEn = "Commercial", BasePrice = 1400, SortOrder = 2 },
                    ],
                },
                new ServiceCatalogItem
                {
                    NameAr = "الإقرارات المالية", NameEn = "Financial Declarations", Slug = "financial-declarations", SortOrder = 6,
                    PricingModel = ServicePricingModel.PredefinedCatalog, RequiresSpecialty = false,
                    Variants =
                    [
                        new ServiceVariant { NameAr = "إقرار", NameEn = "Declaration", BasePrice = 300, SortOrder = 1 },
                        new ServiceVariant { NameAr = "تسديد", NameEn = "Settlement", BasePrice = 300, SortOrder = 2 },
                    ],
                },
            ],
        };

        var business = new ServiceCategory
        {
            NameAr = "خدمات الأعمال", NameEn = "Business Services", Slug = "business", IconKey = "briefcase", SortOrder = 4,
            Services =
            [
                new ServiceCatalogItem { NameAr = "العقود والاتفاقيات", NameEn = "Contracts & Agreements", Slug = "contracts-agreements", SortOrder = 1, PricingModel = ServicePricingModel.CompetitiveBidding },
                new ServiceCatalogItem { NameAr = "تسجيل العلامات التجارية", NameEn = "Trademark Registration", Slug = "trademark-registration", SortOrder = 2, PricingModel = ServicePricingModel.DetailsOnly, RequiresSpecialty = false },
            ],
        };

        var other = new ServiceCategory
        {
            NameAr = "خدمات أخرى", NameEn = "Other Services", Slug = "other", IconKey = "folder", SortOrder = 5,
            Services =
            [
                new ServiceCatalogItem { NameAr = "مراجعة الجهات والدوائر الحكومية", NameEn = "Government Entity Follow-up", Slug = "government-follow-up", SortOrder = 1, PricingModel = ServicePricingModel.CompetitiveBidding },
                new ServiceCatalogItem { NameAr = "خدمات أخرى", NameEn = "Custom Request", Slug = "custom-request", SortOrder = 2, PricingModel = ServicePricingModel.CompetitiveBidding },
            ],
        };

        await db.ServiceCategories.AddRangeAsync([consultations, judiciary, notarization, business, other], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
