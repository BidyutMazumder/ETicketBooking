namespace Identity.Domain.Users.ValueObject;

public record FullName(string FirstName, string LastName)
{
    public string FormattedName => $"{FirstName} {LastName}";
}
