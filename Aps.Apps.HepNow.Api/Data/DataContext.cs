namespace Aps.Apps.HepNow.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.RegisterAllEntities<EntityDef>();
        modelBuilder.SoftDeleteGolobalFilter();

        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        base.OnModelCreating(modelBuilder);
        #region dbContextConfiguring
        modelBuilder.Entity<Products>().Property(a => a.IsActive).HasDefaultValue(true);
        #endregion
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    #region dbContextSets

    #endregion
}

