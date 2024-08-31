using CyrusCustomer.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyrusCustomer.DAL
{
    public class ApplicationDbContext :IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
            protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                        .HasMany(c => c.Credentials)
                        .WithOne(c => c.Customer)
                        .HasForeignKey(c => c.CustomerId)
                        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Customer>()
                        .HasMany(c => c.Branches)
                        .WithOne(c => c.Customer)
                        .HasForeignKey(c => c.CustomerId)
                        .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<Credential>().HasData(

                  new Credential { Id = 1, Email = "admin@Cyrus.com", Password = "Cyrus@2024", Name = "admin", CustomerId = 1 },
         new Credential { Id = 2, Email = "tony@Cyrus.com", Password = "Cyrus@2024", Name = "tony", CustomerId = 1 },
         new Credential { Id = 3, Email = "mahmoud@Cyrus.com", Password = "Cyrus@2024", Name = "mahmoud", CustomerId = 2 },
         new Credential { Id = 4, Email = "mina@examCyrusple.com", Password = "Cyrus@2024", Name = "mina", CustomerId = 2 },
         new Credential { Id = 5, Email = "mohamad@Cyrus.com", Password = "Cyrus@2024", Name = "mohamad", CustomerId = 1 },
         new Credential { Id = 6, Email = "amro@Cyrus.com", Password = "Cyrus@2024", Name = "amro", CustomerId = 1 },
         new Credential { Id = 7, Email = "youssef@Cyrus.com", Password = "Cyrus@2024", Name = "youssef", CustomerId = 2 },
         new Credential { Id = 8, Email = "sameh@Cyrus.com", Password = "Cyrus@2024", Name = "sameh", CustomerId = 2 }
                 );

            base.OnModelCreating(modelBuilder);
            }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Credential> Credentials { get; set; }
        public DbSet<Branche> Branches { get; set; }

    }
}
