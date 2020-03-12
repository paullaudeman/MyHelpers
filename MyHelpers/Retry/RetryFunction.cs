using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace MyHelpers.Retry
{
    /// <summary>
    /// Helper class to assist in executing method calls subsequent times if failures occur
    /// </summary>
    public static class RetryFunction
    {
        /// <summary>
        /// Retries a method for n number of tries indicated by <see name="retryCount"/> pausing for <see name="retryIntervalMilliseconds"/> milliseconds between each attempt.
        /// </summary>
        public static void ExecuteAction(Action action, int retryCount, int retryIntervalMilliseconds, Action<RetryFunctionResult> onRetryFailure, Action<RetryFunctionResult> onRetryCountExceededAction)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var count = 0;

            while (count < retryCount)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception e)
                {
                    onRetryFailure?.Invoke(new RetryFunctionResult
                    {
                        Exception = e,
                        NumberOfTimesRetried = count + 1,
                        RetryIntervalMilliseconds = retryIntervalMilliseconds
                    });

                    Thread.Sleep(retryIntervalMilliseconds);

                    count++;
                }
            }

            if (onRetryCountExceededAction != null)
            {
                var results = new RetryFunctionResult
                {
                    NumberOfTimesRetried = retryCount,
                    RetryIntervalMilliseconds = retryIntervalMilliseconds
                };

                onRetryCountExceededAction(results);
            }
        }

        /// <summary>
        /// Retries a method for n number of tries indicated by <see name="retryCount"/> pausing for <see name="retryIntervalMilliseconds"/> milliseconds between each attempt.
        /// </summary>
        /// <param name="methodToCall">Name of the method to call</param>
        /// <param name="instance">Instance of the class containing the method</param>
        /// <param name="retryCount">Number of attempts to try</param>
        /// <param name="retryIntervalMilliseconds">Number of milliseconds to wait between each attempt</param>
        /// <param name="methodArgs">Any optional method arguments</param>
        /// <exception cref="RetryCountExceededException">Thrown when the specified retry count threshold exceeded</exception>
        /// <exception cref="ArgumentNullException">If the method to call is null</exception>
        /// <exception cref="ArgumentNullException">If the instance to invoke upon is null</exception>
        /// <returns>Value returned from method execution</returns>
        public static object Execute(MethodInfo methodToCall, object instance, int retryCount, int retryIntervalMilliseconds, params object[] methodArgs)
        {
            if (methodToCall == null)
                throw new ArgumentNullException(nameof(methodToCall));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var count = 0;

            while (count < retryCount)
            {
                try
                {
                    return methodToCall.Invoke(instance, methodArgs);
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Retry error: " + e.Message);

                    Thread.Sleep(retryIntervalMilliseconds);

                    count++;
                }
            }

            throw new RetryCountExceededException("Unable to perform action; retry count reached.", retryCount);
        }


    }

    public class RetryCountExceededException : Exception
    {
        public int RetryCount { get; }

        public RetryCountExceededException(string message, int retryCount)
            : base(message)
        {
            this.RetryCount = retryCount;
        }
    }

    public class RetryFunctionResult
    {
        public int NumberOfTimesRetried { get; set; }
        public int RetryIntervalMilliseconds { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            return
                $"NumberOfTimesRetried: {NumberOfTimesRetried}, RetryIntervalMilliseconds: {RetryIntervalMilliseconds}, Exception: {Exception}";
        }
    }
}
