using SmartEdu.Business.Interfaces;
using SmartEdu.DataAccess.EntityModels;
using SmartEdu.DataAccess.Repositories;
using SmartEdu.Shared.DTOs;

namespace SmartEdu.Business.Services
{
    public class AccountService : IAccountService
    {
        private readonly IRepository<User> _userRepo;

        public AccountService(IRepository<User> userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<bool> RequiresPasswordChangeAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || user.IsDeleted) return false;
            return user.RequirePasswordChange;
        }

        public async Task ChangePasswordAsync(int userId, SmartEdu.Shared.DTOs.ChangePasswordDto dto)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");

            if (dto.NewPassword.Length < 8)
                throw new InvalidOperationException("Mật khẩu mới tối thiểu 8 ký tự.");

            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu hiện tại.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.RequirePasswordChange = false;

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task<UserDto?> AuthenticateAsync(string username, string password)
        {
            var users = await _userRepo.GetAllAsync(u => u.Username == username && !u.IsDeleted);
            var user = users.FirstOrDefault();

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Role = user.Role
                };
            }
            return null;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepo.GetAllAsync(u => !u.IsDeleted);
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role
            });
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null || user.IsDeleted) return null;
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        public async Task CreateUserAsync(UserCreateDto dto)
        {
            if (await IsUsernameTakenAsync(dto.Username))
                throw new InvalidOperationException("Tên đăng nhập này đã tồn tại.");

            var user = new User
            {
                Username = dto.Username,
                FullName = dto.FullName,
                Role = dto.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsDeleted = false
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(UserDto dto)
        {
            var user = await _userRepo.GetByIdAsync(dto.Id);
            if (user == null || user.IsDeleted)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");

            user.FullName = dto.FullName;
            user.Role = dto.Role;

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user != null && !user.IsDeleted)
            {
                user.IsDeleted = true;
                _userRepo.Update(user);
                await _userRepo.SaveChangesAsync();
            }
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            var users = await _userRepo.GetAllAsync(u => u.Username == username && !u.IsDeleted);
            return users.Any();
        }
    }
}
