# Clean Architecture Setup Guide

A step-by-step guide to set up a new project or reorganize existing code using the BeautyParlour architecture pattern.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Project Structure](#project-structure)
3. [Layer-by-Layer Setup](#layer-by-layer-setup)
4. [Configuration & DI](#configuration--di)
5. [Adding New Features](#adding-new-features)
6. [Reorganizing Existing Code](#reorganizing-existing-code)
7. [Common Patterns & Templates](#common-patterns--templates)

---

## Quick Start

### Initial Project Creation

```bash
# Create solution
dotnet new sln -n YourProjectName

# Create projects
dotnet new classlib -n YourProjectName.Domain -f net8.0
dotnet new classlib -n YourProjectName.Application -f net8.0
dotnet new classlib -n YourProjectName.Infrastructure -f net8.0
dotnet new webapi -n YourProjectName.API -f net8.0

# Create test projects
dotnet new xunit -n YourProjectName.Application.UnitTest -f net8.0
dotnet new xunit -n YourProjectName.Infrastructure.IntegrationTest -f net8.0

# Add to solution
dotnet sln add YourProjectName.Domain
dotnet sln add YourProjectName.Application
dotnet sln add YourProjectName.Infrastructure
dotnet sln add YourProjectName.API
dotnet sln add YourProjectName.Application.UnitTest
dotnet sln add YourProjectName.Infrastructure.IntegrationTest
```

### Project Dependencies

```
API         → depends on Application, Infrastructure
Application → depends on Domain
Infrastructure → depends on Domain
Domain      → no dependencies
Tests       → depend on respective layers
```

---

## Project Structure

### Directory Layout

```
YourProjectName/
├── YourProjectName.sln
├── YourProjectName.API/
│   ├── Controllers/
│   ├── Middleware/
│   ├── ServiceRegistration/
│   ├── Properties/
│   ├── Program.cs
│   ├── GlobalUsing.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── YourProjectName.API.csproj
│   └── Dockerfile
│
├── YourProjectName.Application/
│   ├── Feature/
│   │   ├── Feature1/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   ├── DTOs/
│   │   │   └── Validators/
│   │   └── Feature2/
│   ├── Common/
│   │   ├── Exceptions/
│   │   └── Responses/
│   ├── Contracts/
│   ├── Models/
│   ├── GlobalUsing.cs
│   ├── ApplicationServiceRegistration.cs
│   └── YourProjectName.Application.csproj
│
├── YourProjectName.Domain/
│   ├── Feature1/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Events/
│   │   └── Abstractions/
│   ├── Feature2/
│   ├── SharedKernel/
│   │   ├── Entity.cs
│   │   └── ValueObject.cs
│   ├── GlobalUsing.cs
│   └── YourProjectName.Domain.csproj
│
├── YourProjectName.Infrastructure/
│   ├── Persistence/
│   │   ├── Context/
│   │   │   └── AppDbContext.cs
│   │   ├── Repositories/
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── Infrastructure/
│   │   ├── Services/
│   │   └── External/
│   ├── GlobalUsing.cs
│   ├── InfrastructureServiceRegistration.cs
│   └── YourProjectName.Infrastructure.csproj
│
├── YourProjectName.Application.UnitTest/
│   ├── Features/
│   ├── GlobalUsing.cs
│   └── YourProjectName.Application.UnitTest.csproj
│
└── YourProjectName.Infrastructure.IntegrationTest/
    ├── GlobalUsing.cs
    └── YourProjectName.Infrastructure.IntegrationTest.csproj
```

---

## Layer-by-Layer Setup

### 1. Domain Layer Setup

#### 1.1 NuGet Dependencies

```xml
<!-- YourProjectName.Domain.csproj -->
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>

<!-- No external dependencies needed -->
```

#### 1.2 Global Usings (`GlobalUsing.cs`)

```csharp
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
```

#### 1.3 Base Entity Class (`SharedKernel/Entity.cs`)

```csharp
namespace YourProjectName.Domain.SharedKernel;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    
    public void MarkUpdated(string? modifiedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
    
    public void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        LastModifiedBy = deletedBy;
    }
    
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}

public interface IDomainEvent { }
```

#### 1.4 Value Object Base

```csharp
namespace YourProjectName.Domain.SharedKernel;

public abstract class ValueObject
{
    public abstract IEnumerable<object> GetAtomicValues();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    public override int GetHashCode() =>
        GetAtomicValues()
            .Aggregate(default(int), (hashcode, value) =>
                HashCode.Combine(hashcode, value?.GetHashCode() ?? 0));
}
```

#### 1.5 Example Domain Entity

```csharp
// Domain/Feature/Entities/Feature.cs
namespace YourProjectName.Domain.Feature.Entities;

public class Feature : Entity
{
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Domain events
    public event Action<FeatureCreatedEvent>? OnFeatureCreated;
    
    private Feature() { }
    
    public static Feature Create(string name, string description)
    {
        var feature = new Feature
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
        };
        
        feature.RaiseDomainEvent(new FeatureCreatedEvent(feature.Id, name));
        return feature;
    }
    
    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        MarkUpdated();
    }
}

// Domain/Feature/Events/FeatureCreatedEvent.cs
public record FeatureCreatedEvent(Guid FeatureId, string FeatureName) : IDomainEvent;
```

### 2. Application Layer Setup

#### 2.1 NuGet Dependencies

```xml
<!-- YourProjectName.Application.csproj -->
<ItemGroup>
    <PackageReference Include="MediatR" Version="12.1.1" />
    <PackageReference Include="FluentValidation" Version="11.8.0" />
</ItemGroup>

<ItemGroup>
    <ProjectReference Include="..\YourProjectName.Domain\YourProjectName.Domain.csproj" />
</ItemGroup>
```

#### 2.2 Global Usings

```csharp
// GlobalUsing.cs
global using MediatR;
global using FluentValidation;
global using YourProjectName.Domain.SharedKernel;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
```

#### 2.3 Common Exceptions

```csharp
// Common/Exceptions/NotFoundException.cs
namespace YourProjectName.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Common/Exceptions/ValidationException.cs
public class ValidationException : Exception
{
    public Dictionary<string, List<string>> Errors { get; set; }
    
    public ValidationException(Dictionary<string, List<string>> errors)
    {
        Errors = errors;
    }
}
```

#### 2.4 Common Response Model

```csharp
// Common/Responses/GlobalResponse.cs
namespace YourProjectName.Application.Common.Responses;

public class GlobalResponse<T>
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public bool IsSuccess { get; set; }
    
    public static GlobalResponse<T> SuccessResponse(T? data = default, string message = "Operation successful")
    {
        return new GlobalResponse<T>
        {
            StatusCode = 200,
            Message = message,
            Data = data,
            IsSuccess = true
        };
    }
    
    public static GlobalResponse<T> FailureResponse(string message, int statusCode = 400)
    {
        return new GlobalResponse<T>
        {
            StatusCode = statusCode,
            Message = message,
            IsSuccess = false
        };
    }
}
```

#### 2.5 MediatR Command/Query Base Types

```csharp
// Contracts/ICommand.cs
namespace YourProjectName.Application.Contracts;

public interface ICommand<out TResponse> : IRequest<TResponse> { }

public interface ICommandHandler<in TCommand, TResponse> 
    : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> { }

// Contracts/IQuery.cs
public interface IQuery<out TResponse> : IRequest<TResponse> { }

public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse> { }
```

#### 2.6 Example Command

```csharp
// Feature/FeatureManagement/Commands/CreateFeatureCommand.cs
namespace YourProjectName.Application.Feature.FeatureManagement.Commands;

public sealed record CreateFeatureCommand(
    string Name,
    string Description
) : ICommand<GlobalResponse<Guid>>;

public class CreateFeatureCommandHandler 
    : ICommandHandler<CreateFeatureCommand, GlobalResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateFeatureCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GlobalResponse<Guid>> Handle(
        CreateFeatureCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var feature = Feature.Create(request.Name, request.Description);
            await _unitOfWork.FeatureRepository.AddAsync(feature, cancellationToken);
            
            int changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            if (changes > 0)
                return GlobalResponse<Guid>.SuccessResponse(
                    feature.Id,
                    "Feature created successfully"
                );
            
            return GlobalResponse<Guid>.FailureResponse("Failed to create feature");
        }
        catch (Exception ex)
        {
            return GlobalResponse<Guid>.FailureResponse(ex.Message, 500);
        }
    }
}
```

#### 2.7 Example Validator

```csharp
// Feature/FeatureManagement/Validators/CreateFeatureValidator.cs
namespace YourProjectName.Application.Feature.FeatureManagement.Validators;

public class CreateFeatureValidator : AbstractValidator<CreateFeatureCommand>
{
    public CreateFeatureValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
            
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}
```

#### 2.8 Validation Behavior (Pipeline)

```csharp
// Common/Behaviors/ValidationBehavior.cs
namespace YourProjectName.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );
        
        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .GroupBy(x => x.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToList());
        
        if (failures.Any())
            throw new ValidationException(failures);
        
        return await next();
    }
}
```

#### 2.9 Service Registration

```csharp
// ApplicationServiceRegistration.cs
namespace YourProjectName.Application;

public static class ApplicationServiceRegistration
{
    public static void ConfigureApplicationService(this IServiceCollection services)
    {
        // MediatR
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
        
        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        
        // Pipeline Behaviors
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>)
        );
    }
}
```

### 3. Infrastructure Layer Setup

#### 3.1 NuGet Dependencies

```xml
<!-- YourProjectName.Infrastructure.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
</ItemGroup>

<ItemGroup>
    <ProjectReference Include="..\YourProjectName.Domain\YourProjectName.Domain.csproj" />
</ItemGroup>
```

#### 3.2 Repository Interface

```csharp
// Persistence/Repositories/IRepository.cs
namespace YourProjectName.Infrastructure.Persistence.Repositories;

public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
```

#### 3.3 Generic Repository Implementation

```csharp
// Persistence/Repositories/Repository.cs
namespace YourProjectName.Infrastructure.Persistence.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : Entity
{
    protected readonly AppDbContext _context;
    
    public Repository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TEntity>()
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }
    
    public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<TEntity>()
            .Where(e => !e.IsDeleted)
            .ToListAsync(cancellationToken);
    }
    
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }
    
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _context.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    }
    
    public void Update(TEntity entity)
    {
        _context.Set<TEntity>().Update(entity);
    }
    
    public void Delete(TEntity entity)
    {
        entity.Delete();
        Update(entity);
    }
}
```

#### 3.4 Unit of Work Interface

```csharp
// Persistence/IUnitOfWork.cs
namespace YourProjectName.Infrastructure.Persistence;

public interface IUnitOfWork
{
    IFeatureRepository FeatureRepository { get; }
    // Add other repositories as needed
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

#### 3.5 DbContext

```csharp
// Persistence/Context/AppDbContext.cs
namespace YourProjectName.Infrastructure.Persistence.Context;

public class AppDbContext : DbContext, IUnitOfWork
{
    public DbSet<Feature> Features { get; set; }
    // Add other DbSets as needed
    
    private IFeatureRepository? _featureRepository;
    public IFeatureRepository FeatureRepository =>
        _featureRepository ??= new FeatureRepository(this);
    
    // Add other repository properties
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await PublishDomainEventsAsync();
        return result;
    }
    
    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker.Entries<Entity>()
            .SelectMany(entry => entry.Entity.GetDomainEvents())
            .ToList();
        
        foreach (var entity in ChangeTracker.Entries<Entity>())
            entity.Entity.ClearDomainEvents();
        
        var publisher = ServiceLocator.GetService<IPublisher>();
        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent);
    }
    
    public async Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.BeginTransactionAsync(cancellationToken);
        return true;
    }
    
    public async Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.CommitTransactionAsync(cancellationToken);
        return true;
    }
    
    public async Task<bool> RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.RollbackTransactionAsync(cancellationToken);
        return true;
    }
}
```

#### 3.6 Entity Configuration

```csharp
// Persistence/Configurations/FeatureConfiguration.cs
namespace YourProjectName.Infrastructure.Persistence.Configurations;

