using LawPortal.Domain.Common;

namespace LawPortal.Domain.Catalog;

/// <summary>
/// A lawyer's years-of-experience bracket, stored as a discrete range rather than a raw integer
/// so it can be displayed consistently (e.g. "1-3") without ad-hoc string formatting that risks
/// the reversed-range bidi bug the design system's mono/&lt;bdi&gt; convention exists to prevent.
/// </summary>
public class ExperienceRange : Entity<int>
{
    public int MinYears { get; set; }
    public int? MaxYears { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Western-numeral display form, e.g. "1-3" or "10+".</summary>
    public string Display => MaxYears.HasValue ? $"{MinYears}-{MaxYears}" : $"{MinYears}+";
}
