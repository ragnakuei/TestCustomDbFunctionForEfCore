using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestCustomDbFunctionForEfCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = DiFactory.GetService<TestDataBaseContext>();
            var customer = context.TableA
                                  .Where(t => EF.Functions.Like(t.Name, "%D%"))
                                  .Select(t => new
                                               {
                                                   Id       = t.Id,
                                                   Name     = t.Name,
                                                   JsonId   = TestDataBaseContext.JsonValue(t.JsonColumn, "$.Id"),
                                                   JsonName = TestDataBaseContext.JsonValue(t.JsonColumn, "$.Name")
                                               })
                                  .FirstOrDefault();

            Console.WriteLine(customer.Id);
            Console.WriteLine(customer.Name);
            Console.WriteLine(customer.JsonId);
            Console.WriteLine(customer.JsonName);
        }
    }

    public static class DiFactory
    {
        private static readonly ServiceProvider _provider;

        static DiFactory()
        {
            var _services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                               .AddJsonFile("appsettings.json", optional: true)
                               .Build();
            _services.AddSingleton(_ => configuration);

            // 加上 Log 至 Console 輸出的功能
            _services.AddLogging(configure => configure.AddConsole());

            _services.AddDbContext<TestDataBaseContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))
                     .AddLogging();

            _provider = _services.BuildServiceProvider();
        }

        public static T GetService<T>()
        {
            return _provider.GetService<T>();
        }
    }


    public class TestDataBaseContext : DbContext
    {
        public TestDataBaseContext(DbContextOptions<TestDataBaseContext> options)
            : base(options)
        {
        }

        public DbSet<TableA> TableA { get; set; }

        public static string JsonValue(string column, [NotParameterized]string path) => throw new NotSupportedException();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TableA>();

            modelBuilder.HasDbFunction(typeof(TestDataBaseContext).GetMethod(nameof(JsonValue)))
                        .HasTranslation(args => SqlFunctionExpression.Create("JSON_VALUE", args, typeof(string), null));
        }
    }

    public class TableA
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string JsonColumn { get; set; }
    }
}
