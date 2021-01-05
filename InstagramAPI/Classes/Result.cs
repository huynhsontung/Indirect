using System;

namespace InstagramAPI.Classes
{
    public enum ResultStatus
    {
        Succeeded,
        Failed,
        ExceptionCaught
    }

    public class Result
    {
        public bool IsSucceeded => Status == ResultStatus.Succeeded;

        public ResultStatus Status { get; }

        public string Json { get; }

        public string Message { get; }
        
        public Exception Exception { get; }

        public Result(ResultStatus status, string message = null, string json = null)
        {
            Status = status;
            Json = json;
            Message = message;
        }

        public Result(Exception exception, string message = null, string json = null)
        {
            Status = ResultStatus.ExceptionCaught;
            Exception = exception;
            Message = string.IsNullOrEmpty(message) ? exception?.Message : message;
            Json = json;
        }
    }

    public class Result<T> : Result
    {
        public T Value { get; }

        public Result(ResultStatus status, T passingValue, string message = null, string json = null) 
            : base(status, message, json)
        {
            Value = passingValue;
        }

        public Result(Exception exception, T passingValue, string message = null, string json = null) 
            : base(exception, message, json)
        {
            Value = passingValue;
        }

        public static Result<T> Success(T passingValue, string json = null, string message = null)
        {
            return new Result<T>(ResultStatus.Succeeded, passingValue, message, json);
        }

        public static Result<T> Fail(T passingValue, string message = null, string json = null)
        {
            return new Result<T>(ResultStatus.Failed, passingValue, message, json);
        }

        public static Result<T> Fail(string json, string message = null)
        {
            return new Result<T>(ResultStatus.Failed, default, message, json);
        }

        public static Result<T> Except(Exception exception, string message = null, string json = null)
        {
            if (string.IsNullOrEmpty(message)) message = exception.Message;
            return new Result<T>(exception, default, message, json);
        }

        public static Result<T> Except(Exception exception, T passingValue, string message = null, string json = null)
        {
            if (string.IsNullOrEmpty(message)) message = exception.Message;
            return new Result<T>(exception, passingValue, message, json);
        }
    }
}
