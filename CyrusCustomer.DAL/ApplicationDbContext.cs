using CyrusCustomer.Domain;
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


            modelBuilder.Entity<Customer>()
                .Property(c => c.Status)
                .HasConversion(
                    v => v.ToString(), // Store the enum as a string
                    v => (CustomerStatus)Enum.Parse(typeof(CustomerStatus), v) // Retrieve the enum from the stored string
                );


            //       modelBuilder.Entity<Credential>().HasData(
            //    new Credential { Id = 1, Email = "admin@Cyrus.com", Password = "Cyrus@2024", Name = "Admin" },
            //    new Credential { Id = 2, Email = "tony@Cyrus.com", Password = "Cyrus@2024", Name = "Tony" },
            //    new Credential { Id = 3, Email = "mahmoud@Cyrus.com", Password = "Cyrus@2024", Name = "Mahmoud" },
            //    new Credential { Id = 4, Email = "mina@Cyrus.com", Password = "Cyrus@2024", Name = "Mina"},
            //    new Credential { Id = 5, Email = "mohamad@Cyrus.com", Password = "Cyrus@2024", Name = "Mohamad" },
            //    new Credential { Id = 6, Email = "amr@Cyrus.com", Password = "Cyrus@2024", Name = "Amro" },
            //    new Credential { Id = 7, Email = "youssef@Cyrus.com", Password = "Cyrus@2024", Name = "Youssef" },
            //    new Credential { Id = 8, Email = "sameh@Cyrus.com", Password = "Cyrus@2024", Name = "Sameh" }
            //);


            base.OnModelCreating(modelBuilder);
            }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Credential> Credentials { get; set; }
        public DbSet<Branch> Branches { get; set; }
        //public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<CustomerUserAssignment> CustomerUserAssignments { get; set; }


    }
}
