using CyrusCustomer.Domain;
using CyrusCustomer.Domain.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CyrusCustomer.Models
{
    public class CustomerListViewModel
    {
        public PaginatedList<Customer> Customers { get; set; }
        public List<ApplicationUser> Users { get; set; }
        public Dictionary<int, List<string>> CustomerUserAssignments { get; set; } = new Dictionary<int, List<string>>();
    }

    //public class CustomerCheckboxViewModel
    //{
    //    public int CustomerId { get; set; }
    //    public string CustomerName { get; set; }
    //    public bool IsSelected { get; set; }
    //}
}
