using System.Collections.Generic;
using System.Threading.Tasks;
using Lexus2_0.Desktop.DataAccess;
using Lexus2_0.Desktop.Models;

namespace Lexus2_0.Desktop.Services
{
    public class IrctcAccountService
    {
        private readonly DatabaseContext _dbContext;

        public IrctcAccountService(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IrctcAccount> CreateAccountAsync(IrctcAccount account)
        {
            return await _dbContext.CreateIrctcAccountAsync(account);
        }

        public async Task<List<IrctcAccount>> GetAllAccountsAsync()
        {
            return await _dbContext.GetAllIrctcAccountsAsync();
        }

        public async Task DeleteAccountAsync(int id)
        {
            await _dbContext.DeleteIrctcAccountAsync(id);
        }
    }
}

