using CyrusCustomer.Domain.Models;

namespace CyrusCustomer.Models
{
    public class BranchViewModel
    {
        public int Id { get; set; }
        public string? BranchName { get; set; }
        public Branch? CustomerId { get; set; }
        public string? ResponsiblePerson { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }
        public string CountOfBranches { get; set; }  
        public string? CustomerName { get; set; }
        public string? UserUpdated { get; set; }
        public DateTime UpdateDate { get; set; }
        public string? Notes { get; set; }
        public bool UpdateConfirmed { get; set; } //that was checkbox property

    }
}
