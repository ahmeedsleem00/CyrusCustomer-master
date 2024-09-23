using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace CyrusCustomer.DAL
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();



            optionsBuilder.UseSqlServer("Server=.;Database=CyrusCustomerPr;Trusted_Connection = True; MultipleActiveResultSets=true;");

            return new ApplicationDbContext(optionsBuilder.Options);


        }
    }
}
