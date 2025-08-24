using Microsoft.EntityFrameworkCore;
using SurveyTool.Infrastructure.Data;

namespace SurveyTool.Tests.Support;

public static class TestFixture
{
    public static AppDbContext NewContext(string? dbName = null)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(opts);
    }
}