public class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Common fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.LastModifiedBy);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.DeletedAt);
        
        // Query filter for soft delete
        builder.HasQueryFilter(e => !e.IsDeleted);
        
        // Specific properties
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(500);
    }
}
```

#### 3.7 Infrastructure Service Registration

```csharp
// InfrastructureServiceRegistration.cs
namespace YourProjectName.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static void ConfigureInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            )
        );
        
        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<AppDbContext>()
        );
        
        // Repositories
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        // Add other repositories as needed
    }
}
```

### 4. API Layer Setup

#### 4.1 NuGet Dependencies

```xml
<!-- YourProjectName.API.csproj -->
<ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
</ItemGroup>

<ItemGroup>
    <ProjectReference Include="..\YourProjectName.Application\YourProjectName.Application.csproj" />
    <ProjectReference Include="..\YourProjectName.Infrastructure\YourProjectName.Infrastructure.csproj" />
</ItemGroup>
```

#### 4.2 Global Exception Middleware

```csharp
// Middleware/GlobalExceptionMiddleware.cs
namespace YourProjectName.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = ex.Errors,
                isSuccess = false
            });
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                GlobalResponse<string>.FailureResponse(ex.Message, 404)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                GlobalResponse<string>.FailureResponse(
                    "Internal server error",
                    500
                )
            );
        }
    }
}
```

#### 4.3 Model Validation Filter

```csharp
// Middleware/ModelValidationFilter.cs
namespace YourProjectName.API.Middleware;

