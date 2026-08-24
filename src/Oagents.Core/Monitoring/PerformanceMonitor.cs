using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AI.Agents.Core.Monitoring
{
    public class PerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly Dictionary<string, Stopwatch> _operations;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger;
            _operations = new Dictionary<string, Stopwatch>();
        }

        public void StartOperation(string operationName)
        {
            var watch = new Stopwatch();
            watch.Start();
            _operations[operationName] = watch;
        }

        public TimeSpan EndOperation(string operationName)
        {
            if (_operations.TryGetValue(operationName, out var watch))
            {
                watch.Stop();
                _operations.Remove(operationName);
                
                var duration = watch.Elapsed;
                _logger.LogInformation($"Operation {operationName} completed in {duration.TotalMilliseconds}ms");
                
                return duration;
            }
            
            _logger.LogWarning($"No operation found with name: {operationName}");
            return TimeSpan.Zero;
        }

        public Dictionary<string, TimeSpan> GetActiveOperations()
        {
            return _operations.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Elapsed
            );
        }
    }
} 