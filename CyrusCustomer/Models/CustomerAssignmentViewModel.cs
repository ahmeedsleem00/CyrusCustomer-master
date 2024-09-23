namespace CyrusCustomer.Models
{
    public class CustomerAssignmentViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public bool IsSelected { get; set; } // This will track the checkbox selection
    }
}