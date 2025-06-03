using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IntegrationPlatform.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace IntegrationPlatform.Infrastructure.ErrorHandling
{
    public class ErrorHandler
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ErrorHandler> _logger;

        public ErrorHandler(ApplicationDbContext context, ILogger<ErrorHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> LogErrorAsync(Exception ex, string source)
        {
            var errorId = GenerateErrorId();
            var error = new ErrorLog
            {
                ErrorId = errorId,
                Message = ex.Message,
                Source = source,
                StackTrace = ex.StackTrace,
                Timestamp = DateTime.UtcNow
            };

            _context.ErrorLogs.Add(error);
            await _context.SaveChangesAsync();

            _logger.LogError(ex, $"Error {errorId} logged from {source}");

            return errorId;
        }

        private int GenerateErrorId()
        {
            return new Random().Next(1000, 9999);
        }
    }

    public class ErrorLog
    {
        public int Id { get; set; }
        public int ErrorId { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 