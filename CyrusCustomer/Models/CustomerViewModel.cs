namespace CyrusCustomer.Models
{
    public class CustomerViewModel
    {
        public string Name { get; set; }
        public string TaxId { get; set; }
        public string Phone { get; set; }
        public string? ResponsiblePerson { get; set; } = null;
        public string Year { get; set; }
        public string Month { get; set; }
        public string CountOfBranches { get; set; }
        public string? Contractor { get; set; } = string.Empty;
        public long? ContractorPhoneNumber { get; set; } 
        public string? InternalAccountant { get; set; }
        public long? InternalAccountantPhone { get; set; }
        public string? CharteredAccountant { get; set; }
        public long? CharteredAccountantPhone { get; set; }
        public decimal? Amount1 { get; set; } = decimal.Zero;
        public decimal? Amount2 { get; set; } = decimal.Zero;
        public decimal? Amount3 { get; set; } = decimal.Zero;
        public string? Comments { get; set; } = string.Empty;
    }
}
