using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IAccountService
    {
        Task<UserDto?> AuthenticateAsync(string username, string password);
        Task ChangePasswordAsync(int userId, SmartEdu.Shared.DTOs.ChangePasswordDto dto);
        Task<bool> RequiresPasswordChangeAsync(int userId);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);

        Task CreateUserAsync(UserCreateDto dto);
        Task UpdateUserAsync(UserDto dto);

        Task DeleteUserAsync(int id);
        Task<bool> IsUsernameTakenAsync(string username);
    }
}
