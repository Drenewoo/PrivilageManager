using Microsoft.EntityFrameworkCore;
using webapp.Models;

namespace webapp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } // Tabela użytkowników
        public DbSet<Department> Departments { get; set; } // Tabela placówek
        public DbSet<Degree> Degrees { get; set; } // Tabela stopni
        public DbSet<Message> Messages { get; set; } // Tabela wiadomości
        public DbSet<Programs> Programs { get; set; } // Tabela programów
        public DbSet<Permission> Permissions { get; set; } // Tabela uprawnień
        public DbSet<UserPermission> UserPermissions { get; set; } // Tabela uprawnień użytkowników
        public DbSet<Status> Statuses { get; set; } // Tabela statusów


    }
}