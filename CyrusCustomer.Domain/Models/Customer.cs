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
        public string ResponsiblePerson { get; set; } = "1";
        public string Year { get; set; }
        public string Month { get; set; }

        //update new column from here
        public string Contractor { get; set; }
        public long ContractorPhoneNumber { get; set; } = 0;


        //Added Default value if any of this properties return null from excel sheet
        public string InternalAccountant { get ; set ; } = "0";
        public long InternalAccountantPhone { get; set; } = 0;
        public string CharteredAccountant { get; set; } = "0";
        public long CharteredAccountantPhone { get; set; } 
        public bool Status { get; set; } //  is check box 

        public string CountOfBranches { get; set; } = "0";  
        //end of update
        public string? UserUpdated { get; set; }
        public bool UpdateConfirmed { get; set; }
        public DateTime UpdateDate { get; set; } = DateTime.Today;
        public string? Notes { get; set; }
        // Foreign key to the Credential (User)
        //public int CredentialId { get; set; }
        //[ForeignKey("CredentialId")]
        //public Credential Credential { get; set; }
        public virtual ICollection<Credential>? Credentials { get; set; }
        public virtual ICollection<Branch>? Branches { get; set; }
    }
}
