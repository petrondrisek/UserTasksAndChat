using Microsoft.EntityFrameworkCore;
using UserTasksAndChat.Events;
using UserTasksAndChat.Models;

namespace UserTasksAndChat.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly DomainEventDispatcher _dispatcher;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            DomainEventDispatcher dispatcher
        ) : base(options)
        {
            _dispatcher = dispatcher;
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<Mission> Missions { get; set; } = null!;
        public DbSet<MissionChat> MissionChats { get; set; } = null!;
        public DbSet<MissionLastVisit> MissionLastVisits { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // First user creation
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = Guid.Parse("7a91c19e-640f-4829-b48d-aed6f5d43671"),
                Username = "admin",
                Email = "admin@admin.cz",
                CreatedAt = DateTime.Parse("2023-10-01T00:00:00Z"),
                UpdatedAt = DateTime.Parse("2023-10-01T00:00:00Z"),
                Permissions = new List<UserPermissions> { UserPermissions.User, UserPermissions.ManageUsers, UserPermissions.ManageMissions},
            });

            // User can be null
            modelBuilder.Entity<Mission>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .IsRequired(false);
        }

        // Event hooks
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var domainEvents = ChangeTracker.Entries<Entity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            await _dispatcher.DispatchAsync(domainEvents, cancellationToken);

            foreach (var entry in ChangeTracker.Entries<Entity>())
                entry.Entity.ClearDomainEvents();

            var result = await base.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}
