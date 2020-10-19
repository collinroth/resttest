using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestTest.Banking
{
    public interface IBankDataProviderOfSinglePageREST
    {
        Task<PageResponseDto> GetPageOfTransactions(int pageNumber, CancellationToken cancellationToken);
    }
}
