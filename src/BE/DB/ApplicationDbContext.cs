using Microsoft.EntityFrameworkCore;
using DB.Models;

namespace DB;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Exchange> Exchanges { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<BookSwipe> BookSwipes { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Created = now;
                entry.Entity.Modified = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.Modified = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasOne(b => b.Owner)
                  .WithMany(u => u.Books)
                  .HasForeignKey(b => b.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Exchange>(entity =>
        {
            entity.HasOne(e => e.Book)
                  .WithMany()
                  .HasForeignKey(e => e.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Owner)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Receiver)
                  .WithMany()
                  .HasForeignKey(e => e.ReceiverId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasOne(c => c.User1)
                  .WithMany()
                  .HasForeignKey(c => c.UserId1)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.User2)
                  .WithMany()
                  .HasForeignKey(c => c.UserId2)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Exchange)
                  .WithMany()
                  .HasForeignKey(c => c.ExchangeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasOne(m => m.Chat)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(m => m.ChatId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.Sender)
                  .WithMany()
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<BookSwipe>(entity =>
        {
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Book)
                  .WithMany()
                  .HasForeignKey(s => s.BookId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => new { s.UserId, s.BookId })
                  .IsUnique();
        });
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}