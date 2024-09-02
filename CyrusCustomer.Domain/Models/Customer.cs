using CyrusCustomer.Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyrusCustomer.Domain.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? TaxId { get; set; }  
        public string? Phone { get; set; }
        public string? BranchName { get; set; }
        public string? ResponsiblePerson { get; set; } 
        public string? UserUpdated { get; set; }
        public bool UpdateConfirmed { get; set; }
        public DateTime UpdateDate { get; set; } = DateTime.Today;
        public string? Notes { get; set; }
        public virtual ICollection<Credential>? Credentials { get; set; }
        public virtual ICollection<Branch>? Branches { get; set; }
    }
}
