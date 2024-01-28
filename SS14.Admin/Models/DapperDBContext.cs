using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Models;

public class DapperDBContext : DbContext
{
    public DapperDBContext(DbContextOptions options) : base(options)
    {
    }
}
