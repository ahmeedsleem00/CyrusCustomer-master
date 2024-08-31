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
        public string BranchName { get; set; }
        public string? VersionUpdated { get; set; }
        public string? UserUpdated { get; set; }
        public DateTime UpdateDate { get; set; }
        public string? Notes { get; set; }
        public virtual ICollection<Credential>? Credentials { get; set; }
        public virtual ICollection<Branche>? Branches { get; set; }
    }
}
