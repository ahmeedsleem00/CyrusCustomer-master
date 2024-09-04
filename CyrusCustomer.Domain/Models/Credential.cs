using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CyrusCustomer.Domain.Models;

namespace CyrusCustomer.Domain.Models
{
    public class Credential
    {
       public int Id { get; set; }
        public string? Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }
        public int CustomerId { get; set; }
    
        public virtual Customer Customer { get; set; }

    }
}
