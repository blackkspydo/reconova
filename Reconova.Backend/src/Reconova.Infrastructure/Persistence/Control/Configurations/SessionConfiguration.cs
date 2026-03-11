using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Identity;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.IpAddress).HasColumnName("ip_address").HasMaxLength(45).IsRequired();
        builder.Property(s => s.UserAgent).HasColumnName("user_agent").HasMaxLength(500).IsRequired();
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(s => s.LastActiveAt).HasColumnName("last_active_at");
        builder.Property(s => s.IsRevoked).HasColumnName("is_revoked").HasDefaultValue(false);
        builder.Property(s => s.RevokedAt).HasColumnName("revoked_at");
        builder.Property(s => s.IsImpersonation).HasColumnName("is_impersonation").HasDefaultValue(false);
        builder.Property(s => s.ImpersonatedByUserId).HasColumnName("impersonated_by_user_id");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ExpiresAt);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
