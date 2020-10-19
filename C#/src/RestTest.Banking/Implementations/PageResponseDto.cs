using System;
using System.Collections.Generic;
using System.Text;

namespace RestTest.Banking
{
    public class PageResponseDto
    {
        public int totalCount { get; set; }
        public int page { get; set; }
        public IList<FinancialTransactionDto> transactions { get; set; }
    }
}
