using IntegrationPlatform.Contracts.Models;

namespace IntegrationPlatform.Contracts.Interfaces;

public interface IIntegrationService
{
    Task<IntegrationJob> StartJobAsync(string jobType);
    Task<IntegrationJob> GetJobStatusAsync(int jobId);
    Task<IntegrationJob> UpdateJobStatusAsync(int jobId, string status);
    Task<FileTransfer> AddFileTransferAsync(FileTransfer fileTransfer);
    Task<IntegrationLog> AddLogAsync(IntegrationLog log);
} 