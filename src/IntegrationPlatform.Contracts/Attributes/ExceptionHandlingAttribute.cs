using System;

namespace IntegrationPlatform.Contracts.Attributes
{
    /// <summary>
    /// Decorate classes or methods with this attribute to instruct the underlying pipeline to automatically
    /// handle exceptions using a configurable retry policy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ExceptionHandlingAttribute : Attribute
    {
        /// <summary>
        /// The number of retries to attempt when an operation throws an exception.
        /// </summary>
        public int RetryCount { get; }

        /// <summary>
        /// The delay (in milliseconds) between retries.
        /// </summary>
        public int RetryDelayMs { get; }

        /// <summary>
        /// Whether to rethrow the exception after all retries are exhausted.
        /// </summary>
        public bool RethrowAfterRetries { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ExceptionHandlingAttribute"/>.
        /// </summary>
        /// <param name="retryCount">Number of retries. Default is 3.</param>
        /// <param name="retryDelayMs">Delay between retries in milliseconds. Default is 500.</param>
        /// <param name="rethrowAfterRetries">Whether to rethrow the exception after retries are exhausted. Default is true.</param>
        public ExceptionHandlingAttribute(int retryCount = 3, int retryDelayMs = 500, bool rethrowAfterRetries = true)
        {
            RetryCount = retryCount;
            RetryDelayMs = retryDelayMs;
            RethrowAfterRetries = rethrowAfterRetries;
        }
    }
}
