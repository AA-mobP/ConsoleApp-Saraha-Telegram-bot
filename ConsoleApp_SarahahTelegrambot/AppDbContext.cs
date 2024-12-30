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
            optionsBuilder.UseSqlServer("Data Source=DESKTOP-ECE5S76\\SQL2022;Database=SarahaTelegramBot;Integrated Security=True;Encrypt=False;Trust Server Certificate=True");
        }
    }
}
