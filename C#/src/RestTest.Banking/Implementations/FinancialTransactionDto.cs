using System;
using System.Text.Json.Serialization;

namespace RestTest.Banking
{
    public class FinancialTransactionDto
    {
        public DateTime Date { get; set; }
        public string Ledger { get; set; }
        public double Amount { get; set; }
        public string Company { get; set; }
        public FinancialTransactionDto()
        {
        }
        public FinancialTransactionDto(DateTime date, string ledger, double amount, string company)
        {
            this.Date = date;
            this.Ledger = ledger;
            this.Amount = amount;
            this.Company = company;
        }
    }
}
