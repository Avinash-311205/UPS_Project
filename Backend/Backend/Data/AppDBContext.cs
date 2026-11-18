using Microsoft.EntityFrameworkCore;
using Backend.Model;
namespace Backend.Data
{
    public class AppDBContext:DbContext
    {
        public  AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
    }
}
