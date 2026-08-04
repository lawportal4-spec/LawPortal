using LawPortal.Domain.Catalog;
using LawPortal.Domain.Identity;
using LawPortal.Domain.Pricing;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only synthetic demo data — generated names, not real people, clearly gated to
/// the Development environment (see Program.cs). Tops up the approved-lawyer count to
/// <see cref="TargetCount"/> idempotently so restarts don't keep growing the table, and exists
/// solely so the directory's pagination and filters have production-shaped data to work
/// against (the plan's P2 exit criterion: "500+ lawyers so pagination is real").
/// </summary>
public static class DevLawyerSeeder
{
    public const int TargetCount = 550;
    private const int BatchSize = 100;

    private static readonly string[] MaleFirstNames =
        ["محمد", "عبدالله", "أحمد", "خالد", "سعود", "فيصل", "عبدالعزيز", "تركي", "بندر", "ناصر", "سلطان", "ماجد", "يوسف", "إبراهيم", "عمر"];
    private static readonly string[] FemaleFirstNames =
        ["سارة", "نورة", "فاطمة", "ريم", "لمى", "هند", "جواهر", "منيرة", "أمل", "لطيفة", "شذى", "رنا", "دانة", "العنود", "غادة"];
    private static readonly string[] FamilyNames =
        ["الشهري", "القحطاني", "الدوسري", "العتيبي", "الحربي", "الغامدي", "المطيري", "الزهراني", "السبيعي", "الرشيدي", "البقمي", "الشمري", "العنزي", "الجهني", "الخالدي"];
    private static readonly string[] BioTemplatesAr =
    [
        "محامٍ مرخص، عضو في هيئة المحامين، متخصص في تقديم استشارات دقيقة وحلول عملية للعملاء.",
        "محامية مرخصة تتمتع بخبرة واسعة في التمثيل القانوني ومتابعة القضايا حتى صدور الأحكام.",
        "محامٍ مرخص من وزارة العدل، يقدّم خدماته بحرص على وضوح الإجراءات وسرعة الاستجابة.",
    ];

