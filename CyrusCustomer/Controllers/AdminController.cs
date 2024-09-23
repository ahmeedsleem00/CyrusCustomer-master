using CyrusCustomer.DAL;
using CyrusCustomer.Domain;
using CyrusCustomer.Domain.Models;
using CyrusCustomer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CyrusCustomer.Controllers
{
    [Route("Admin/[action]")]

    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            this._context = context;
            this._userManager = userManager;
        }

        public IActionResult AssignCustomers()
        {
            ViewData["Users"] = new SelectList(_context.Users, "Id", "UserName");
            ViewData["Customers"] = new MultiSelectList(_context.Customers, "Id", "Name");
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> AssignCustomers(string SelectedUserId, List<int> SelectedCustomerIds)
        {
            foreach (var customerId in SelectedCustomerIds)
            {
                // Check if the assignment already exists
                var existingAssignment = await _context.CustomerUserAssignments
                    .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.UserId == SelectedUserId);

                if (existingAssignment == null)
                {
                    var assignment = new CustomerUserAssignment
                    {
                        CustomerId = customerId,
                        UserId = SelectedUserId
                    };
                    _context.CustomerUserAssignments.Add(assignment);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }






        [HttpPost]
        public async Task<IActionResult> UpdateCustomerAssignments(Dictionary<int, List<string>> userAssignments)
        {
            foreach (var assignment in userAssignments)
            {
                int customerId = assignment.Key;
                List<string> userIds = assignment.Value;

                // Remove old assignments
                var oldAssignments = _context.CustomerUserAssignments.Where(cua => cua.CustomerId == customerId);
                _context.CustomerUserAssignments.RemoveRange(oldAssignments);

                // Add new assignments
                foreach (var userId in userIds)
                {
                    _context.CustomerUserAssignments.Add(new CustomerUserAssignment
                    {
                        CustomerId = customerId,
                        UserId = userId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


    }
}
