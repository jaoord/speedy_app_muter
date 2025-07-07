using System;

namespace SpeedyAppMuter.Utils
{
    /// <summary>
    /// Represents the result of an operation that can either succeed or fail
    /// Provides a consistent way to handle errors without exceptions
    /// </summary>
    /// <typeparam name="T">The type of the result value</typeparam>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public string? Error { get; }

        private Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result with a value
        /// </summary>
        /// <param name="value">The result value</param>
        /// <returns>A successful result</returns>
        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null);
        }

        /// <summary>
        /// Creates a failed result with an error message
        /// </summary>
        /// <param name="error">The error message</param>
        /// <returns>A failed result</returns>
        public static Result<T> Failure(string error)
        {
            return new Result<T>(false, default, error);
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        /// <param name="exception">The exception that caused the failure</param>
        /// <returns>A failed result</returns>
        public static Result<T> Failure(Exception exception)
        {
            return new Result<T>(false, default, exception.Message);
        }

        /// <summary>
        /// Transforms the result value using the provided function if successful
        /// </summary>
        /// <typeparam name="TResult">The type of the transformed result</typeparam>
        /// <param name="func">The transformation function</param>
        /// <returns>A new result with the transformed value</returns>
        public Result<TResult> Map<TResult>(Func<T, TResult> func)
        {
            if (IsFailure)
                return Result<TResult>.Failure(Error!);

            try
            {
                return Result<TResult>.Success(func(Value!));
            }
            catch (Exception ex)
            {
                return Result<TResult>.Failure(ex);
            }
        }

        /// <summary>
        /// Chains another operation that returns a Result
        /// </summary>
        /// <typeparam name="TResult">The type of the chained result</typeparam>
        /// <param name="func">The function that returns a Result</param>
        /// <returns>The result of the chained operation</returns>
        public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> func)
        {
            if (IsFailure)
                return Result<TResult>.Failure(Error!);

            try
            {
                return func(Value!);
            }
            catch (Exception ex)
            {
                return Result<TResult>.Failure(ex);
            }
        }

        /// <summary>
        /// Executes an action if the result is successful
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <returns>The original result</returns>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess)
            {
                action(Value!);
            }
            return this;
        }

        /// <summary>
        /// Executes an action if the result is a failure
        /// </summary>
        /// <param name="action">The action to execute with the error message</param>
        /// <returns>The original result</returns>
        public Result<T> OnFailure(Action<string> action)
        {
            if (IsFailure)
            {
                action(Error!);
            }
            return this;
        }

        /// <summary>
        /// Gets the value or throws an exception if the result is a failure
        /// </summary>
        /// <returns>The result value</returns>
        /// <exception cref="InvalidOperationException">Thrown when the result is a failure</exception>
        public T GetValueOrThrow()
        {
            if (IsFailure)
                throw new InvalidOperationException($"Result is a failure: {Error}");

            return Value!;
        }

        /// <summary>
        /// Gets the value or returns the provided default value if the result is a failure
        /// </summary>
        /// <param name="defaultValue">The default value to return on failure</param>
        /// <returns>The result value or the default value</returns>
        public T GetValueOrDefault(T defaultValue)
        {
            return IsSuccess ? Value! : defaultValue;
        }

        public override string ToString()
        {
            return IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
        }
    }

    /// <summary>
    /// Non-generic Result for operations that don't return a value
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? Error { get; }

        private Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        /// <returns>A successful result</returns>
        public static Result Success()
        {
            return new Result(true, null);
        }

        /// <summary>
        /// Creates a failed result with an error message
        /// </summary>
        /// <param name="error">The error message</param>
        /// <returns>A failed result</returns>
        public static Result Failure(string error)
        {
            return new Result(false, error);
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        /// <param name="exception">The exception that caused the failure</param>
        /// <returns>A failed result</returns>
        public static Result Failure(Exception exception)
        {
            return new Result(false, exception.Message);
        }

        /// <summary>
        /// Executes an action if the result is successful
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <returns>The original result</returns>
        public Result OnSuccess(Action action)
        {
            if (IsSuccess)
            {
                action();
            }
            return this;
        }

        /// <summary>
        /// Executes an action if the result is a failure
        /// </summary>
        /// <param name="action">The action to execute with the error message</param>
        /// <returns>The original result</returns>
        public Result OnFailure(Action<string> action)
        {
            if (IsFailure)
            {
                action(Error!);
            }
            return this;
        }

        /// <summary>
        /// Throws an exception if the result is a failure
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the result is a failure</exception>
        public void ThrowIfFailure()
        {
            if (IsFailure)
                throw new InvalidOperationException($"Result is a failure: {Error}");
        }

        public override string ToString()
        {
            return IsSuccess ? "Success" : $"Failure: {Error}";
        }
    }
} 