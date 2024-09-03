using CyrusCustomer.DAL;
using CyrusCustomer.Domain.Models;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore;

namespace CyrusCustomer.Spreadsheet
{
    public class DatabaseUpdater
    {
        private readonly ApplicationDbContext _context;

        public DatabaseUpdater(ApplicationDbContext context)
        {
            this._context = context;
        }

        public async Task UpdateDatabase(List<Customer> customers)
        {
            foreach (var customer in customers)
            {
                var existingCustomer = await _context.Customers
                                                     .FirstOrDefaultAsync(x => x.TaxId == customer.TaxId);
                if (existingCustomer != null)
                {
                    existingCustomer.Year = customer.Year;
                    existingCustomer.Month = customer.Month;
                    existingCustomer.CountOfBranches = customer.CountOfBranches;
                    existingCustomer.Name = customer.Name;
                    existingCustomer.Phone = customer.Phone;
                    existingCustomer.BranchName = customer.BranchName;
                    existingCustomer.ResponsiblePerson = customer.ResponsiblePerson;
                    existingCustomer.UserUpdated = customer.UserUpdated;
                    existingCustomer.UpdateDate = customer.UpdateDate;
                    existingCustomer.Notes = customer.Notes;
                    existingCustomer.Contractor = customer.Contractor;
                    existingCustomer.ContractorPhoneNumber = customer.ContractorPhoneNumber;
                    existingCustomer.InternalAccountant = customer.InternalAccountant;
                    existingCustomer.InternalAccountantPhone = customer.InternalAccountantPhone;
                    existingCustomer.CharteredAccountant = customer.CharteredAccountant;
                    existingCustomer.CharteredAccountantPhone = customer.CharteredAccountantPhone;
                    existingCustomer.Status = customer.Status;

                }
                else
                {
                    _context.Customers.Add(customer);
                }
            }
            await _context.SaveChangesAsync();
            
        }

        //public async Task UpdateCustomer(string filePath)
        //{
        //    var reader = new ExcelReader();
        //    var customerList = reader.ReadExcel(filePath);

        //    var dbUpdater = new DatabaseUpdater(new ApplicationDbContext());
        //    await dbUpdater.UpdateDatabase(customerList);

        //}
    }
}
