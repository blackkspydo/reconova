using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Identity;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Identity;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly ControlDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UserService(ControlDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UserDto> GetProfileAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new BusinessRuleException("NOT_AUTHENTICATED", "Not authenticated");
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        return MapToDto(user);
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        if (_currentUser.Role != UserRole.SuperAdmin && user.TenantId != _currentUser.TenantId)
            throw new ForbiddenException("ACCESS_DENIED", "Access denied");

        return MapToDto(user);
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking();

        if (_currentUser.Role != UserRole.SuperAdmin)
            query = query.Where(u => u.TenantId == _currentUser.TenantId);

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(u => u.Email.Contains(request.Search) || u.FirstName.Contains(request.Search) || u.LastName.Contains(request.Search));

        if (request.Role.HasValue)
            query = query.Where(u => u.Role == request.Role.Value);

        if (request.Status.HasValue)
            query = query.Where(u => u.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto> { Items = users.Select(MapToDto).ToList(), TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == emailLower, cancellationToken))
            throw new ConflictException("EMAIL_EXISTS", "Email is already registered");

        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var user = new User
        {
            Email = emailLower,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            Status = UserStatus.Active,
            TenantId = tenantId,
            EmailVerified = true,
            LastPasswordChangeAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        if (_currentUser.Role != UserRole.SuperAdmin && user.TenantId != _currentUser.TenantId)
            throw new ForbiddenException("ACCESS_DENIED", "Access denied");

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.Role.HasValue) user.Role = request.Role.Value;
        if (request.Status.HasValue) user.Status = request.Status.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        if (_currentUser.Role != UserRole.SuperAdmin && user.TenantId != _currentUser.TenantId)
            throw new ForbiddenException("ACCESS_DENIED", "Access denied");

        if (user.Id == _currentUser.UserId)
            throw new BusinessRuleException("SELF_DELETE", "Cannot delete your own account");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static UserDto MapToDto(User user) => new(
        user.Id, user.Email, user.FirstName, user.LastName,
        user.Role, user.Status, user.TenantId,
        user.EmailVerified, user.TwoFactorEnabled,
        user.LastLoginAt, user.CreatedAt
    );
}
