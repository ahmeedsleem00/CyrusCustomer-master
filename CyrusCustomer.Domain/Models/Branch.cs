using CyrusCustomer.Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyrusCustomer.Domain.Models
{
    public class Branch
    {
        public int Id { get; set; }
        public string BranchName { get; set; }
        public int CustomerId { get; set; }  // fk
        public string? UserUpdated { get; set; }
        public string? ResponsiblePerson { get; set; }
        public bool UpdateConfirmed { get; set; } //that was checkbox property

        public DateTime UpdateDate { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }
    }
}
