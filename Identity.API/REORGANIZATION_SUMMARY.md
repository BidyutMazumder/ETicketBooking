# Identity Service Reorganization - Clean Architecture Implementation

## Overview

The Identity service has been successfully reorganized to follow the Clean Architecture pattern using MediatR for CQRS (Command Query Responsibility Segregation). This document outlines the changes made and the new structure.

---

## Major Changes

### 1. **MediatR Integration**
- **Replaced**: Custom `SimpleMediator` implementation
- **Added**: `MediatR` version 12.2.1 package
- **Benefit**: Industry-standard CQRS pattern with built-in pipeline behaviors and handler discovery

### 2. **Project Structure Reorganization**

#### **Before**
```
Identity.Application/
├── Commands/
│   ├── UserCommands.cs (all commands in one file)
│   └── CommandHandlers/ (separate handlers)
├── Queries/
│   ├── UserQueries.cs (all queries in one file)
│   └── QueryHandlers/ (separate handlers)
├── Interfaces/
│   └── IUserRepository.cs
├── Mediator/
│   └── SimpleMediator.cs
└── Mappings/
    └── UserMapper.cs
```

#### **After**
```
Identity.Application/
├── Features/
│   └── Users/
│       ├── Commands/
│       │   ├── CreateUser/
│       │   │   ├── CreateUserCommand.cs
│       │   │   ├── CreateUserCommandHandler.cs
│       │   │   └── CreateUserValidator.cs
│       │   ├── UpdateUser/
│       │   │   ├── UpdateUserCommand.cs
│       │   │   └── UpdateUserCommandHandler.cs
│       │   └── DeleteUser/
│       │       ├── DeleteUserCommand.cs
│       │       └── DeleteUserCommandHandler.cs
│       └── Queries/
│           ├── UserQueries.cs
│           ├── GetUserById/
│           │   └── GetUserByIdQueryHandler.cs
│           ├── GetUserByEmail/
│           │   └── GetUserByEmailQueryHandler.cs
│           └── GetAllUsers/
│               └── GetAllUsersQueryHandler.cs
├── Common/
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   └── ValidationException.cs
│   ├── Interfaces/
│   │   └── IUserRepository.cs
│   ├── Mappings/
│   │   ├── IUserMapper.cs
│   │   └── UserMapper.cs
│   └── Responses/
│       └── PagedResponse.cs
└── ServiceRegistration/
    └── ApplicationServiceRegistration.cs
```

---

## Layer-by-Layer Changes

### **Domain Layer (Identity.Domain)**
- ✅ No changes to domain entities or value objects
- ✅ Uses existing `Shared.Kernel` base classes
- Domain events preserved and functional

### **Application Layer (Identity.Application)**

#### **NuGet Changes**
- ✅ Added: `MediatR` 12.2.1
- ✅ Already had: `FluentValidation` 11.11.0

