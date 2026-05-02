# Quick Reference Guide

## 🎯 Project Architecture Overview

### Clean Program.cs
```csharp
// ✅ Organized by layer
ServiceRegistration.ConfigureServiceRegistration(builder.Services, builder.Configuration);
builder.Services.AddApplicationServices();          // Application layer
builder.Services.AddInfrastructureServices(config);  // Infrastructure layer
```

### Service Registration Pattern

**Application Layer** (`ApplicationServiceRegistration.cs`)
- Mediator (custom implementation)
- Mappers
- Command/Query Handlers
- Validators

**Infrastructure Layer** (`InfrastructureServiceRegistration.cs`)
- DbContext
- Repositories
- Infrastructure services (PasswordHasher, etc.)

**API Layer** (`ServiceRegistration.cs`)
- Swagger/OpenAPI
- Authentication
- API-specific middleware

---

## 📋 How to Use

### 1. **Creating a New Command**

```csharp
// 1. Define the command (implement ICommand<TResponse>)
public sealed record CreateProductCommand(string Name, decimal Price) 
    : ICommand<Response<ProductDto>>;

// 2. Create a handler (implement ICommandHandler<TCommand, TResponse>)
public interface ICreateProductHandler 
    : ICommandHandler<CreateProductCommand, Response<ProductDto>> { }

public sealed class CreateProductHandler : ICreateProductHandler
{
    public async Task<Response<ProductDto>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // Implementation
        var product = Product.Create(request.Name, request.Price);
        await _repository.AddAsync(product, cancellationToken);
        return Response<ProductDto>.Success(_mapper.MapToDto(product));
    }
}

// 3. Register in ApplicationServiceRegistration
services.AddScoped<ICreateProductHandler, CreateProductHandler>();

// 4. Use in controller
var result = await _mediator.SendCommand<CreateProductCommand, Response<ProductDto>>(
    new CreateProductCommand(name, price),
    cancellationToken);
```

### 2. **Creating a New Query**

```csharp
// 1. Define the query (implement IQuery<TResponse>)
public sealed record GetProductByIdQuery(Guid Id) : IQuery<Response<ProductDto>>;

// 2. Create a handler (implement IQueryHandler<TQuery, TResponse>)
public interface IGetProductByIdHandler 
    : IQueryHandler<GetProductByIdQuery, Response<ProductDto>> { }

public sealed class GetProductByIdHandler : IGetProductByIdHandler
{
    public async Task<Response<ProductDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Response<ProductDto>.Failure(
                new Error("Error.NotFound", "Product not found"));

        return Response<ProductDto>.Success(_mapper.MapToDto(product));
    }
}

// 3. Register in ApplicationServiceRegistration
services.AddScoped<IGetProductByIdHandler, GetProductByIdHandler>();

// 4. Use in controller
var result = await _mediator.SendQuery<GetProductByIdQuery, Response<ProductDto>>(
    new GetProductByIdQuery(id),
    cancellationToken);
```

### 3. **Creating a New Entity Configuration**

```csharp
// 1. Create configuration class (implement IEntityTypeConfiguration<TEntity>)
public sealed class ProductEntityConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Price)
            .HasPrecision(10, 2);

        builder.OwnsOne(e => e.Category, nav =>
        {
            nav.Property(c => c.Name).HasColumnName("CategoryName");
        });
    }
}

// 2. DbContext automatically applies all configurations
// modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
```

---

## 🔄 Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      API Controller                              │
│                  UsersController.cs                              │
└────────────────────────────┬────────────────────────────────────┘
                             │
                    ┌────────▼────────┐
                    │    IMediator    │
                    │ (SimpleMediator)│
                    └────────┬────────┘
                             │
                ┌────────────┴────────────┐
                │                         │
        ┌───────▼────────┐      ┌────────▼────────┐
        │ CommandHandlers│      │ QueryHandlers   │
        │                │      │                 │
        │ - Create User  │      │ - Get By Id     │
        │ - Update User  │      │ - Get By Email  │
        │ - Delete User  │      │ - Get All Users │
        └───────┬────────┘      └────────┬────────┘
                │                        │
                └────────────┬───────────┘
                             │
                    ┌────────▼────────┐
                    │ IUserRepository │
                    │  (Persistence)  │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │ DbContext       │
                    │ (EF Core)       │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │   Database      │
                    │   (SQL Server)  │
                    └─────────────────┘
