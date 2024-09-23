namespace CyrusCustomer.Models
{
    public class ReportContainer
    {
        public List<Totals> Totals { get; set; }
        public List<StatusCountViewModel> StatusCountViewModels { get; set; }
        public int GrandTotal { get; set; } // For the total count


    }
    public class Totals
    {
        public int Count { get; set; }
        public string Status { get; set; }

    }
    public class StatusCountViewModel
    {


        public string UserName { get; set; }
        public int Pending { get; set; }
        public int Yes { get; set; }
        public int No { get; set; }
        public int NA { get; set; }
        public int Non { get; set; }

    }
}
