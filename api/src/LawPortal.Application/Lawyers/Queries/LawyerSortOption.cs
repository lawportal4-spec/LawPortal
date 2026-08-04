namespace LawPortal.Application.Lawyers.Queries;

/// <summary>The 6 sort dimensions from the documented lawyer-selection screen (الترتيب العام،
/// المدينة، التقييمات، المؤهلات، سعر الاستشارة, plus a most-booked dimension distinct from
/// "newest" for الأنسب).</summary>
public enum LawyerSortOption
{
    Newest = 1,
    MostRequested = 2,
    Rating = 3,
    City = 4,
    Experience = 5,
    Price = 6,
}
