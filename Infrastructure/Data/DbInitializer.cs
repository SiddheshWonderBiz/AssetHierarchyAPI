using AssetHierarchyAPI.Domain.Models;
using AssetHierarchyAPI.Infrastructure.Data;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext dbContext)
        {
            dbContext.Database.EnsureCreated();

            if (!dbContext.Signals.Any())
            {
                var demosignal = new Signal
                {
                    Name = "demosignal",
                    AssetId = 2,
                    ValueType ="Real",
                    Description= "Demo signal with 2000 values"
                };
                dbContext.Signals.Add(demosignal);
                dbContext.SaveChanges();

                var random = new Random();
                var signalValues = Enumerable.Range(0, 2000)
                                             .Select(i => new SignalValue
                                             {
                                                 SignalId = demosignal.Id,
                                                 Value = random.NextDouble() * 2000
                                             })
                                             .ToList();

                dbContext.SignalValues.AddRange(signalValues);
                dbContext.SaveChanges();
            }
        }
    }
}
