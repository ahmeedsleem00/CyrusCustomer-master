using CyrusCustomer.Domain.Models;
using CyrusCustomer.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CyrusCustomer.Models
{
    public class AssignViewModel
    {
        public IEnumerable<SelectListItem> Users { get; set; }
        public string SelectedUserId { get; set; }
        public string SearchString { get; set; }
        public PaginatedList<Customer> PaginatedCustomers { get; set; }
        public Dictionary<int, List<string>> CustomerAssignments { get; set; }

    }
}