    public static async Task SeedAsync(LawPortalDbContext db, CancellationToken cancellationToken = default)
    {
        // Total row count, not IsVerified count — this seeder's job is "550 demo profiles
        // exist," not "550 are currently verified." Counting only verified rows meant any
        // later action that un-verifies one (e.g. a lawyer renewing an expiring licence,
        // resetting IsVerified pending re-review) dropped the count by one and made this
        // seeder regenerate a "new" lawyer whose sequence-derived email collided with the
        // original — a real startup crash caught via RenewLicenseCommand's own verification test.
        var currentCount = await db.LawyerProfiles.CountAsync(cancellationToken);
        if (currentCount >= TargetCount) return;

        var toCreate = TargetCount - currentCount;

        var cityIds = await db.Cities.Select(c => c.Id).ToListAsync(cancellationToken);
        var regionIds = await db.Cities.Select(c => new { c.Id, c.RegionId }).ToDictionaryAsync(c => c.Id, c => c.RegionId, cancellationToken);
        var specialtyIds = await db.Specialties.Select(s => s.Id).ToListAsync(cancellationToken);
        var languageIds = await db.Languages.Select(l => l.Id).ToListAsync(cancellationToken);
        var arabicLanguageId = await db.Languages.Where(l => l.Code == "ar").Select(l => l.Id).FirstAsync(cancellationToken);
        var experienceRangeIds = await db.ExperienceRanges.OrderBy(e => e.SortOrder).Select(e => e.Id).ToListAsync(cancellationToken);

        var random = new Random(20260803); // fixed seed — deterministic across restarts/environments

        for (var offset = 0; offset < toCreate; offset += BatchSize)
        {
            var batch = new List<LawyerProfile>();
            var count = Math.Min(BatchSize, toCreate - offset);

            for (var i = 0; i < count; i++)
            {
                var sequence = currentCount + offset + i;
                var isFemale = random.Next(2) == 0;
                var firstName = isFemale ? Pick(random, FemaleFirstNames) : Pick(random, MaleFirstNames);
                var fatherName = isFemale ? Pick(random, MaleFirstNames) : Pick(random, MaleFirstNames);
                var familyName = Pick(random, FamilyNames);
                var fullNameAr = $"{firstName} {fatherName} {familyName}";
                var cityId = Pick(random, cityIds);

                var userId = Guid.NewGuid();
                var user = new User
                {
                    Id = userId,
                    Email = $"lawyer.dev.{sequence}@lawportal.sa",
                    IsEmailVerified = true,
                    PasswordHash = null, // synthetic accounts are never logged into — no usable password set
                    UserType = UserType.Lawyer,
                    Status = UserStatus.Active,
                };

                var profileId = Guid.NewGuid();
                var licenseIssued = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-random.Next(1, 8)));
                var hasRatings = random.Next(10) > 1; // ~80% have ratings; the rest show "newly joined"
                var isVatRegistered = random.Next(2) == 0;

                var profile = new LawyerProfile
                {
                    Id = profileId,
                    UserId = userId,
                    User = user,
                    FullName = fullNameAr,
                    Slug = $"lawyer-{sequence}-{profileId:N}"[..40],
                    BioAr = Pick(random, BioTemplatesAr),
                    Gender = isFemale ? Domain.Identity.Gender.Female : Domain.Identity.Gender.Male,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-random.Next(1, 3 * 365)), // varied "joined" dates for a realistic Newest sort
                    CityId = cityId,
                    RegionId = regionIds[cityId],
                    ExperienceRangeId = Pick(random, experienceRangeIds),
                    IsVerified = true,
                    IsVatRegistered = isVatRegistered,
                    VatNumber = isVatRegistered ? MakeVatNumber(sequence) : null,
                    AvgRating = hasRatings ? Math.Round((decimal)(random.NextDouble() * 1.5 + 3.5), 1) : null,
                    RatingCount = hasRatings ? random.Next(1, 120) : 0,
                    CompletedRequestCount = hasRatings ? random.Next(1, 200) : 0,
                    License = new LawyerLicense
                    {
                        Id = Guid.NewGuid(),
                        LawyerProfileId = profileId,
                        LicenseNumber = (100000 + sequence).ToString(),
                        IssueDate = licenseIssued,
                        ExpiryDate = licenseIssued.AddYears(5),
                        VerificationStatus = LicenseVerificationStatus.Approved,
                        VerifiedAtUtc = DateTime.UtcNow,
                    },
                    Pricing = new LawyerPricing
                    {
                        Id = Guid.NewGuid(),
                        LawyerProfileId = profileId,
                        WrittenPrice = RoundToHalf(random.Next(100, 300)),
                        Price15 = RoundToHalf(random.Next(120, 250)),
                        Price30 = RoundToHalf(random.Next(200, 450)),
                        Price45 = RoundToHalf(random.Next(300, 950)),
                    },
                };

                var specialtyCount = random.Next(1, 4);
                foreach (var specialtyId in Shuffle(random, specialtyIds).Take(specialtyCount))
                {
                    profile.LawyerSpecialties.Add(new LawyerSpecialty { LawyerProfileId = profileId, SpecialtyId = specialtyId });
                }

                profile.LawyerLanguages.Add(new LawyerLanguage { LawyerProfileId = profileId, LanguageId = arabicLanguageId });
                if (random.Next(3) == 0)
                {
                    var secondLanguage = Pick(random, languageIds);
                    if (secondLanguage != arabicLanguageId)
                        profile.LawyerLanguages.Add(new LawyerLanguage { LawyerProfileId = profileId, LanguageId = secondLanguage });
                }

                batch.Add(profile);
            }

            await db.LawyerProfiles.AddRangeAsync(batch, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static T Pick<T>(Random random, IReadOnlyList<T> items) => items[random.Next(items.Count)];

    private static IEnumerable<T> Shuffle<T>(Random random, IReadOnlyList<T> items) =>
        items.OrderBy(_ => random.Next());

    private static decimal RoundToHalf(int value) => Math.Round(value / 5m) * 5m;

    /// <summary>A plausible-looking 15-digit Saudi VAT registration number — real ones both
    /// start and end with "3". Deterministic from the seed sequence, matching this seeder's
    /// existing fixed-seed reproducibility.</summary>
    private static string MakeVatNumber(int sequence) => $"3{sequence:D13}3";
}
