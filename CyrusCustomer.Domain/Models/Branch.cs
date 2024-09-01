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

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }
    }
}
