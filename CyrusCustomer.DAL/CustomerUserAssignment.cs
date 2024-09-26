using CyrusCustomer.Domain;
using System.ComponentModel.DataAnnotations;

namespace CyrusCustomer.Models
{
    public class CustomerUserAssignment
    {
        [Key]
        public int Id { get; set; } // Primary key property

        public int CustomerId { get; set; }
        public string UserId { get; set; }
    }
}
