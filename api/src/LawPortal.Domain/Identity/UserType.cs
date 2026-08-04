namespace LawPortal.Domain.Identity;

public enum UserType
{
    Client = 1,
    Lawyer = 2,
    Admin = 3,
}

public enum UserStatus
{
    PendingVerification = 1,
    Active = 2,
    Suspended = 3,
    Banned = 4,
    Deleted = 5,
}

public enum AccountKind
{
    Individual = 1,
    Company = 2,
}

public enum LicenseVerificationStatus
{
    PendingReview = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
}

public enum OtpPurpose
{
    Login = 1,
}
