using CyrusCustomer.DAL;
using CyrusCustomer.Domain.Models;
using CyrusCustomer.Models;
using CyrusCustomer.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers.ToListAsync();
            return View(customers);
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
            if(customers == null)
            {
                return NotFound();
            }
            var viewModel = customers.Branches.Select(b => new BranchViewModel
            {
                BranchName = b.BranchName,
                ResponsiblePerson = customers.ResponsiblePerson, // Or logic to fetch
                CustomerName = customers.Name
            }).ToList();
            return View(customers.Branches);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,TaxId,Phone,VersionUpdated,UserUpdated,UpdateDate,Notes")] Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customer);
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
                        throw new Exception(message: "There Was an Error, Return to Developers Team");
                    }

                }

            }
            return View(customer);
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

            if(customer == null) { return NotFound(); }
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
            var allCustomers = _context.Customers.ToList();
            _context.Customers.RemoveRange(allCustomers);
          await _context.SaveChangesAsync();

            ViewBag.Message = "All customer data has been deleted.";
            return RedirectToAction("Index", "Home");

        }
        #endregion

        #region Excel Sheet Methods

        public IActionResult DownloadBranchesFileExcel()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/spreedsheet/Update_Sheet.xlsx");

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreedsheet.sheet", "Update_Sheet.xlsx");
        }
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

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var customerName = worksheet.Cells[row, 1].Text.Trim();
                        var taxId = worksheet.Cells[row, 2].Text.Trim();
                        var branchName = worksheet.Cells[row, 3].Text.Trim();
                        var responsiblePerson = worksheet.Cells[row, 4].Text.Trim();
                        var phone = worksheet.Cells[row, 5].Text.Trim();
                        var userUpdated = worksheet.Cells[row, 6].Text.Trim();
                        var updateDateCell = worksheet.Cells[row, 7].Text.Trim();
                        DateTime.TryParse(updateDateCell, out DateTime updateDate);
                      
                        // Group branches by customer
                        if (!customerBranches.ContainsKey(customerName))
                        {
                            customerBranches[customerName] = new List<string>();
                        }
                        customerBranches[customerName].Add(branchName);

                        // Check if customer already exists in the database
                        var existingCustomer = await _context.Customers
                            .Include(c => c.Branches)
                            .FirstOrDefaultAsync(c => c.TaxId == taxId);

                        if (existingCustomer != null)
                        {
                            // Update existing customer details
                            existingCustomer.Name = customerName;
                            existingCustomer.Phone = phone;
                            existingCustomer.ResponsiblePerson = responsiblePerson;
                            existingCustomer.UserUpdated = userUpdated;
                            existingCustomer.UpdateDate = updateDate;
                            //existingCustomer.Notes = notes;

                            // Add new branches to existing customer
                            if (existingCustomer.Branches.All(b => b.BranchName != branchName))
                            {
                                var newBranch = new Branch { BranchName = branchName };
                                existingCustomer.Branches.Add(newBranch);
                            }

                            _context.Customers.Update(existingCustomer);
                        }
                        else
                        {
                            // Create a new customer with branches
                            var newCustomer = new Customer
                            {
                                Name = customerName,
                                TaxId = taxId,
                                Phone = phone,
                                ResponsiblePerson = responsiblePerson,
                                UserUpdated = userUpdated,
                                UpdateDate = updateDate,
                                //Notes = notes,
                        //        Credentials = new List<Credential>
                        //{
                        //    new Credential { Email = email, Password = password }
                        //},
                                Branches = new List<Branch>
                        {
                            new Branch { BranchName = branchName }
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
            Console.WriteLine("Number of customers in DB: " + customers.Count);
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
