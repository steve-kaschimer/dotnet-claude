using Microsoft.EntityFrameworkCore;

namespace DotnetClaude.Core.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessageEntity> Messages => Set<ChatMessageEntity>();
    public DbSet<QueryMetric> QueryMetrics => Set<QueryMetric>();
    public DbSet<ToolInvocation> ToolInvocations => Set<ToolInvocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>()
            .HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .HasMany(c => c.Metrics)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QueryMetric>()
            .HasMany(q => q.ToolInvocations)
            .WithOne(t => t.QueryMetric)
            .HasForeignKey(t => t.QueryMetricId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QueryMetric>()
            .HasOne(q => q.AssistantMessage)
            .WithMany()
            .HasForeignKey(q => q.AssistantMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<QueryMetric>()
            .Property(q => q.EstimatedCostUsd)
            .HasPrecision(18, 8);
    }
}
