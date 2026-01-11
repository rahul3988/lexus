using System.Collections.Generic;
using System.Threading.Tasks;
using Lexus2_0.Desktop.DataAccess;
using Lexus2_0.Desktop.Models;

namespace Lexus2_0.Desktop.Services
{
    public class PaymentOptionService
    {
        private readonly DatabaseContext _dbContext;

        public PaymentOptionService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PaymentOption> CreatePaymentOptionAsync(PaymentOption option)
        {
            return await _dbContext.CreatePaymentOptionAsync(option);
        }

        public async Task<List<PaymentOption>> GetAllPaymentOptionsAsync()
        {
            return await _dbContext.GetAllPaymentOptionsAsync();
        }

        public async Task DeletePaymentOptionAsync(int id)
        {
            await _dbContext.DeletePaymentOptionAsync(id);
        }
    }
}

