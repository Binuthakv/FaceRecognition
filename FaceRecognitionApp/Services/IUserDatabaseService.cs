using FaceRecognitionApp.Models;

namespace FaceRecognitionApp.Services;

public interface IUserDatabaseService
{
    Task InitializeAsync();
    Task<int> SaveUserAsync(UserRegistration user);
    Task<int> UpdateUserAsync(UserRegistration user);
    Task<int> DeleteUserAsync(UserRegistration user);
    Task<UserRegistration?> GetUserAsync(int id);
    Task<UserRegistration?> GetUserByUserIdAsync(string userId);
    Task<List<UserRegistration>> GetAllUsersAsync();
    Task<bool> UserIdExistsAsync(string userId);
    Task<int> GetUserCountAsync();
}