public class ModelValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            var response = GlobalResponse<string>.FailureResponse(
                string.Join(", ", errors)
            );
            
            context.Result = new BadRequestObjectResult(response);
        }
    }
    
    public void OnActionExecuted(ActionExecutedContext context) { }
}
```

#### 4.4 Controller Base Pattern

```csharp
// Controllers/BaseController.cs
namespace YourProjectName.API.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly ISender _sender;
    
    protected BaseController(ISender sender)
    {
        _sender = sender;
    }
}

// Controllers/FeaturesController.cs
namespace YourProjectName.API.Controllers;

public class FeaturesController : BaseController
{
    public FeaturesController(ISender sender) : base(sender) { }
    
    [HttpPost]
    public async Task<IActionResult> CreateFeature(
        [FromBody] CreateFeatureCommand command)
    {
        var result = await _sender.Send(command);
        return StatusCode(result.StatusCode, result);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllFeatures()
    {
        var result = await _sender.Send(new GetAllFeaturesQuery());
        return StatusCode(result.StatusCode, result);
    }
}
```

#### 4.5 Program.cs Setup

```csharp
using Serilog;
using YourProjectName.API.Middleware;
using YourProjectName.Application;
using YourProjectName.Infrastructure;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Services
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ModelValidationFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "YourProjectName API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Dependency Injection Setup
builder.Services.ConfigureApplicationService();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Middleware Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();
```

#### 4.6 Global Usings

```csharp
// GlobalUsing.cs
global using MediatR;
global using YourProjectName.Application.Common.Responses;
global using YourProjectName.Application.Common.Exceptions;
global using YourProjectName.Application.Feature.FeatureManagement.Commands;
global using YourProjectName.Application.Feature.FeatureManagement.Queries;
global using Microsoft.AspNetCore.Mvc;
global using System.Threading;
global using System.Threading.Tasks;
```

---

## Configuration & DI

### Dependency Injection Order

1. **API Layer** configuration (Swagger, Controllers)
2. **Application Layer** configuration (MediatR, Validation)
3. **Infrastructure Layer** configuration (Database, Repositories)

### Service Lifetimes

```csharp
// Scoped (per HTTP request)
services.AddScoped<IUnitOfWork>();
services.AddScoped<IRepository<TEntity>>();
services.AddScoped<CommandHandler>();

// Transient (new each time)
services.AddTransient<CustomService>();

// Singleton (shared across all requests)
services.AddSingleton<IConfiguration>();
```

---

## Adding New Features

### Step-by-Step Process

#### Step 1: Create Domain Entity

```csharp
// Domain/NewFeature/Entities/NewEntity.cs
namespace YourProjectName.Domain.NewFeature.Entities;

public class NewEntity : Entity
{
    public string PropertyName { get; set; }
    
