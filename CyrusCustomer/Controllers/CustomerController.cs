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
using System.Security.Claims;

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





        [HttpGet]
        public async Task<IActionResult> Index(string searchString, string selectedUserId, int pageNumber = 1, int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = user.Email == "admin@Cyrus.com";

            var customersQuery = _context.Customers.AsQueryable();

            if (!isAdmin)
            {
                var assignedCustomerIds = await _context.CustomerUserAssignments
                    .Select(cua => cua.CustomerId)
                    .ToListAsync();

                customersQuery = customersQuery.Where(c => c.Status != CustomerStatus.Yes);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                CustomerStatus? status = null;

                if (Enum.TryParse(typeof(CustomerStatus), searchString.Trim(), true, out var parsedStatus))
                {
                    status = (CustomerStatus)parsedStatus;
                }
                var searchByStatus = _context.Customers.Select(c => c.Status).ToString();
                customersQuery = customersQuery.Where(s => s.Name.Contains(searchString.Trim())
                                                        || s.TaxId.Contains(searchString.Trim())
                                                        || s.CountOfBranches.Contains(searchString.Trim())
                                                        || s.Month.Contains(searchString.Trim())
                                                        || s.Year.Contains(searchString.Trim())
                                                        //|| s.By.Contains(searchString.Trim())
                                                        || (status.HasValue && s.Status == status.Value)
                                                        || _context.CustomerUserAssignments.Any(cua => cua.CustomerId == s.Id && _context.Users
                                                         .Any(u => u.Id == cua.UserId && u.Email
                                                         .Contains(searchString.Trim())))
                                                        );
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
                .ToDictionary(g => g.Key, g => g.Select(cua => cua.UserId).ToList());

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
                SearchString = searchString,
                SelectedUserId = selectedUserId,
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
                ResponsiblePerson = customers.ResponsiblePerson, 
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

            // Get the name of the user who is assigning the customers
            var assigningUser = await _context.Users.FindAsync(Id);
            string assigningUserName = assigningUser != null ? assigningUser.UserName : "Unknown"; // Use the UserName property as needed

            // Get all assignments for the selected customers
            var existingAssignments = await _context.CustomerUserAssignments
                .Where(cua => SelectedCustomerIds.Contains(cua.CustomerId)).ToListAsync();

            // For each customer, check if there are existing assignments
            foreach (var customerId in SelectedCustomerIds)
            {
                // Check if the customer is already assigned to another user
                var previousAssignment = existingAssignments
                    .FirstOrDefault(cua => cua.CustomerId == customerId && cua.UserId != Id);

                // If there is a previous assignment, remove it
                if (previousAssignment != null)
                {
                    _context.CustomerUserAssignments.Remove(previousAssignment);
                }

                // Now, check if the current user already has an assignment for this customer
                var existingAssignment = existingAssignments
                    .FirstOrDefault(cua => cua.CustomerId == customerId && cua.UserId == Id);

                if (existingAssignment != null)
                {
                    // Update properties of the existing assignment if needed
                    existingAssignment.By = assigningUserName; // Update the "By" field
                    _context.CustomerUserAssignments.Update(existingAssignment);
                }
                else
                {
                    // If no existing assignment, create a new one
                    var assignment = new CustomerUserAssignment
                    {
                        CustomerId = customerId,
                        UserId = Id,
                        By = assigningUserName // Set the "By" field
                    };
                    _context.CustomerUserAssignments.Add(assignment);
                }
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
            var customer = await _context.Customers
                .Include(c=> c.Users)
        .Include(c => c.Credentials)
        .Include(c => c.Branches)
        .FirstOrDefaultAsync(m => m.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

                //// Prepare the view model
                //var model = new CommentAndUpdateViewModel
                //{
                //    Id = customer.Id,
                //    Comments = customer.Comments,
                //    IsUpdated = customer.IsUpdated,
                //    SelectedStatus = new List<string>() // Assuming you have a property in the Customer class to hold statuses
                //};

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

            var customer = await _context.Customers
        .Include(c => c.Users) // Include users if you need to manipulate them
        .FirstOrDefaultAsync(c => c.Id == model.Id);

            if (customer == null)
            {
                return NotFound();
            }
            

            // Update the customer's properties
            customer.Comments = model.Comments;
            customer.IsUpdated = model.IsUpdated;
            customer.Status = model.Status; 
            customer.By = model.By;
            customer.CountOfBranches = model.CountOfBranches.ToString();
            if(model.Status == CustomerStatus.Yes)
            {
                var user = _context.CustomerUserAssignments.FirstOrDefault(cu => cu.CustomerId == customer.Id);
                //var user = User;
                //var x = _context.Users.First(c => c.Email == User.FindFirstValue(ClaimTypes.Email));
                //user.UserId  = x.Id;
                if (user != null)
                {
                    // Find the user by their email
                    var currentUser = _context.Users
                        .FirstOrDefault(c => c.Email == User.FindFirstValue(ClaimTypes.Email));

                    if (currentUser != null)
                    {
                        // Assign the current user's ID to the assignment
                        user.UserId = currentUser.Id;

                        // Save the changes to the database
                        _context.SaveChanges();
                    }
                }
                    //_context.SaveChanges();
            }
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

                            if (customer.Branches == null)
                            {
                                customer.Branches = new List<Branch>(); // Replace Branch with your actual branch type
                            }

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


                        // Check for existing assignment
                        var assignedUser = users.FirstOrDefault(u => u.UserName == data.by);
                        if (assignedUser != null)
                        {
                            var existingAssignment = await _context.CustomerUserAssignments
                      .FirstOrDefaultAsync(cua => cua.CustomerId == customer.Id);

                            if (existingAssignment != null)
                            {

                                existingAssignment.UserId = assignedUser.Id;
                                _context.CustomerUserAssignments.Update(existingAssignment);
                            }
                            else { 
                            // If it doesn't exist, create a new assignment
                            var newAssignment = new CustomerUserAssignment
                                {
                                    CustomerId = customer.Id,
                                    UserId = assignedUser.Id
                                };
                                await _context.CustomerUserAssignments.AddAsync(newAssignment);
                            }
                        }
                        _context.SaveChanges();


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


        #region Manage Users

        public IActionResult ManageUsers()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        #endregion


        #region Change Password
        [HttpGet]
        public async Task<IActionResult> ChangePassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel { UserId = user.Id, Email = user.Email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

    var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                // Set success message in TempData
                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction(nameof(ManageUsers)); // Redirect to the page you want after password change
            }

            // Add errors if password change failed
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
        #endregion



    }
}
