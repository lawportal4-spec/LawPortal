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

        await SeedRegionsAsync(db, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The kingdom's 13 administrative regions with their main cities. Unlike the
    /// all-or-nothing seeds above this one tops up — an existing database (production started
    /// with only five regions) gains whatever is missing, matched by Arabic name, and nothing
    /// already there is renamed or removed.</summary>
    private static readonly (string Ar, string En, (string Ar, string En)[] Cities)[] SaudiRegions =
    [
        ("الرياض", "Riyadh", [("الرياض", "Riyadh"), ("الخرج", "Al Kharj"), ("الدوادمي", "Ad Dawadimi"), ("المجمعة", "Al Majmaah"), ("القويعية", "Al Quwayiyah"), ("وادي الدواسر", "Wadi Ad Dawasir"), ("الزلفي", "Az Zulfi"), ("شقراء", "Shaqra"), ("عفيف", "Afif"), ("الأفلاج", "Al Aflaj")]),
        ("مكة المكرمة", "Makkah", [("مكة المكرمة", "Makkah"), ("جدة", "Jeddah"), ("الطائف", "Taif"), ("القنفذة", "Al Qunfudhah"), ("رابغ", "Rabigh"), ("الليث", "Al Lith"), ("الجموم", "Al Jumum"), ("الخرمة", "Al Khurmah")]),
        ("المنطقة الشرقية", "Eastern Province", [("الدمام", "Dammam"), ("الخبر", "Khobar"), ("الظهران", "Dhahran"), ("الأحساء", "Al Ahsa"), ("حفر الباطن", "Hafar Al Batin"), ("الجبيل", "Jubail"), ("القطيف", "Qatif"), ("الخفجي", "Khafji"), ("رأس تنورة", "Ras Tanura"), ("بقيق", "Abqaiq")]),
        ("القصيم", "Qassim", [("بريدة", "Buraidah"), ("عنيزة", "Unaizah"), ("الرس", "Ar Rass"), ("البكيرية", "Al Bukayriyah"), ("المذنب", "Al Mithnab"), ("البدائع", "Al Badai")]),
        ("المدينة المنورة", "Madinah", [("المدينة المنورة", "Madinah"), ("ينبع", "Yanbu"), ("العلا", "AlUla"), ("المهد", "Al Mahd"), ("بدر", "Badr"), ("خيبر", "Khaybar"), ("الحناكية", "Al Hinakiyah")]),
        ("عسير", "Asir", [("أبها", "Abha"), ("خميس مشيط", "Khamis Mushait"), ("بيشة", "Bisha"), ("محايل عسير", "Muhayil Asir"), ("النماص", "An Namas"), ("سراة عبيدة", "Sarat Abidah"), ("ظهران الجنوب", "Dhahran Al Janub")]),
        ("تبوك", "Tabuk", [("تبوك", "Tabuk"), ("الوجه", "Al Wajh"), ("ضباء", "Duba"), ("تيماء", "Tayma"), ("أملج", "Umluj"), ("حقل", "Haql")]),
        ("حائل", "Hail", [("حائل", "Hail"), ("بقعاء", "Baqaa"), ("الغزالة", "Al Ghazalah"), ("الشنان", "Ash Shinan")]),
        ("الحدود الشمالية", "Northern Borders", [("عرعر", "Arar"), ("رفحاء", "Rafha"), ("طريف", "Turaif"), ("العويقيلة", "Al Uwayqilah")]),
        ("جازان", "Jazan", [("جازان", "Jazan"), ("صبيا", "Sabya"), ("أبو عريش", "Abu Arish"), ("صامطة", "Samtah"), ("بيش", "Baish"), ("الدرب", "Ad Darb"), ("فرسان", "Farasan")]),
        ("نجران", "Najran", [("نجران", "Najran"), ("شرورة", "Sharurah"), ("حبونا", "Hubuna")]),
        ("الباحة", "Al Bahah", [("الباحة", "Al Bahah"), ("بلجرشي", "Baljurashi"), ("المندق", "Al Mandaq"), ("المخواة", "Al Makhwah"), ("العقيق", "Al Aqiq")]),
        ("الجوف", "Al Jouf", [("سكاكا", "Sakaka"), ("القريات", "Al Qurayyat"), ("دومة الجندل", "Dumat Al Jandal"), ("طبرجل", "Tabarjal")]),
    ];

    private static async Task SeedRegionsAsync(LawPortalDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Regions.Include(r => r.Cities).ToListAsync(cancellationToken);
        foreach (var (ar, en, cities) in SaudiRegions)
        {
            var region = existing.FirstOrDefault(r => r.NameAr == ar);
            if (region is null)
            {
                region = new Region { NameAr = ar, NameEn = en };
                db.Regions.Add(region);
            }

            foreach (var (cityAr, cityEn) in cities)
            {
                if (region.Cities.All(c => c.NameAr != cityAr))
                    region.Cities.Add(new City { NameAr = cityAr, NameEn = cityEn });
            }
        }
    }
}
