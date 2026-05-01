using Shared.Kernel.Domain.Exceptions;
namespace Identity.Domain.Users.ValueObject;

public record Email
{
    public string Value { get; init; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains("@"))
            throw new DomainException("Invalid email format.");

        return new Email(value);
    }
}