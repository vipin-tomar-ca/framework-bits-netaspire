namespace IntegrationPlatform.Contracts.Interfaces;

public interface ISftpService
{
    /// <summary>
    /// Establishes a connection to the SFTP server
    /// </summary>
    /// <param name="host">SFTP server hostname</param>
    /// <param name="port">SFTP server port</param>
    /// <param name="username">SFTP username</param>
    /// <param name="certificatePath">Path to the certificate file</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> ConnectAsync(string host, int port, string username, string certificatePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the service is currently connected to an SFTP server
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Uploads a file to the SFTP server
    /// </summary>
    /// <param name="localPath">Local file path</param>
    /// <param name="remotePath">Remote file path</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if upload is successful</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to SFTP server</exception>
    Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the SFTP server
    /// </summary>
    /// <param name="remotePath">Remote file path</param>
    /// <param name="localPath">Local file path</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if download is successful</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to SFTP server</exception>
    Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the SFTP server
    /// </summary>
    /// <param name="remotePath">Remote file path</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if deletion is successful</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to SFTP server</exception>
    Task<bool> DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a remote directory
    /// </summary>
    /// <param name="remotePath">Remote directory path</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>List of file names</returns>
    /// <exception cref="InvalidOperationException">Thrown when not connected to SFTP server</exception>
    Task<List<string>> ListFilesAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the SFTP server
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
} 