```

---

## 📊 Service Registration Checklist

When adding new services:

- [ ] Define the interface
- [ ] Implement the interface
- [ ] Register in appropriate service registration file
  - [ ] Application layer services → `ApplicationServiceRegistration`
  - [ ] Infrastructure services → `InfrastructureServiceRegistration`
  - [ ] API services → `ServiceRegistration`
- [ ] Verify dependency injection graph (no circular dependencies)
- [ ] Add to GlobalUsing if used across multiple files
- [ ] Test the service in integration tests

---

## 🐛 Debugging

### Mediator Handler Not Found
```
InvalidOperationException: No handler registered for command CreateUserCommand
```

**Solution:** Verify handler is registered in `ApplicationServiceRegistration`

### Mapping Issues
```
NullReferenceException in UserMapper.MapToDto
```

**Solution:** Check if all required properties are correctly mapped

### Database Configuration Missing
```
ArgumentException: Entity type 'User' does not exist in metadata
```

**Solution:** Ensure `UserEntityConfiguration` is in the same assembly as `DbContext` and implements `IEntityTypeConfiguration<User>`

---

## 📦 Project Dependencies

### Identity.Application
- FluentValidation 11.11.0
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0
- Shared.Kernel
- Identity.Domain

### Identity.Infrastructure
- Microsoft.EntityFrameworkCore 10.0.6
- Microsoft.EntityFrameworkCore.SqlServer 10.0.6
- Microsoft.EntityFrameworkCore.Design 10.0.6
- BCrypt.Net-Next 4.0.3
- Identity.Application
- Identity.Domain
- Shared.Kernel

### Identity.API
- Microsoft.AspNetCore.OpenApi 10.0.6
- Swashbuckle.AspNetCore 6.9.0
- Identity.Application
- Identity.Domain
- Identity.Infrastructure

---

## 🚀 Performance Considerations

### Custom Mediator
- **Reflection overhead**: Minimal, handlers cached by service provider
- **No pipelines**: Direct handler invocation (faster for simple handlers)
- **Scalability**: Use factory pattern if handler count > 100

### Manual Mapping
- **No reflection**: Direct property assignment (faster than AutoMapper)
- **Memory efficiency**: No cache overhead
- **Type safety**: Compile-time checking

### Entity Configuration
- **Lazy loading**: DbContext uses `ApplyConfigurationsFromAssembly` (one-time cost)
- **No reflection penalty**: Configuration applied once at startup

---

## ✅ Best Practices

1. **Always use interfaces** for dependency injection
2. **Register services in correct layer** service registration file
3. **Keep handlers focused** - single responsibility
4. **Use explicit mapping** - easier to debug
5. **Validate in handlers** - before business logic
6. **Log errors** - for debugging and monitoring
7. **Handle cancellation tokens** - for graceful shutdown
8. **Use GlobalUsing** - to reduce redundant imports

---

## 🔗 Related Files

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point and service configuration orchestration |
| `ApplicationServiceRegistration.cs` | Application layer DI setup |
| `InfrastructureServiceRegistration.cs` | Infrastructure layer DI setup |
| `SimpleMediator.cs` | Custom command/query dispatcher |
| `UserMapper.cs` | Manual Entity-to-DTO mapping |
| `ICommandHandlers.cs` | Command handler interfaces |
| `IQueryHandlers.cs` | Query handler interfaces |
| `UserEntityConfiguration.cs` | Entity mapping configuration |
| `IdentityDbContext.cs` | EF Core context |

---

## 📞 Support

For issues or questions:
1. Check the error message in build output
2. Verify service registration
3. Check handler interface implementation
4. Review REFACTORING_SUMMARY.md for detailed information
5. Check Git history for recent changes

---

**Last Updated:** 2024
**Status:** ✅ Production Ready
