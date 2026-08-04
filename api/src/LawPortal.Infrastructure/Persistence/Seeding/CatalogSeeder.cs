using LawPortal.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the reference data every environment needs: the 13 shared legal specialties (with the
/// one sub-specialty branch documented for "Rights &amp; General"), Saudi regions/cities, UI
/// languages, and lawyer experience brackets. Idempotent — safe to run on every startup.
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(LawPortalDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Specialties.AnyAsync(cancellationToken))
        {
            var specialties = new List<Specialty>
            {
                new() { NameAr = "أحوال الشخصية", NameEn = "Personal Status", Slug = "personal-status", SortOrder = 1 },
                new() { NameAr = "إرث وتركات", NameEn = "Inheritance & Estates", Slug = "inheritance-estates", SortOrder = 2 },
                new() { NameAr = "جنائية", NameEn = "Criminal", Slug = "criminal", SortOrder = 3 },
                new()
                {
                    NameAr = "حقوق وعامة", NameEn = "Rights & General", Slug = "rights-general", SortOrder = 4,
                    SubSpecialties =
                    [
                        new SubSpecialty { NameAr = "قرض", NameEn = "Loan" },
                        new SubSpecialty { NameAr = "بيع وشراء", NameEn = "Sale & Purchase" },
                        new SubSpecialty { NameAr = "مخالفات الوكيل الشرعي", NameEn = "Legal Agent Violations" },
                        new SubSpecialty { NameAr = "إثبات ملكية", NameEn = "Proof of Ownership" },
                        new SubSpecialty { NameAr = "مطالبات مالية", NameEn = "Financial Claims" },
                        new SubSpecialty { NameAr = "الدعاوى المستعجلة", NameEn = "Urgent Claims" },
                    ],
                },
                new() { NameAr = "عمالية", NameEn = "Labour", Slug = "labour", SortOrder = 5 },
                new() { NameAr = "تجارية", NameEn = "Commercial", Slug = "commercial", SortOrder = 6 },
                new() { NameAr = "الملكية الفكرية", NameEn = "Intellectual Property", Slug = "intellectual-property", SortOrder = 7 },
                new() { NameAr = "قضايا إدارية", NameEn = "Administrative", Slug = "administrative", SortOrder = 8 },
                new() { NameAr = "قضايا التنفيذ", NameEn = "Execution Cases", Slug = "execution-cases", SortOrder = 9 },
                new() { NameAr = "اللجان شبه القضائية", NameEn = "Quasi-Judicial Committees", Slug = "quasi-judicial-committees", SortOrder = 10 },
                new() { NameAr = "الأخطاء الطبية", NameEn = "Medical Malpractice", Slug = "medical-malpractice", SortOrder = 11 },
                new() { NameAr = "مرورية", NameEn = "Traffic", Slug = "traffic", SortOrder = 12 },
                new() { NameAr = "عقارية", NameEn = "Real Estate", Slug = "real-estate", SortOrder = 13 },
            };

            await db.Specialties.AddRangeAsync(specialties, cancellationToken);
        }

        if (!await db.Languages.AnyAsync(cancellationToken))
        {
            await db.Languages.AddRangeAsync(
            [
                new Language { Code = "ar", NameAr = "العربية", NameEn = "Arabic" },
                new Language { Code = "en", NameAr = "الإنجليزية", NameEn = "English" },
                new Language { Code = "ur", NameAr = "الأردية", NameEn = "Urdu" },
                new Language { Code = "fr", NameAr = "الفرنسية", NameEn = "French" },
            ], cancellationToken);
        }

        if (!await db.ExperienceRanges.AnyAsync(cancellationToken))
        {
            await db.ExperienceRanges.AddRangeAsync(
            [
                new ExperienceRange { MinYears = 0, MaxYears = 2, SortOrder = 1 },
                new ExperienceRange { MinYears = 1, MaxYears = 3, SortOrder = 2 },
                new ExperienceRange { MinYears = 3, MaxYears = 5, SortOrder = 3 },
                new ExperienceRange { MinYears = 5, MaxYears = 10, SortOrder = 4 },
                new ExperienceRange { MinYears = 10, MaxYears = null, SortOrder = 5 },
            ], cancellationToken);
        }

        if (!await db.Regions.AnyAsync(cancellationToken))
        {
            var riyadh = new Region { NameAr = "الرياض", NameEn = "Riyadh" };
            var makkah = new Region { NameAr = "مكة المكرمة", NameEn = "Makkah" };
            var eastern = new Region { NameAr = "المنطقة الشرقية", NameEn = "Eastern Province" };
            var qassim = new Region { NameAr = "القصيم", NameEn = "Qassim" };
            var madinah = new Region { NameAr = "المدينة المنورة", NameEn = "Madinah" };

            riyadh.Cities.Add(new City { NameAr = "الرياض", NameEn = "Riyadh" });
            makkah.Cities.Add(new City { NameAr = "مكة المكرمة", NameEn = "Makkah" });
            makkah.Cities.Add(new City { NameAr = "جدة", NameEn = "Jeddah" });
            makkah.Cities.Add(new City { NameAr = "الطائف", NameEn = "Taif" });
            eastern.Cities.Add(new City { NameAr = "الدمام", NameEn = "Dammam" });
            eastern.Cities.Add(new City { NameAr = "الخبر", NameEn = "Khobar" });
            qassim.Cities.Add(new City { NameAr = "بريدة", NameEn = "Buraidah" });
            madinah.Cities.Add(new City { NameAr = "المدينة المنورة", NameEn = "Madinah" });

            await db.Regions.AddRangeAsync([riyadh, makkah, eastern, qassim, madinah], cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
