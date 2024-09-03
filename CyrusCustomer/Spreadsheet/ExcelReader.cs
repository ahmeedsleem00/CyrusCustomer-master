using CyrusCustomer.DAL;
using CyrusCustomer.Domain.Models;
using OfficeOpenXml;

namespace CyrusCustomer.Spreadsheet
{
    public class ExcelReader
    {

        public static List<Customer> ReadExcelFile(string filepath)
        {
            var customers = new List<Customer>();

            using (var package = new ExcelPackage(filepath))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) { throw new Exception("No work sheet found"); }
                
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row < rowCount; row++) 
                {
                    long contractorPhoneNumber;
                    bool isPhoneParsed = long.TryParse(worksheet.Cells[row, 7].Text, out contractorPhoneNumber);
                    long internalAccountantPhone;
                    bool isInternalPhone = long.TryParse(worksheet.Cells[row,11].Text, out internalAccountantPhone);
                    long charteredAccountantPhone;
                    bool isChartedAccountantPhone= long.TryParse(worksheet.Cells[row,13].Text,out charteredAccountantPhone);
                    bool status;
                    bool isStatus = bool.TryParse(worksheet.Cells[row,14].Text, out status);

                    var customer = new Customer
                    {
                        Month = worksheet.Cells[row , 1].Text,
                         Year = worksheet.Cells[row, 2].Text,
                        TaxId = worksheet.Cells[row, 3].Text,
                        Name = worksheet.Cells[row,4].Text,
                        CountOfBranches = worksheet.Cells[row, 5].Text, 
                        Contractor = worksheet.Cells[row,6].Text,
                        ContractorPhoneNumber = worksheet.Cells[row, 7].Text,
                        ResponsiblePerson = worksheet.Cells[row, 8].Text, 
                        //BranchName = worksheet.Cells[row, 6].Text, 
                        Phone = worksheet.Cells[row, 9].Text, 
                        InternalAccountant = worksheet.Cells[row, 10].Text, 
                        InternalAccountantPhone = worksheet.Cells[row, 11].Text,
                        CharteredAccountant = worksheet.Cells[row,12].Text,
                        CharteredAccountantPhone = worksheet.Cells[row, 13].Text,
                        Status = isStatus ? status : false,
                        UpdateDate = DateTime.Parse(worksheet.Cells[row, 9].Text), 
                        Notes = worksheet.Cells[row, 10].Text 
                    };
                    customers.Add(customer);
                }
            }
            return customers;
        }

      
    }
}