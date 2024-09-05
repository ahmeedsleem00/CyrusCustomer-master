using CyrusCustomer.DAL;
using CyrusCustomer.Domain.Models;
using CyrusCustomer.Models;
using CyrusCustomer.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Diagnostics.Contracts;
using System.Net;

namespace CyrusCustomer.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            this._context = context;
        }

        #region Index and Upload and ViewBranches
        public async Task<IActionResult> Index()
        {
            //// Get the current logged-in user's email
            //var userEmail = User.Identity.Name;

            var customers=  await _context.Customers.ToListAsync();

            //// Retrieve the corresponding credential/user from the database
            //var user = await _context.Credentials.FirstOrDefaultAsync(c => c.Email == userEmail);

            //// Get only the customers associated with the logged-in user
            //var customers = await _context.Customers
            //                              .Where(c => c.CredentialId == user.Id)
            //                              .ToListAsync();

            //return View(customers);
            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 10)
        {
            //int countOfBranches = 1;

            var customersQuery = from c in _context.Customers
                            select c;
            if (!String.IsNullOrEmpty(searchString))
            {
                customersQuery = customersQuery.Where(s => s.Name.Contains(searchString)
                                                 || s.TaxId.Contains(searchString)
                                                 || s.CountOfBranches.Contains(searchString)
                                                 || s.Month.Contains(searchString)
                                                 || s.Year.Contains(searchString));
            }
            int totalRecords = await customersQuery.CountAsync();
            var customers = await customersQuery
                                  .Skip((pageNumber - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            var paginatedList = new PaginatedList<Customer>(customers, totalRecords, pageNumber, pageSize);

            return View(paginatedList);
            //return View(await customers.ToListAsync());
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }


        public async Task<IActionResult> ViewBranches(int customerId)
        {
            var customers = await _context.Customers.Include(c => c.Branches)
                                               .FirstOrDefaultAsync(c => c.Id == customerId);
            if (customers == null)
            {
                return NotFound();
            }
            var viewModel = customers.Branches.Select(b => new BranchViewModel
            {
                Id = b.Id,
                BranchName = b.BranchName,
                ResponsiblePerson = customers.ResponsiblePerson, // Or logic to fetch
                CustomerName = b.BranchName,
                UserUpdated = b.UserUpdated,
                UpdateDate = b.UpdateDate,
                Notes = b.Notes

            }).ToList();
            return View(viewModel);
        }
        #endregion

        #region Details

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var customer = await _context.Customers.Include(c => c.Credentials).Include(c => c.Branches).FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);

        }
        #endregion

        #region Create

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,TaxId,Phone,VersionUpdated,UserUpdated,UpdateDate,Notes")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");

            }
            return View(customer);

        }
        #endregion

        #region Edit Methods

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();

            }
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("Id,Name,TaxId,Phone,VersionUpdated,UserUpdated,UpdateDate,Notes")] Customer customer)
        //{
        //    if (id != customer.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(customer);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!CustomerExists(customer.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw new Exception(message: "There Was an Error, Return to Developers Team");
        //            }

        //        }

        //    }
        //    return View(customer);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,TaxId,Phone,ResponsiblePerson,BranchName,UserUpdated,UpdateDate,Notes,UpdateConfirmed")] Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the existing customer from the database
                    var existingCustomer = await _context.Customers.FindAsync(id);

                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    // Update the existing customer properties

                    existingCustomer.UserUpdated = customer.UserUpdated;
                    existingCustomer.UpdateDate = customer.UpdateDate;
                    existingCustomer.Notes = customer.Notes;
                    existingCustomer.UpdateConfirmed = customer.UpdateConfirmed; // Update this property

                    // Save changes to the database
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw new Exception("There was an error, please contact the development team.");
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    throw new Exception("An unexpected error occurred: " + ex.Message);
                }

                // Redirect to the customer details page or another appropriate action
                return RedirectToAction("Index");
            }

            // If model state is not valid, return the same view with the current model state
            return View(customer);
        }


        // GET: Branch/EditBranches/5
        public async Task<IActionResult> EditBranches(int id, int customerId)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null)
            {
                return NotFound();
            }
            ViewBag.CustomerId = customerId;
            return View(branch);
        }

        // POST: Branch/EditBranches/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBranches(int id, [Bind("Id,BranchName,UserUpdated,UpdateDate,Notes,CustomerId,UpdateConfirmed")] BranchViewModel branchViewModel)
        {
            if (id != branchViewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the Branch entity from the database
                    var branch = await _context.Branches.FindAsync(branchViewModel.Id);

                    if (branch == null)
                    {
                        return NotFound();
                    }

                    // Update the Branch entity with data from the ViewModel
                    branch.BranchName = branchViewModel.BranchName;
                    branch.UserUpdated = branchViewModel.UserUpdated;
                    branch.UpdateDate = branchViewModel.UpdateDate;
                    branch.Notes = branchViewModel.Notes;
                    branch.UpdateConfirmed = branchViewModel.UpdateConfirmed; //




                    // Save changes to the database
                    _context.Update(branch);
                    await _context.SaveChangesAsync();

                    // Redirect to the ViewBranches action
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BranchExists(branchViewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                //  return RedirectToAction(nameof(ViewBranches), new { customerId = branchViewModel.CustomerId });

            }
            return RedirectToAction("Index", "Customer");
            //  return View(branchViewModel);

        }

        private bool BranchExists(int id)
        {
            return _context.Branches.Any(e => e.Id == id);
        }

        #endregion

        #region Deleted Methods

        public async Task<IActionResult> Delete(int id)
        {
            //if (id == null) { return NotFound(); }
            var customer = await _context.Customers.FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null) { return NotFound(); }
            return View(customer);

        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null) { return NotFound(); }
            _context.Customers.Remove(customer);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            // Load all customers along with their related branches
            var allCustomers = await _context.Customers.Include(c => c.Branches).ToListAsync();

            // Delete all branches
            foreach (var customer in allCustomers)
            {
                _context.Branches.RemoveRange(customer.Branches);
            }

            // Delete all customers
            _context.Customers.RemoveRange(allCustomers);

            // Save changes to the database
            await _context.SaveChangesAsync();

            ViewBag.Message = "All customer data has been deleted.";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Excel Sheet Methods

        //public IActionResult DownloadBranchesFileExcel()
        //{
        //    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/spreedsheet/Update_Sheet.xlsx");

        //    var fileBytes = System.IO.File.ReadAllBytes(filePath);
        //    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreedsheet.sheet", "Update_Sheet.xlsx");
        //}
        private async Task UpdateCustomersFromExcel(IFormFile file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook == null || package.Workbook.Worksheets.Count == 0)
                    {
                        throw new Exception("The uploaded file is not a valid Excel file or contains no worksheets.");
                    }

                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        throw new Exception("No worksheet found in the Excel file.");
                    }

                    var rowCount = worksheet.Dimension.Rows;

                    // Use a dictionary to group branch names by customer
                    var customerBranches = new Dictionary<string, List<string>>();
                    var customerData = new List<(string Year, string Month, string CountOfBranches, string CustomerName, string TaxId, string BranchName, string ResponsiblePerson, string Phone, string UserUpdated, DateTime UpdateDate,
                        string Status, string Contractor, string ContractorPhoneNumber, string InternalAccountant, string InternalAccountantPhone, string CharteredAccountant, string CharteredAccountantPhone)>();

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var year = worksheet.Cells[row, 1].Text.Trim();
                        var month = worksheet.Cells[row, 2].Text.Trim();
                        var taxId = worksheet.Cells[row, 3].Text.Trim();
                        var customerName = worksheet.Cells[row, 4].Text.Trim();
                        var countOfBranches = worksheet.Cells[row, 5].Text.Trim();
                        var contractor = worksheet.Cells[row, 6].Text.Trim();
                        var contractorPhoneNumber = worksheet.Cells[row, 7].Text.Trim();
                        var responsiblePerson = worksheet.Cells[row, 8].Text.Trim();
                        var phone = worksheet.Cells[row, 9].Text.Trim();
                        var internalAccountant = worksheet.Cells[row, 10].Text.Trim();
                        var internalAccountantPhone = worksheet.Cells[row, 11].Text.Trim();
                        var charteredAccountant = worksheet.Cells[row, 12].Text.Trim();
                        var charteredAccountantPhone = worksheet.Cells[row, 13].Text.Trim();
                        var status = worksheet.Cells[row, 14].Text.Trim();
                        var branchName = worksheet.Cells[row, 15].Text.Trim();

                        var userUpdated = worksheet.Cells[row, 9].Text.Trim();
                        var updateDateCell = worksheet.Cells[row, 10].Text.Trim();

                        DateTime.TryParse(updateDateCell, out DateTime updateDate);

                        customerData.Add((year, month, countOfBranches, customerName,
                            taxId, branchName, responsiblePerson, phone,
                            userUpdated, updateDate, status, contractor, contractorPhoneNumber,
                            internalAccountant, internalAccountantPhone, charteredAccountant,
                            charteredAccountantPhone));

                        // Group branches by customer
                        if (!customerBranches.ContainsKey(customerName))
                        {
                            customerBranches[customerName] = new List<string>();
                        }
                        customerBranches[customerName].Add(branchName);

                        // Group branches by customer TaxId
                        if (!customerBranches.ContainsKey(taxId))
                        {
                            customerBranches[taxId] = new List<string>();
                        }

                        if (!customerBranches[taxId].Contains(branchName))
                        {
                            customerBranches[taxId].Add(branchName);
                        }
                    }

                    var existingCustomers = await _context.Customers.Include(c => c.Branches).ToListAsync();

                    var joinedData = from excelData in customerData
                                     join dbCustomer in existingCustomers
                                     on excelData.TaxId equals dbCustomer.TaxId into joined
                                     from customer in joined.DefaultIfEmpty()
                                     select new
                                     {
                                         ExcelData = excelData,
                                         DbCustomer = customer
                                     };

                    foreach (var item in joinedData)
                    {
                        if (item.DbCustomer != null)
                        {
                            // Existing customer, update details
                            item.DbCustomer.Name = item.ExcelData.CustomerName;
                            item.DbCustomer.Phone = item.ExcelData.Phone;
                            item.DbCustomer.ResponsiblePerson = item.ExcelData.ResponsiblePerson;
                            item.DbCustomer.UserUpdated = item.ExcelData.UserUpdated ?? "DefaultUser"; // Set a default value if null
                            item.DbCustomer.UpdateDate = item.ExcelData.UpdateDate;

                            // Add new branch if not exists
                            if (item.DbCustomer.Branches.All(b => b.BranchName != item.ExcelData.BranchName))
                            {
                                item.DbCustomer.Branches.Add(new Branch { BranchName = item.ExcelData.BranchName });
                            }

                            _context.Customers.Update(item.DbCustomer);
                        }
                        else
                        {
                            // Initialize long variables for the properties that need conversion
                            long contractorPhoneNumber = 0;
                            long internalAccountant = 0;
                            long internalAccountantPhone = 0;
                            long charteredAccountant = 0;
                            long charteredAccountantPhone = 0;
                            //int countOfBranches = /*1*/; // Assuming this is an integer that needs parsing

                            // Try parsing each property from ExcelData
                            long.TryParse(item.ExcelData.ContractorPhoneNumber, out contractorPhoneNumber);
                            long.TryParse(item.ExcelData.InternalAccountant, out internalAccountant);
                            long.TryParse(item.ExcelData.InternalAccountantPhone, out internalAccountantPhone);
                            long.TryParse(item.ExcelData.CharteredAccountant, out charteredAccountant);
                            long.TryParse(item.ExcelData.CharteredAccountantPhone, out charteredAccountantPhone);
                            //int.TryParse(item.ExcelData.CountOfBranches, out countOfBranches);

                            // New customer
                            var newCustomer = new Customer
                            {
                                Name = item.ExcelData.CustomerName,
                                TaxId = item.ExcelData.TaxId,
                                Phone = item.ExcelData.Phone,
                                Year = item.ExcelData.Year,
                                Month = item.ExcelData.Month,
                                CountOfBranches = item.ExcelData.CountOfBranches, // Convert back to string to match Customer class property
                                ResponsiblePerson = item.ExcelData.ResponsiblePerson,
                                Contractor = item.ExcelData.Contractor,
                                ContractorPhoneNumber = contractorPhoneNumber, // Convert back to string
                                InternalAccountant = internalAccountant.ToString(), // Convert back to string
                                InternalAccountantPhone = internalAccountantPhone, // Convert back to string
                                CharteredAccountant = charteredAccountant.ToString(), // Convert back to string
                                CharteredAccountantPhone = charteredAccountantPhone, // Convert back to string
                                UserUpdated = item.ExcelData.UserUpdated ?? "DefaultUser", // Set a default value if null
                                Branches = new List<Branch>
    {
        new Branch { BranchName = item.ExcelData.BranchName }
    }
                            };

                            _context.Customers.Add(newCustomer);
                        }
                    }


                    try
                    {
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Data saved successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error occurred: " + ex.Message);
                    }
                }
            }

            var customers = await _context.Customers.ToListAsync();

        }
    

        [HttpPost]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Content("not found");
            }
            //var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FileName);

            //using (var stream = new FileStream(filePath, FileMode.Create))
            //{
            //    await file.CopyToAsync(stream);
            //}
            //var fileInfo = new FileInfo(file.FileName);
            await UpdateCustomersFromExcel(file);

            return RedirectToAction("Index", "Customer");

        }
        #endregion



    }
}