#### **Key Additions**
- **Common/Exceptions/**
  - `NotFoundException` - For resource not found scenarios
  - `ValidationException` - For validation failures

- **Common/Behaviors/**
  - `ValidationBehavior<TRequest, TResponse>` - MediatR pipeline behavior that automatically validates requests

- **Common/Responses/**
  - `PagedResponse<T>` - Supports pagination responses

- **Features/Users/**
  - Organized commands and queries by feature area
  - Each command/query in its own directory with handler and validator
  - Single responsibility principle applied

#### **Removed Files**
- `Mediator/SimpleMediator.cs` - Replaced by MediatR
- Old `Commands/UserCommands.cs` - Split into feature directories
- Old `Queries/UserQueries.cs` - Split into feature directories
- Old `CommandHandlers/` and `QueryHandlers/` - Moved to feature structure
- Old `Interfaces/IUserRepository.cs` - Moved to Common/Interfaces

### **Infrastructure Layer (Identity.Infrastructure)**

#### **Changes**
- ✅ Updated repository to use new interface location: `Identity.Application.Common.Interfaces`
- ✅ Service registration updated for MediatR
- Updated namespace imports in service registration

### **API Layer (Identity.API)**

#### **Controller Changes**
- **Before**: Injected custom `IMediator`, used `SendCommand<>()` and `SendQuery<>()`
- **After**: Injected MediatR's `ISender`, uses simple `Send()` method

```csharp
// Before
var result = await _mediator.SendCommand<CreateUserCommand, Response<UserDto>>(command, cancellationToken);

// After
var result = await _sender.Send(command, cancellationToken);
```

#### **Middleware Added**
- **GlobalExceptionMiddleware** - Centralized exception handling for:
  - `ValidationException` - Returns 400 with validation errors
  - `NotFoundException` - Returns 404
  - General exceptions - Returns 500 with generic error message

#### **Program.cs Updates**
- Added `GlobalExceptionMiddleware` to pipeline
- Updated service registration calls
- Swagger configuration retained

---

## Service Registration Flow

### **ApplicationServiceRegistration.cs**
```csharp
1. AddMediatR() - Registers all handlers from assembly
2. AddTransient(IPipelineBehavior, ValidationBehavior) - Adds validation pipeline
3. AddValidatorsFromAssembly() - Registers FluentValidation validators
4. AddScoped(IUserMapper, UserMapper) - Registers mapper
```

### **InfrastructureServiceRegistration.cs**
```csharp
1. AddDbContext<IdentityDbContext>()
2. AddScoped<IUserRepository, UserRepository>()
3. AddScoped<IPasswordHasher, PasswordHasher>()
```

### **Program.cs**
```csharp
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(configuration);
```

---

## CQRS Flow Example: CreateUser

### **1. Request Flow**
```
API Controller
    ↓
ISender.Send(CreateUserCommand)
    ↓
MediatR Pipeline
    ├→ ValidationBehavior (validates command)
    ├→ CreateUserCommandHandler
    │   ├→ Check if email exists
    │   ├→ Create domain entity
    │   ├→ Save to repository
    │   └→ Return Response<UserDto>
```

### **2. Response Format**
```csharp
Response<UserDto>
{
    IsSuccess: true,
    StatusCode: 200,
    Message: "Operation successful",
    Data: UserDto { ... }
}
```

---

## Validation Pipeline

### **How It Works**
1. Command/Query is sent via MediatR
2. `ValidationBehavior` intercepts the request
3. Runs all registered validators for the command type
4. If validation fails, throws `ValidationException`
5. If validation passes, proceeds to handler

### **Example: CreateUserValidator**
```csharp
public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        // ... more rules
    }
}
```

---

## Global Exception Handling

The `GlobalExceptionMiddleware` handles all unhandled exceptions:

```csharp
catch (ValidationException ex)
    → 400 Bad Request with errors dictionary

catch (NotFoundException ex)
    → 404 Not Found with error code and message

catch (Exception ex)
    → 500 Internal Server Error with generic message
```

---

## Testing Migration Guide

### **Command Handler Testing**
```csharp
[Fact]
public async Task CreateUserCommand_WithValidData_ReturnsSuccess()
{
    // Arrange
    var command = new CreateUserCommand("email@test.com", "John", "Doe", "Password123", "User");
    var handler = new CreateUserCommandHandler(mockRepo, mockHasher, mockMapper);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
}
```

### **Validator Testing**
```csharp
[Fact]
public void CreateUserValidator_WithInvalidEmail_FailsValidation()
{
    var validator = new CreateUserValidator();
    var command = new CreateUserCommand("invalid", "John", "Doe", "Pass", "User");

    var result = validator.Validate(command);
    Assert.False(result.IsValid);
}
```

---

## Benefits of This Reorganization

### **1. Scalability**
- ✅ Feature-based directory structure makes adding new features easier
- ✅ Each command/query is isolated

### **2. Maintainability**
- ✅ Related handler and validator together in one directory
- ✅ Clear separation of concerns

### **3. Testability**
- ✅ Handlers are isolated and easy to unit test
- ✅ Validators are independently testable
- ✅ Pipeline behavior can be tested separately

### **4. Industry Standards**
- ✅ Uses standard MediatR implementation
- ✅ CQRS pattern properly implemented
- ✅ Aligns with clean architecture principles

### **5. Extensibility**
- ✅ Easy to add new pipeline behaviors (logging, caching, etc.)
- ✅ Easy to add new validators
- ✅ Easy to add new commands/queries

---

## Migration from Old Code

### **If you have custom handlers:**
```csharp
// Old implementation
public class CustomCommandHandler : ICommandHandler<CustomCommand, Response<ResultDto>>
{
    public async Task<Response<ResultDto>> Handle(CustomCommand request, CancellationToken cancellationToken)
    { ... }
}

// New implementation
public class CustomCommandHandler : IRequestHandler<CustomCommand, Response<ResultDto>>
{
    public async Task<Response<ResultDto>> Handle(CustomCommand request, CancellationToken cancellationToken)
    { ... }
}
```

Key changes:
- Inherit from `IRequestHandler<,>` instead of `ICommandHandler<,>`/`IQueryHandler<,>`
- Method signature remains the same
- MediatR will auto-discover and register

---

## Compatibility Notes

- ✅ All existing endpoints work without change
- ✅ Request/Response formats unchanged
- ✅ Database layer unchanged
- ✅ Domain entities unchanged
- ✅ Shared.Kernel unchanged
- ✅ .NET 10 compatibility maintained

---

## Next Steps

1. **Add Unit Tests**: Create test projects for handlers and validators
2. **Add Logging**: Implement logging behavior in pipeline
3. **Add Caching**: Add caching behavior if needed
4. **Add Authentication**: Extend with JWT handling if needed
5. **Add Authorization**: Add authorization policy behaviors

---

## References

- MediatR Documentation: https://github.com/jbogard/MediatR
- Clean Architecture: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- CQRS Pattern: https://martinfowler.com/bliki/CQRS.html
- FluentValidation: https://docs.fluentvalidation.net/

