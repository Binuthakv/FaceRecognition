using System.Net;
using System.Net.Http.Json;
using FaceRecognitionApp.Models;

namespace FaceRecognitionApp.Services;

/// <summary>
/// <see cref="IUserDatabaseService"/> implementation that delegates all
/// persistence operations to the remote REST API (<c>api/users</c>).
/// </summary>
public class ApiUserDatabaseService : IUserDatabaseService
{
    private readonly HttpClient _http;

    public ApiUserDatabaseService(HttpClient http) => _http = http;

    // Nothing to initialise — the API manages its own database.
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task<List<UserRegistration>> GetAllUsersAsync()
        => await _http.GetFromJsonAsync<List<UserRegistration>>("api/users") ?? [];

    public async Task<UserRegistration?> GetUserAsync(int id)
    {
        var response = await _http.GetAsync($"api/users/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserRegistration>();
    }

    public async Task<UserRegistration?> GetUserByUserIdAsync(string userId)
    {
        var response = await _http.GetAsync(
            $"api/users/by-userid/{Uri.EscapeDataString(userId)}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserRegistration>();
    }

    public async Task<bool> UserIdExistsAsync(string userId)
        => await _http.GetFromJsonAsync<bool>(
            $"api/users/exists/{Uri.EscapeDataString(userId)}");

    public async Task<int> GetUserCountAsync()
        => await _http.GetFromJsonAsync<int>("api/users/count");

    public async Task<int> SaveUserAsync(UserRegistration user)
    {
        var response = await _http.PostAsJsonAsync("api/users", user);
        response.EnsureSuccessStatusCode();

        // API returns UserRegistrationResponse with Id, UserId, Name, EmbeddingsExtracted
        var result = await response.Content.ReadFromJsonAsync<UserRegistrationResponse>();
        return result?.Id ?? 0;
    }

    public async Task<int> UpdateUserAsync(UserRegistration user)
    {
        var response = await _http.PutAsJsonAsync($"api/users/{user.Id}", user);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<int> DeleteUserAsync(UserRegistration user)
    {
        var response = await _http.DeleteAsync($"api/users/{user.Id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<AdminLoginResponse?> LoginAdminAsync(string username, string password, bool rememberMe)
    {
        try
        {
            var request = new AdminLoginRequest(username, password, rememberMe);
            var response = await _http.PostAsJsonAsync("api/auth/login", request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AdminLoginResponse>();
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
