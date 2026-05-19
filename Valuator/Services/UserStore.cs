using System.Text.Json;
using Valuator.Models;

namespace Valuator.Services;

public class UserStore
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public UserStore(IWebHostEnvironment environment, IConfiguration configuration)
    {
        string configuredPath = configuration.GetValue<string>("Users:FilePath")
            ?? Path.Combine("App_Data", "users.json");

        _filePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        string? directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    public bool TryCreateUser(string login, string password, out string error)
    {
        login = login.Trim();

        if (string.IsNullOrWhiteSpace(login))
        {
            error = "Логин не может быть пустым";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            error = "Пароль не может быть пустым";
            return false;
        }

        lock (_lock)
        {
            List<UserAccount> users = LoadUsers();

            if (users.Any(user => string.Equals(user.Login, login, StringComparison.OrdinalIgnoreCase)))
            {
                error = "Пользователь с таким логином уже существует";
                return false;
            }

            var passwordData = PasswordHasher.HashPassword(password);

            users.Add(new UserAccount
            {
                Login = login,
                PasswordHash = passwordData.Hash,
                PasswordSalt = passwordData.Salt
            });

            SaveUsers(users);
        }

        error = string.Empty;
        return true;
    }

    public bool ValidateUser(string login, string password)
    {
        login = login.Trim();

        lock (_lock)
        {
            UserAccount? user = LoadUsers()
                .FirstOrDefault(item => string.Equals(item.Login, login, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                return false;
            }

            return PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
        }
    }

    private List<UserAccount> LoadUsers()
    {
        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<UserAccount>>(json) ?? new List<UserAccount>();
    }

    private void SaveUsers(List<UserAccount> users)
    {
        string json = JsonSerializer.Serialize(users, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }
}
