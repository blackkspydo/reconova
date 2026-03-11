using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Identity;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        builder.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(u => u.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(u => u.TenantId).HasColumnName("tenant_id");
        builder.Property(u => u.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
        builder.Property(u => u.EmailVerificationToken).HasColumnName("email_verification_token").HasMaxLength(500);
        builder.Property(u => u.EmailVerificationTokenExpiresAt).HasColumnName("email_verification_token_expires_at");
        builder.Property(u => u.PasswordResetToken).HasColumnName("password_reset_token").HasMaxLength(500);
        builder.Property(u => u.PasswordResetTokenExpiresAt).HasColumnName("password_reset_token_expires_at");
        builder.Property(u => u.TwoFactorEnabled).HasColumnName("two_factor_enabled").HasDefaultValue(false);
        builder.Property(u => u.TwoFactorSecret).HasColumnName("two_factor_secret").HasMaxLength(500);
        builder.Property(u => u.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0);
        builder.Property(u => u.LockoutEndAt).HasColumnName("lockout_end_at");
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(u => u.LastLoginIp).HasColumnName("last_login_ip").HasMaxLength(45);
        builder.Property(u => u.LastPasswordChangeAt).HasColumnName("last_password_change_at");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(u => u.Email).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(u => u.TenantId);
        builder.HasIndex(u => u.Status);

        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(u => u.FullName);
    }
}
