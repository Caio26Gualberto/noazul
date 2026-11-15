using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NoAzul.Application.Abstractions.Authentication;
using NoAzul.Domain.Entities;
using NoAzul.Infra.Data.Identity;

namespace NoAzul.Infra.Data.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public const string CaseInsensitiveCollation = "case_insensitive";

        private readonly ICurrentUser _currentUserContext;

        public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUserContext) : base(options)
        {
            _currentUserContext = currentUserContext;
        }

        public new DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<AuthorizationAuditLog> AuthorizationAuditLogs => Set<AuthorizationAuditLog>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<BackgroundJobRun> BackgroundJobRuns => Set<BackgroundJobRun>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<BankConnection> BankConnections => Set<BankConnection>();
        public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var isPostgres = Database.IsNpgsql();
            var softDeleteFilter = DatabaseProvider.SoftDeleteIndexFilter(this);
            var jsonType = DatabaseProvider.JsonColumnType(this);

            if (isPostgres)
                builder.HasCollation(
                    CaseInsensitiveCollation,
                    locale: "und-u-ks-level2",
                    provider: "icu",
                    deterministic: false);

            builder.Entity<ApplicationUser>().ToTable("UsersIdentity");
            builder.Entity<ApplicationRole>(b =>
            {
                b.ToTable("Roles");
                b.Property(r => r.Description).HasMaxLength(256);
            });
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            builder.Entity<User>(b =>
            {
                b.ToTable("Users");
                b.HasKey(u => u.Id);

                b.Property(u => u.Name).HasMaxLength(256);

                var email = b.Property(u => u.Email).HasMaxLength(256);
                if (isPostgres)
                    email.UseCollation(CaseInsensitiveCollation);

                b.HasIndex(u => u.Email).IsUnique();
            });

            builder.Entity<ApplicationUser>(b =>
            {
                b.HasOne(a => a.User)
                    .WithOne()
                    .HasForeignKey<ApplicationUser>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(a => a.UserName).IsUnique();

                if (isPostgres)
                    b.Property(a => a.Email).UseCollation(CaseInsensitiveCollation);
            });

            builder.Entity<RefreshToken>(b =>
            {
                b.ToTable("RefreshTokens");
                b.HasKey(r => r.Id);

                b.Property(r => r.TokenHash).HasMaxLength(64);
                b.HasIndex(r => r.TokenHash).IsUnique();

                b.HasOne(r => r.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(r => r.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Item>(b =>
            {
                b.ToTable("Items");
                b.HasKey(i => i.Id);

                b.Property(i => i.Name).HasMaxLength(120).IsRequired();
                b.Property(i => i.Description).HasMaxLength(1000);

                b.HasOne(i => i.User)
                    .WithMany()
                    .HasForeignKey(i => i.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(i => new { i.UserId, i.Name })
                    .IsUnique()
                    .HasFilter(softDeleteFilter);

                b.HasIndex(i => new { i.Status, i.ExpiresAt });
                b.HasIndex(i => new { i.UserId, i.CreatedAt });
            });

            builder.Entity<BankConnection>(b =>
            {
                b.ToTable("BankConnections");
                b.HasKey(c => c.Id);

                b.Property(c => c.PluggyItemId).HasMaxLength(64).IsRequired();
                b.Property(c => c.InstitutionName).HasMaxLength(120).IsRequired();
                b.Property(c => c.BankCode).HasMaxLength(3);
                b.Property(c => c.InstitutionImageUrl).HasMaxLength(500);

                b.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(c => c.PluggyItemId)
                    .IsUnique()
                    .HasFilter(softDeleteFilter);

                b.HasIndex(c => c.UserId);
            });

            builder.Entity<FinancialAccount>(b =>
            {
                b.ToTable("FinancialAccounts");
                b.HasKey(a => a.Id);

                b.Property(a => a.PluggyAccountId).HasMaxLength(64).IsRequired();
                b.Property(a => a.Name).HasMaxLength(120).IsRequired();
                b.Property(a => a.Number).HasMaxLength(64).IsRequired();
                b.Property(a => a.CurrencyCode).HasMaxLength(3).IsRequired();
                b.Property(a => a.CardBrand).HasMaxLength(32);
                b.Property(a => a.Balance).HasPrecision(18, 2);
                b.Property(a => a.CreditLimit).HasPrecision(18, 2);
                b.Property(a => a.AvailableCreditLimit).HasPrecision(18, 2);
                b.Property(a => a.PreviousBillAmount).HasPrecision(18, 2);

                b.HasOne(a => a.BankConnection)
                    .WithMany(c => c.Accounts)
                    .HasForeignKey(a => a.BankConnectionId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(a => a.PluggyAccountId)
                    .IsUnique()
                    .HasFilter(softDeleteFilter);

                b.HasIndex(a => a.BankConnectionId);
            });

            builder.Entity<AuthorizationAuditLog>(b =>
            {
                b.ToTable("AuthorizationAuditLogs");
                b.HasKey(a => a.Id);

                b.Property(a => a.Action).HasMaxLength(64);
                b.Property(a => a.TargetType).HasMaxLength(16);
                b.Property(a => a.TargetId).HasMaxLength(64);
                b.Property(a => a.TargetName).HasMaxLength(256);
                b.Property(a => a.ActorEmail).HasMaxLength(256);
                b.Property(a => a.IpAddress).HasMaxLength(64);
                b.Property(a => a.OldValueJson).HasColumnType(jsonType);
                b.Property(a => a.NewValueJson).HasColumnType(jsonType);

                b.HasIndex(a => a.CreatedAt);
                b.HasIndex(a => a.Action);
            });

            builder.Entity<BackgroundJobRun>(b =>
            {
                b.ToTable("BackgroundJobRuns");
                b.HasKey(j => j.Id);

                b.Property(j => j.RequestedByEmail).HasMaxLength(256);
                b.Property(j => j.DeduplicationKey).HasMaxLength(200);
                b.Property(j => j.ErrorMessage).HasMaxLength(2000);
                b.Property(j => j.ResultStorageKey).HasMaxLength(400);
                b.Property(j => j.HangfireJobId).HasMaxLength(64);
                b.Property(j => j.PayloadJson).HasColumnType(jsonType);
                b.Property(j => j.ResultJson).HasColumnType(jsonType);

                b.HasOne(j => j.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(j => j.RequestedByUserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(j => new { j.JobType, j.CreatedAt });
                b.HasIndex(j => new { j.Status, j.CreatedAt });
                b.HasIndex(j => j.DeduplicationKey)
                    .IsUnique()
                    .HasFilter(DatabaseProvider.NotNullIndexFilter(this, nameof(BackgroundJobRun.DeduplicationKey)));
            });

            builder.Entity<AuditLog>(b =>
            {
                b.ToTable("AuditLogs");
                b.HasKey(a => a.Id);

                b.Property(a => a.EntityType).HasMaxLength(128).IsRequired();
                b.Property(a => a.EntityId).HasMaxLength(64).IsRequired();
                b.Property(a => a.EntityDescription).HasMaxLength(256);
                b.Property(a => a.ActorEmail).HasMaxLength(256);
                b.Property(a => a.ActorName).HasMaxLength(256);
                b.Property(a => a.IpAddress).HasMaxLength(64);
                b.Property(a => a.ChangesJson).HasColumnType(jsonType);

                b.HasIndex(a => new { a.OccurredAt, a.Id }).IsDescending();
                b.HasIndex(a => new { a.EntityType, a.EntityId });
                b.HasIndex(a => new { a.ActorUserId, a.OccurredAt });
                b.HasIndex(a => new { a.Action, a.OccurredAt });
            });
        }

        public override int SaveChanges()
        {
            ApplyAuditing();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditing();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditing()
        {
            var now = DateTime.UtcNow;
            var userId = _currentUserContext.UserIdOrNull;

            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        entry.Entity.CreatedBy = userId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = userId;

                        if (entry.Entity.IsDeleted && entry.Entity.DeletedAt is null)
                        {
                            entry.Entity.DeletedAt = now;
                            entry.Entity.DeletedBy = userId;
                        }
                        break;
                }
            }
        }
    }
}
