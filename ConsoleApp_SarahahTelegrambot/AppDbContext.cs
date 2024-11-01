using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_SarahahTelegrambot
{
    public class AppDbContext : DbContext
    {
        public DbSet<MessageInfoModel> tblMessages { get; set; }
        public DbSet<UserModel> tblUsers { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("DbConnectionStringHere");
        }
    }
}
