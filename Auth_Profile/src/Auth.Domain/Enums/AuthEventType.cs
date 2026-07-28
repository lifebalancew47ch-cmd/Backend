namespace Auth.Domain.Enums;

public enum AuthEventType
{
    Login,
    Logout,
    Register,
    PasswordChange,
    PasswordReset,
    EmailConfirmation,
    AccountLockout,
    TokenRefresh,
    TokenRevocation,
    FailedLogin,
    ProfileUpdate,
    RoleChange
}
