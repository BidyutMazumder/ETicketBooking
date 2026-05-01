using System.Diagnostics.CodeAnalysis;

namespace Shared.Kernel.Domain.Abstractions;

public class Result
{
    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException();
        }
        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        Error = error;
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

public class Response<TValue> : Result
{
    private readonly TValue? _value;
    protected internal Response(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }
    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public TValue? Data => _value;

    public static Response<TValue> Success(TValue value) => new(value, true, Error.None);
    public static Response<TValue> Failure(Error error) => new(default, false, error);
    public static Response<TValue> Create(TValue? value) =>
        value is not null ? Success(value) : Failure(Error.NullValue);

    public static implicit operator Response<TValue>(TValue? value) => Create(value);
}

