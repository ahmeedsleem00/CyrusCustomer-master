using CyrusCustomer.DAL;
using CyrusCustomer.Domain.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;

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

                    // parsing status as a string and converting it to enum
                    string statusText = worksheet.Cells[row, 14].Text.Trim();
                    CustomerStatus statusEnum = ConvertStringToStatus(statusText);

                    // Parsing decimal values
                    decimal amount1 = 0;
                    decimal.TryParse(worksheet.Cells[row, 17].Text, out amount1); // Assuming column 17 for Amount1

                    decimal amount2 = 0;
                    decimal.TryParse(worksheet.Cells[row, 18].Text, out amount2); // Assuming column 18 for Amount2

                    decimal amount3 = 0;
                    decimal.TryParse(worksheet.Cells[row, 19].Text, out amount3); // Assuming column 19 for Amount3

                    bool collected = false;
                    bool.TryParse(worksheet.Cells[row, 20].Text, out collected); // Assuming column 20 for Collected

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
                        Status = statusEnum,
                        Amount1 = amount1,
                        Amount2 = amount2,
                        Amount3 = amount3,
                        Collected = collected,
                        UpdateDate = DateTime.Parse(worksheet.Cells[row, 15].Text), // Assuming this is the correct column for UpdateDate
                        Notes = worksheet.Cells[row, 16].Text, // Assuming this is the correct column for Notes
                    };
                    customers.Add(customer);
                }
            }
            return customers;
        }

        private static CustomerStatus ConvertStringToStatus(string statusText)
        {
            return statusText switch
            {
                "Yes" => CustomerStatus.Yes,
                "No" => CustomerStatus.No,
                "Pending" => CustomerStatus.Pending,
                "N/A" => CustomerStatus.NA,
                "Non" => CustomerStatus.Non,
            };
        }
    }
}
