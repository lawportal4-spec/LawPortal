using LawPortal.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawPortal.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.PhoneE164).HasMaxLength(20);
        builder.HasIndex(u => u.PhoneE164).IsUnique();
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PreferredLocale).HasMaxLength(5);

        builder.HasOne(u => u.ClientProfile).WithOne(p => p.User!)
            .HasForeignKey<ClientProfile>(p => p.UserId);
        builder.HasOne(u => u.LawyerProfile).WithOne(p => p.User!)
            .HasForeignKey<LawyerProfile>(p => p.UserId);
        builder.HasOne(u => u.AdminProfile).WithOne(p => p.User!)
            .HasForeignKey<AdminProfile>(p => p.UserId);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}

public class ClientProfileConfiguration : IEntityTypeConfiguration<ClientProfile>
{
    public void Configure(EntityTypeBuilder<ClientProfile> builder)
    {
        builder.ToTable("client_profiles");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).HasMaxLength(200);
        builder.Property(c => c.CompanyName).HasMaxLength(200);
        builder.Property(c => c.CrNumber).HasMaxLength(50);
    }
}

public class LawyerProfileConfiguration : IEntityTypeConfiguration<LawyerProfile>
{
    public void Configure(EntityTypeBuilder<LawyerProfile> builder)
    {
        builder.ToTable("lawyer_profiles");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.FullName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Slug).HasMaxLength(220).IsRequired();
        builder.HasIndex(l => l.Slug).IsUnique();
        builder.Property(l => l.VatNumber).HasMaxLength(50);
        builder.Property(l => l.CountryCode).HasMaxLength(2).HasDefaultValue("SA").IsRequired();
        builder.Property(l => l.AvgRating).HasColumnType("decimal(3,2)");
        builder.Property(l => l.AcceptingNewRequests).HasDefaultValue(true);

        builder.HasOne(l => l.License).WithOne(lic => lic.LawyerProfile!)
            .HasForeignKey<LawyerLicense>(lic => lic.LawyerProfileId);

        builder.HasIndex(l => new { l.IsVerified, l.CityId });
        builder.HasIndex(l => new { l.IsVerified, l.AvgRating });
        builder.HasIndex(l => new { l.IsVerified, l.Gender });
    }
}

public class LawyerLicenseConfiguration : IEntityTypeConfiguration<LawyerLicense>
{
    public void Configure(EntityTypeBuilder<LawyerLicense> builder)
    {
        builder.ToTable("lawyer_licenses");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.LicenseNumber).HasMaxLength(50).IsRequired();
        builder.Property(l => l.LicenseType).HasDefaultValue(LawyerLicenseType.Licensed);
        builder.Property(l => l.DocumentStorageKey).HasMaxLength(500);
        builder.Property(l => l.DocumentFileName).HasMaxLength(255);
        builder.Property(l => l.DocumentContentType).HasMaxLength(100);
        builder.Property(l => l.CorrectionNote).HasMaxLength(1000);
        builder.HasIndex(l => l.LicenseNumber).IsUnique();
    }
}

public class AdminProfileConfiguration : IEntityTypeConfiguration<AdminProfile>
{
    public void Configure(EntityTypeBuilder<AdminProfile> builder)
    {
        builder.ToTable("admin_profiles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.DisplayName).HasMaxLength(200).IsRequired();
    }
}
