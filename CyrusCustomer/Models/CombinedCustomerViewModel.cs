using CyrusCustomer.Domain.Models;

namespace CyrusCustomer.Models
{
    public class CombinedCustomerViewModel
    {
        public PaginatedList<Customer> PaginatedCustomers { get; set; }
        public AssignViewModel AssignViewModel { get; set; }

    }
}
