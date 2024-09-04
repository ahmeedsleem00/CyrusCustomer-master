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
                if (worksheet == null) { throw new Exception("No worksheet found"); }

                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    // Parsing long values
                    long contractorPhoneNumber = 0;
                    long.TryParse(worksheet.Cells[row, 7].Text, out contractorPhoneNumber);

                    long internalAccountantPhone = 0;
                    long.TryParse(worksheet.Cells[row, 11].Text, out internalAccountantPhone);

                    long charteredAccountantPhone = 0;
                    long.TryParse(worksheet.Cells[row, 13].Text, out charteredAccountantPhone);

                    bool status = false;
                    bool.TryParse(worksheet.Cells[row, 14].Text, out status);

                    //int countOfBranches = 1; // Default to 1
                    //int.TryParse(worksheet.Cells[row, 5].Text, out countOfBranches);

                    var customer = new Customer
                    {
                        Month = worksheet.Cells[row, 1].Text,
                        Year = worksheet.Cells[row, 2].Text,
                        TaxId = worksheet.Cells[row, 3].Text,
                        Name = worksheet.Cells[row, 4].Text,
                        CountOfBranches = worksheet.Cells[row, 5].Text,
                        Contractor = worksheet.Cells[row, 6].Text,
                        ContractorPhoneNumber = contractorPhoneNumber,
                        ResponsiblePerson = worksheet.Cells[row, 8].Text,
                        Phone = worksheet.Cells[row, 9].Text,
                        InternalAccountant = worksheet.Cells[row, 10].Text,
                        InternalAccountantPhone = internalAccountantPhone,
                        CharteredAccountant = worksheet.Cells[row, 12].Text,
                        CharteredAccountantPhone = charteredAccountantPhone,
                        Status = status,
                        UpdateDate = DateTime.Parse(worksheet.Cells[row, 15].Text), // Assuming this is the correct column for UpdateDate
                        Notes = worksheet.Cells[row, 16].Text  // Assuming this is the correct column for Notes
                    };
                    customers.Add(customer);
                }
            }
            return customers;
        }


    }
}