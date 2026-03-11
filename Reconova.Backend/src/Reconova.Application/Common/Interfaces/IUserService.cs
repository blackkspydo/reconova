using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Identity;

namespace Reconova.Application.Common.Interfaces;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<UserDto>> GetUsersAsync(UserListRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> GetProfileAsync(CancellationToken cancellationToken = default);
}
