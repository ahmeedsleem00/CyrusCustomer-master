using CyrusCustomer.DAL;
using CyrusCustomer.Domain;
using CyrusCustomer.Domain.Models;
using CyrusCustomer.DTO_s;
using CyrusCustomer.Models;
using CyrusCustomer.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        private readonly UserManager<IdentityUser> _userManager;

        public CustomerController(ApplicationDbContext context
            , UserManager<IdentityUser> userManager
            )
        {
            this._context = context;
            this._userManager = userManager;
        }

        #region Status Count View
        public async Task<IActionResult> StatusCounts()
        {
            var statusCounts = await _context.Customers
                        .Where(c => !string.IsNullOrEmpty(c.By)) 
                .GroupBy(c => c.By)
                .Select(g => new StatusCountViewModel
                {
                    UserName = g.Key.ToString(),
                    Pending = g.Where(obj=>obj.Status == CustomerStatus.Pending).Count(),
                    Yes = g.Where(obj => obj.Status == CustomerStatus.Yes).Count(),
                    No = g.Where(obj => obj.Status == CustomerStatus.No).Count(),
                    Non = g.Where(obj => obj.Status == CustomerStatus.Non).Count(),
                    NA = g.Where(obj=>obj.Status == CustomerStatus.NA).Count(), 
                })
                .ToListAsync();

            // Calculate totals
            //var totals = await _context.Customers
            //    .GroupBy(c => c.Status)
            //    .Select(g => new Totals
            //    {
            //        Status = g.Key.ToString(),
            //        Count = g.Count(),
            //    }).ToListAsync();

            // Calculate grand total

            var totals = await _context.Customers
                .GroupBy(c => c.Status)
                .Select(g => new Totals
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                }).ToListAsync();

            var grandTotal = totals.Sum(t => t.Count);

            return View(new ReportContainer { Totals = totals,StatusCountViewModels = statusCounts,
                GrandTotal = grandTotal // Assuming you add this property to ReportContainer
            });
        }

        #endregion

        #region Index and Upload and ViewBranches


        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var allCustomers = await _context.Customers.ToListAsync(); // Admin sees all customers
                return View(allCustomers);
            }
            else
            {
                var userCustomers = await _context.CustomerUserAssignments
                    .Where(uc => uc.UserId == user.Id)
                    .Select(uc => uc.Customer)
                    .ToListAsync();

                return View(userCustomers); // Non-admin sees assigned customers only
            }


        }


        [HttpGet]
        public async Task<IActionResult> Index(string searchString, string selectedUserId, int pageNumber = 1, int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = user.Email == "admin@Cyrus.com";

            var customersQuery = _context.Customers.AsQueryable();

            if (!isAdmin)
            {
                var assignedCustomerIds = _context.CustomerUserAssignments
                    .Where(cua => cua.UserId == user.Id)
                    .Select(cua => cua.CustomerId);

                customersQuery = customersQuery.Where(c => assignedCustomerIds.Contains(c.Id));
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                customersQuery = customersQuery.Where(s => s.Name.Contains(searchString.Trim())
                                                    || s.TaxId.Contains(searchString.Trim())
                                                    || s.CountOfBranches.Contains(searchString.Trim())
                                                    || s.Month.Contains(searchString.Trim())
                                                    || s.Year.Contains(searchString.Trim())
                                                    || s.By.Contains(searchString.Trim()));
            }

            int totalRecords = await customersQuery.CountAsync();

            var customers = await customersQuery
                                    .Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            var customerAssignments = await _context.CustomerUserAssignments
                .Where(cua => customers.Select(c => c.Id).Contains(cua.CustomerId))
                .ToListAsync();

            var customerAssignmentsDict = customerAssignments
                .GroupBy(cua => cua.CustomerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(cua => cua.UserId).ToList()
                );

            IEnumerable<SelectListItem> users = new List<SelectListItem>();
            if (isAdmin)
            {
                users = _userManager.Users.Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.Email
                });
            }

            var viewModel = new AssignViewModel
            {
                Users = users,
                PaginatedCustomers = new PaginatedList<Customer>(customers, totalRecords, pageNumber, pageSize),
                CustomerAssignments = customerAssignmentsDict,
                SearchString = searchString, // Pass the search string to the view
                SelectedUserId = selectedUserId,// Pass the selected user ID to the view

            };

            return View(viewModel);


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

        #region Assign Methods




        [HttpPost]
        //[Authorize(Roles = "Admin")]  // Only Admin can access this method
        public async Task<IActionResult> AssignCustomers(string Id, List<int> SelectedCustomerIds)
        {
            if (string.IsNullOrWhiteSpace(Id) || SelectedCustomerIds == null || !SelectedCustomerIds.Any())
            {
                return BadRequest("Invalid input.");
            }

            var existingAssignments = _context.CustomerUserAssignments
                .Where(cua => cua.UserId == Id);

            foreach (var customerId in SelectedCustomerIds)
            {
                var assignment = new CustomerUserAssignment
                {
                    CustomerId = customerId,
                    UserId = Id
                };
                _context.CustomerUserAssignments.Add(assignment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
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

        [HttpPost]
        public async Task<IActionResult> SaveCommentAndUpdate(CommentAndUpdateViewModel model)
        {

            if (!ModelState.IsValid)
            {
                // Handle invalid model state
                return RedirectToAction("Details", new { id = model.Id });
            }

            // Debugging: Check if model values are correctly received

            var customer = await _context.Customers.FindAsync(model.Id);
            if (customer == null)
            {
                return NotFound();
            }

            // Update the customer's properties
            customer.Comments = model.Comments;
            customer.IsUpdated = model.IsUpdated;

            // Save changes to the database
            _context.Update(customer);
            await _context.SaveChangesAsync();

            // Redirect back to the Details page
            return RedirectToAction("Details", new { id = model.Id });
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
                    //var customerBranches = new Dictionary<string, List<string>>();
                    var customerData = new List<(string Year, string Month, string CountOfBranches, string CustomerName, string TaxId, string BranchName, string ResponsiblePerson, string Phone, string UserUpdated, DateTime UpdateDate,
                            decimal Amount1, decimal Amount2, decimal Amount3, bool Collected
                        , string Contractor, string ContractorPhoneNumber, string InternalAccountant, string InternalAccountantPhone, string CharteredAccountant, string CharteredAccountantPhone, string User, string by, CustomerStatus Status)>();

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
                        var branchName = worksheet.Cells[row, 16].Text.Trim();
                        var by = worksheet.Cells[row, 15].Text.Trim();
                        var userUpdated = worksheet.Cells[row, 9].Text.Trim();
                        var updateDateCell = worksheet.Cells[row, 10].Text.Trim();
                        //var users = worksheet.Cells[row, 18].Text.Trim(); // Users column should be in column 18

                        // Parse new properties
                        decimal amount1 = 0;
                        decimal.TryParse(worksheet.Cells[row, 18].Text.Trim(), out amount1); // Assuming column 18 for Amount1

                        decimal amount2 = 0;
                        decimal.TryParse(worksheet.Cells[row, 19].Text.Trim(), out amount2); // Assuming column 19 for Amount2

                        decimal amount3 = 0;
                        decimal.TryParse(worksheet.Cells[row, 20].Text.Trim(), out amount3); // Assuming column 20 for Amount3

                        bool collected = false;
                        bool.TryParse(worksheet.Cells[row, 21].Text.Trim(), out collected); // Assuming column 21 for Collected

                        DateTime.TryParse(updateDateCell, out DateTime updateDate);

                        CustomerStatus customerStatus;
                        if (!Enum.TryParse(status, true, out customerStatus))
                        {
                            customerStatus = CustomerStatus.NA; // or whatever default you want
                        }
                        // Get userId from the "By" column

                        customerData.Add((year, month, countOfBranches, customerName, taxId, branchName, responsiblePerson, phone, userUpdated, updateDate, amount1, amount2, amount3, collected, contractor, contractorPhoneNumber, internalAccountant, internalAccountantPhone, charteredAccountant, charteredAccountantPhone, "DefaultUser", by, customerStatus));


                        //// Group branches by customer
                        ///
                        //if (!customerBranches.ContainsKey(customerName))
                        //{
                        //    customerBranches[customerName] = new List<string>();
                        //}
                        //customerBranches[customerName].Add(branchName);

                        //// Group branches by customer TaxId
                        //if (!customerBranches.ContainsKey(taxId))
                        //{
                        //    customerBranches[taxId] = new List<string>();
                        //}

                        //if (!customerBranches[taxId].Contains(branchName))
                        //{
                        //    customerBranches[taxId].Add(branchName);
                        //}
                        // }


                        //var joinedData = from excelData in customerData
                        //                 join dbCustomer in existingCustomers
                        //                 on excelData.TaxId equals dbCustomer.TaxId into joined
                        //                 from customer in joined.DefaultIfEmpty()
                        //                 select new
                        //                 {
                        //                     ExcelData = excelData,
                        //                     DbCustomer = customer
                        //                 };

              
                }
                    var existingCustomers = await _context.Customers.ToListAsync();
                    var users = await _context.Users.ToListAsync(); // Fetch all users from the database

                    foreach (var data in customerData)
                    {
                        // Find the user from the database whose UserName matches the 'By' column from Excel

                        var customer = existingCustomers.FirstOrDefault(c => c.TaxId == data.TaxId);


                        if (customer != null)
                        {
                            // Update existing customer details
                            customer.Name = data.CustomerName;
                            customer.Phone = data.Phone;
                            customer.ResponsiblePerson = data.ResponsiblePerson;
                            customer.UserUpdated = data.UserUpdated ?? "DefaultUser";
                            customer.UpdateDate = data.UpdateDate;
                            customer.Amount1 = data.Amount1;
                            customer.Amount2 = data.Amount2;
                            customer.Amount3 = data.Amount3;
                            customer.Status = data.Status;
                            customer.Collected = data.Collected;


                            if (customer.Branches.All(b => b.BranchName != data.BranchName))
                            {
                                customer.Branches.Add(new Branch { BranchName = data.BranchName });
                            }

                            _context.Customers.Update(customer);
                        }
                        else
                        {
                            long contractorPhone;
                            long internalAccountantPhoneNumber;

                            long charteredAccountantPhoneNumber;

                            // Try parsing each property from string to long
                            long.TryParse(data.ContractorPhoneNumber, out contractorPhone);
                            long.TryParse(data.InternalAccountantPhone, out internalAccountantPhoneNumber);
                            long.TryParse(data.CharteredAccountantPhone, out charteredAccountantPhoneNumber);



                            // New customer

                            var newCustomer = new Customer
                            {
                                Name = data.CustomerName,
                                TaxId = data.TaxId,
                                Phone = data.Phone,
                                Year = data.Year,
                                Month = data.Month,
                                CountOfBranches = data.CountOfBranches,
                                ResponsiblePerson = data.ResponsiblePerson,
                                Contractor = data.Contractor,
                                ContractorPhoneNumber = contractorPhone, // Ensure it's a string
                                InternalAccountant = data.InternalAccountant?.ToString(), // Ensure it's a string
                                InternalAccountantPhone = internalAccountantPhoneNumber, // Ensure it's a string
                                CharteredAccountantPhone = charteredAccountantPhoneNumber,
                                CharteredAccountant = data.CharteredAccountant?.ToString(), // Ensure it's a string

                                UserUpdated = data.UserUpdated ?? "DefaultUser",
                                Status = data.Status,
                                By = data.by,
                                Amount1 = data.Amount1,
                                Amount2 = data.Amount2,
                                Amount3 = data.Amount3,
                                Collected = data.Collected,
                                Branches = new List<Branch> { new Branch { BranchName = data.BranchName } }
                            };
                            _context.Customers.Add(newCustomer);
                            customer = newCustomer;

                        }
                        _context.SaveChanges();

                        var assignedUser = users.FirstOrDefault(u => u.UserName == data.by);

                        if (assignedUser != null)
                        {
                            _context.CustomerUserAssignments.Add(new CustomerUserAssignment { CustomerId = customer.Id, UserId = assignedUser.Id });
                        }

                    }


                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error occurred: " + ex.Message);
                    }
                }

            var customers = await _context.Customers.ToListAsync();
        }
    
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
