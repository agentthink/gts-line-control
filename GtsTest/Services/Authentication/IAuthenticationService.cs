using GtsTest.Models;
using System;

namespace GtsTest.Services.Authentication
{
    public interface IAuthenticationService
    {
        bool RegisterUser(string username, string fullName, string password, string role = "Operator");
        bool ValidateCredentials(string username, string password, out User? userOut);
        bool ChangePassword(string username, string currentPassword, string newPassword);
        string ResetPasswordAsAdmin(string username);
        string? EnsureDefaultAdmin();

        bool HasPermission(User user, string permissionCode);
    }
}