    private NewEntity() { }
    
    public static NewEntity Create(string propertyName)
    {
        return new NewEntity
        {
            PropertyName = propertyName,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

#### Step 2: Create Value Objects (if needed)

```csharp
namespace YourProjectName.Domain.NewFeature.ValueObjects;

public sealed record PropertyValue
{
    public string Value { get; }
    
    public PropertyValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException("Value cannot be null");
        Value = value;
    }
}
```

#### Step 3: Create Domain Events (if needed)

```csharp
namespace YourProjectName.Domain.NewFeature.Events;

public record NewEntityCreatedEvent(Guid EntityId, string PropertyName) : IDomainEvent;
```

#### Step 4: Create Command

```csharp
namespace YourProjectName.Application.Feature.NewFeature.Commands;

public sealed record CreateNewEntityCommand(string PropertyName) 
    : ICommand<GlobalResponse<Guid>>;
```

#### Step 5: Create Command Handler

```csharp
public class CreateNewEntityCommandHandler 
    : ICommandHandler<CreateNewEntityCommand, GlobalResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateNewEntityCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GlobalResponse<Guid>> Handle(
        CreateNewEntityCommand request,
        CancellationToken cancellationToken)
    {
        var entity = NewEntity.Create(request.PropertyName);
        await _unitOfWork.NewEntityRepository.AddAsync(entity, cancellationToken);
        
        var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return changes > 0
            ? GlobalResponse<Guid>.SuccessResponse(entity.Id)
            : GlobalResponse<Guid>.FailureResponse("Failed to create entity");
    }
}
```

#### Step 6: Create Validator

```csharp
namespace YourProjectName.Application.Feature.NewFeature.Validators;

public class CreateNewEntityValidator : AbstractValidator<CreateNewEntityCommand>
{
    public CreateNewEntityValidator()
    {
        RuleFor(x => x.PropertyName)
            .NotEmpty().WithMessage("Property name is required")
            .MaximumLength(100).WithMessage("Property name must not exceed 100 characters");
    }
}
```

#### Step 7: Create Entity Configuration

```csharp
namespace YourProjectName.Infrastructure.Persistence.Configurations;

public class NewEntityConfiguration : IEntityTypeConfiguration<NewEntity>
{
    public void Configure(EntityTypeBuilder<NewEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PropertyName).IsRequired().HasMaxLength(100);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
```

#### Step 8: Create Repository (if specialized operations needed)

```csharp
namespace YourProjectName.Infrastructure.Persistence.Repositories;

public interface INewEntityRepository : IRepository<NewEntity>
{
    Task<NewEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

public class NewEntityRepository : Repository<NewEntity>, INewEntityRepository
{
    public NewEntityRepository(AppDbContext context) : base(context) { }
    
    public async Task<NewEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Set<NewEntity>()
            .FirstOrDefaultAsync(e => e.PropertyName == name && !e.IsDeleted, cancellationToken);
    }
}
```

#### Step 9: Register in UnitOfWork

```csharp
public interface IUnitOfWork
{
    INewEntityRepository NewEntityRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class AppDbContext : DbContext, IUnitOfWork
{
    private INewEntityRepository? _newEntityRepository;
    public INewEntityRepository NewEntityRepository =>
        _newEntityRepository ??= new NewEntityRepository(this);
}
```

#### Step 10: Create Controller

```csharp
public class NewEntitiesController : BaseController
{
    public NewEntitiesController(ISender sender) : base(sender) { }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNewEntityCommand command)
    {
        var result = await _sender.Send(command);
        return StatusCode(result.StatusCode, result);
    }
}
```

#### Step 11: Create Unit Tests

```csharp
namespace YourProjectName.Application.UnitTest.Features;

public class CreateNewEntityCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidData_ReturnsSuccessResponse()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var handler = new CreateNewEntityCommandHandler(mockUnitOfWork.Object);
        var command = new CreateNewEntityCommand("Test Property");
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
    }
}
```

---

## Reorganizing Existing Code

### Migration Strategy

#### Phase 1: Analyze Existing Codebase

```
Identify:
├── Data models/entities
├── Business logic
├── Data access patterns
├── API endpoints
├── External dependencies
├── Cross-cutting concerns
└── Existing services
```

#### Phase 2: Map to Clean Architecture

```
Existing Code → Clean Architecture
├── Models/Entities → Domain/Entities
├── ValueObjects → Domain/ValueObjects
├── Business Logic → Application/Handlers
├── Validation → Application/Validators
├── Repositories → Infrastructure/Repositories
├── DbContext → Infrastructure/Context
├── Controllers → API/Controllers (minimal changes)
└── Services → Application/Services
```

#### Phase 3: Create Domain Layer

1. Copy entities to `Domain/[Feature]/Entities/`
2. Add base `Entity` class to `SharedKernel/`
3. Create value objects if needed
4. Define domain events for significant state changes

#### Phase 4: Create Application Layer

1. Extract business logic to Command/Query handlers
2. Create DTOs matching request/response contracts
3. Move validation logic to FluentValidation validators
4. Create service contracts in `Contracts/`

#### Phase 5: Create Infrastructure Layer

1. Move DbContext to `Persistence/Context/`
2. Create repositories from existing data access code
3. Implement `IUnitOfWork` in DbContext
4. Create entity configurations using Fluent API

#### Phase 6: Update API Layer

1. Inject `ISender` instead of services
2. Replace service calls with `_sender.Send(command)`
3. Simplify controllers to routing + response mapping
4. Add global exception middleware

#### Phase 7: Update Program.cs

1. Add service registration calls
2. Configure middleware pipeline
3. Set up logging
4. Add CORS if needed

#### Example Refactoring

**Before (Monolithic):**
```csharp
public class FeatureController : ControllerBase
{
    private readonly DbContext _db;
    
    [HttpPost]
    public async Task<ActionResult> Create(CreateRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
            return BadRequest("Name required");
        
        var feature = new Feature { Name = request.Name };
        _db.Features.Add(feature);
        await _db.SaveChangesAsync();
        
        return Ok(new { id = feature.Id });
    }
}
```

**After (Clean Architecture):**
```csharp
// Program.cs
builder.Services.ConfigureApplicationService();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);

// Command
public sealed record CreateFeatureCommand(string Name) : ICommand<GlobalResponse<Guid>>;

// Validator
public class CreateFeatureValidator : AbstractValidator<CreateFeatureCommand>
{
    public CreateFeatureValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name required");
    }
}

// Handler
public class CreateFeatureCommandHandler 
    : ICommandHandler<CreateFeatureCommand, GlobalResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<GlobalResponse<Guid>> Handle(
        CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        var feature = Feature.Create(request.Name);
        await _unitOfWork.FeatureRepository.AddAsync(feature, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return GlobalResponse<Guid>.SuccessResponse(feature.Id);
    }
}

// Controller
[Route("api/[controller]/[action]")]
[ApiController]
public class FeaturesController : ControllerBase
{
    private readonly ISender _sender;
    
    public FeaturesController(ISender sender) => _sender = sender;
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeatureCommand command)
    {
        var result = await _sender.Send(command);
        return StatusCode(result.StatusCode, result);
    }
}
```

---

## Common Patterns & Templates

### Command Handler Template

```csharp
public sealed record {FeatureName}Command(/* parameters */) 
    : ICommand<GlobalResponse<{ReturnType}>>;

public class {FeatureName}CommandHandler 
    : ICommandHandler<{FeatureName}Command, GlobalResponse<{ReturnType}>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<{FeatureName}CommandHandler> _logger;
    
    public {FeatureName}CommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<{FeatureName}CommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<GlobalResponse<{ReturnType}>> Handle(
        {FeatureName}Command request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Business logic here
            
            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            return changes > 0
                ? GlobalResponse<{ReturnType}>.SuccessResponse(/* data */)
                : GlobalResponse<{ReturnType}>.FailureResponse("Operation failed");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
            return GlobalResponse<{ReturnType}>.FailureResponse(ex.Message, 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Handler}", nameof({FeatureName}CommandHandler));
            return GlobalResponse<{ReturnType}>.FailureResponse("Internal server error", 500);
        }
    }
}
```

### Query Handler Template

```csharp
public sealed record {FeatureName}Query(/* parameters */) 
    : IQuery<GlobalResponse<{ReturnType}>>;

public class {FeatureName}QueryHandler 
    : IQueryHandler<{FeatureName}Query, GlobalResponse<{ReturnType}>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public {FeatureName}QueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GlobalResponse<{ReturnType}>> Handle(
        {FeatureName}Query request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _unitOfWork.{Repository}
                .GetAsync(/* criteria */, cancellationToken);
            
            return result != null
                ? GlobalResponse<{ReturnType}>.SuccessResponse(result)
                : GlobalResponse<{ReturnType}>.FailureResponse("Not found", 404);
        }
        catch (Exception ex)
        {
            return GlobalResponse<{ReturnType}>.FailureResponse("Error retrieving data", 500);
        }
    }
}
```

### Entity Template

```csharp
public class {EntityName} : Entity
{
    public string {Property1} { get; set; }
    public string {Property2} { get; set; }
    
    private {EntityName}() { }
    
    public static {EntityName} Create(string property1, string property2)
    {
        var entity = new {EntityName}
        {
            {Property1} = property1,
            {Property2} = property2,
            CreatedAt = DateTime.UtcNow
        };
        
        entity.RaiseDomainEvent(new {EntityName}CreatedEvent(entity.Id));
        return entity;
    }
    
    public void Update(string property1, string property2)
    {
        {Property1} = property1;
        {Property2} = property2;
        MarkUpdated();
    }
}
```

---

## Checklist for New Project

- [ ] Create solution structure
- [ ] Setup Domain layer with Entity base class
- [ ] Setup Application layer with MediatR
- [ ] Setup Infrastructure layer with DbContext
- [ ] Setup API layer with controllers
- [ ] Configure Program.cs and dependency injection
- [ ] Add Swagger documentation
- [ ] Setup logging (Serilog)
- [ ] Add middleware (exception handling, validation)
- [ ] Add first feature end-to-end
- [ ] Setup unit tests
- [ ] Setup integration tests
- [ ] Create database migrations
- [ ] Document architecture decisions

---

## Best Practices

1. **Keep Controllers Thin** - Move logic to handlers
2. **Validate Early** - Use FluentValidation at application boundary
3. **Use Domain Events** - Signal important state changes
4. **Leverage Repositories** - Abstract data access
5. **Test Handlers** - Focus unit tests on application logic
6. **Log Appropriately** - Use structured logging for debugging
7. **Keep Entities Pure** - Minimize external dependencies in Domain
8. **Use Value Objects** - Encapsulate domain concepts
9. **Document Decisions** - Record architectural choices
10. **Review Code** - Maintain architecture through reviews

