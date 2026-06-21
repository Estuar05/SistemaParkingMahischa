using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Controllers;

public sealed class LoginController
{
    private readonly AuthService _authService = new();

    public User? Login(string username, string password) => _authService.Login(username, password);
}

