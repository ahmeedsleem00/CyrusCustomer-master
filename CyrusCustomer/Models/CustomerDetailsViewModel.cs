using CyrusCustomer.Domain.Models;

namespace CyrusCustomer.Models
{
    public class CustomerDetailsViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public CustomerStatus[] SelectedStatuses { get; set; } = new CustomerStatus[0];
    }
}
