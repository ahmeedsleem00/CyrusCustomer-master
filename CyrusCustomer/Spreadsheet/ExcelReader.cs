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
                    var customer = new Customer
                    {
                        Name = worksheet.Cells[row, 1].Text, 
                        TaxId = worksheet.Cells[row, 2].Text, 
                        Phone = worksheet.Cells[row, 3].Text, 
                        BranchName = worksheet.Cells[row, 4].Text, 
                        VersionUpdated = worksheet.Cells[row, 5].Text, 
                        UserUpdated = worksheet.Cells[row, 6].Text, 
                        UpdateDate = DateTime.Parse(worksheet.Cells[row, 7].Text), 
                        Notes = worksheet.Cells[row, 8].Text 
                    };
                    customers.Add(customer);
                }
            }
            return customers;
        }

      
    }
}