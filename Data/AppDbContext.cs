using EquipmentDB.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentDB.Data;

public class AppDbContext : DbContext
{
    public DbSet<Room> Rooms { get; set; }
    public DbSet<EquipmentType> EquipmentTypes { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<EventLogEntry> EventLog { get; set; }
    public DbSet<LoginHistory> LoginHistory { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder o)
    {
        var cs = DatabaseConfig.ConnectionString;
        o.UseMySql(cs, ServerVersion.AutoDetect(cs))
         .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Room>().ToTable("rooms");
        m.Entity<EquipmentType>().ToTable("equipment_types");
        m.Entity<Status>().ToTable("statuses");
        m.Entity<Staff>().ToTable("staff");
        m.Entity<User>().ToTable("users");
        m.Entity<Equipment>().ToTable("equipment");
        m.Entity<EventLogEntry>().ToTable("event_log");
        m.Entity<LoginHistory>().ToTable("login_history");

        m.Entity<Staff>().HasOne(s => s.Room).WithMany(r => r.Staff)
            .HasForeignKey(s => s.RoomId).OnDelete(DeleteBehavior.SetNull);
        m.Entity<User>().HasOne(u => u.Staff).WithMany(s => s.Users)
            .HasForeignKey(u => u.StaffId).OnDelete(DeleteBehavior.SetNull);
        m.Entity<Equipment>().HasOne(e => e.Type).WithMany(t => t.Equipment)
            .HasForeignKey(e => e.TypeId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Equipment>().HasOne(e => e.Room).WithMany(r => r.Equipment)
            .HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Equipment>().HasOne(e => e.Status).WithMany(s => s.Equipment)
            .HasForeignKey(e => e.StatusId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<EventLogEntry>().HasOne(e => e.User).WithMany(u => u.EventLogs)
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        m.Entity<LoginHistory>().HasOne(l => l.User).WithMany(u => u.LoginHistories)
            .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
