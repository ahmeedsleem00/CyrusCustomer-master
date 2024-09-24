using System.ComponentModel.DataAnnotations;

namespace CyrusCustomer.Domain.Models
{
    public class CustomerUserAssignment
    {
        [Key]
        public int Id { get; set; } // Primary key property

        public int CustomerId { get; set; }
        public string UserId { get; set; }
        //public  ApplicationUser User { get; set; }
        public virtual Customer Customer { get; set; }
        public string? By { get; set; }
       // public virtual ApplicationUser User { get; set; } // Reference to the user

    }
}