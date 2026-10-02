using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CaseAuth.Api.Data;

// Used only by `dotnet ef migrations add/remove` at design time, so migrations can be
// generated without spinning up the full host or requiring a real connection string.
public class CaseAuthDbContextFactory : IDesignTimeDbContextFactory<CaseAuthDbContext>
{
    public CaseAuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CaseAuthDbContext>();
        optionsBuilder.UseSqlite("Data Source=caseauth.design.db");
        return new CaseAuthDbContext(optionsBuilder.Options);
    }
}
