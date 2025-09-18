using Application.Interfaces;
using Application.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Infrastructure.Queues
{
    public class CalculationQueue : ICalculationQueue
    {
        private readonly ConcurrentQueue<CalculationJob> _jobs = new();

        public void Enqueue(CalculationJob job) { _jobs.Enqueue(job); }

        public bool TryDequeue(out CalculationJob job)
        {
            return _jobs.TryDequeue(out job);
        }
    }
}
