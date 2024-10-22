using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.IRepositories;
using Verify.Domain.Entities;

namespace Verify.Infrastructure.Implementations.Repositories;
internal sealed class AccountRepository : IBaseRepository<Account>, IAccountRepository
{
    public AccountRepository()
    {
            
    }

    public Task<Account> CreateAsync(Account entity)
    {
        throw new NotImplementedException();
    }

    public Task<Account> DeleteAsync(Account entity)
    {
        throw new NotImplementedException();
    }

    public IQueryable<Account> FindAll()
    {
        throw new NotImplementedException();
    }

    public IQueryable<Account> FindByCondition(Expression<Func<Account, bool>> expression)
    {
        throw new NotImplementedException();
    }

    public Task<Account?> FindByIdAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<Account> UpdateAsync(Account entity)
    {
        throw new NotImplementedException();
    }
}
