using Auth.Domain.Entities;
using MongoDB.Driver;

namespace Auth.Infrastructure.Persistence;

public static class MongoDbInitializer
{
    public static async Task InitializeAsync(IMongoDatabase database)
    {
        await CreateIndexesAsync(database);
        await SeedDefaultRolesAsync(database);
    }

    private static async Task CreateIndexesAsync(IMongoDatabase database)
    {
        var usersCollection = database.GetCollection<User>("users");

        var emailIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "IX_users_email" });

        var usernameIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Username),
            new CreateIndexOptions { Unique = true, Name = "IX_users_username" });

        var createdAtIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.CreatedAt),
            new CreateIndexOptions { Name = "IX_users_createdAt" });

        await usersCollection.Indexes.CreateManyAsync(new[] { emailIndex, usernameIndex, createdAtIndex });

        var refreshTokensCollection = database.GetCollection<RefreshToken>("refresh_tokens");

        var tokenIndex = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.Token),
            new CreateIndexOptions { Unique = true, Name = "IX_refresh_tokens_token" });

        var refreshTokenUserIdIndex = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.UserId),
            new CreateIndexOptions { Name = "IX_refresh_tokens_userId" });

        var refreshTokenExpiresIndex = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.ExpiresAt),
            new CreateIndexOptions { Name = "IX_refresh_tokens_expiresAt" });

        var refreshTokenCreatedAtIndex = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.CreatedAt),
            new CreateIndexOptions { Name = "IX_refresh_tokens_createdAt" });

        await refreshTokensCollection.Indexes.CreateManyAsync(new[] { tokenIndex, refreshTokenUserIdIndex, refreshTokenExpiresIndex, refreshTokenCreatedAtIndex });

        var auditLogsCollection = database.GetCollection<AuditLog>("audit_logs");

        var auditLogUserIdIndex = new CreateIndexModel<AuditLog>(
            Builders<AuditLog>.IndexKeys.Ascending(a => a.UserId),
            new CreateIndexOptions { Name = "IX_audit_logs_userId" });

        var auditLogActionIndex = new CreateIndexModel<AuditLog>(
            Builders<AuditLog>.IndexKeys.Ascending(a => a.Action),
            new CreateIndexOptions { Name = "IX_audit_logs_action" });

        var auditLogCreatedAtIndex = new CreateIndexModel<AuditLog>(
            Builders<AuditLog>.IndexKeys.Ascending(a => a.CreatedAt),
            new CreateIndexOptions { Name = "IX_audit_logs_createdAt" });

        await auditLogsCollection.Indexes.CreateManyAsync(new[] { auditLogUserIdIndex, auditLogActionIndex, auditLogCreatedAtIndex });

        var loginHistoryCollection = database.GetCollection<LoginHistory>("login_history");

        var loginHistoryUserIdIndex = new CreateIndexModel<LoginHistory>(
            Builders<LoginHistory>.IndexKeys.Ascending(l => l.UserId),
            new CreateIndexOptions { Name = "IX_login_history_userId" });

        var loginHistoryCreatedAtIndex = new CreateIndexModel<LoginHistory>(
            Builders<LoginHistory>.IndexKeys.Ascending(l => l.CreatedAt),
            new CreateIndexOptions { Name = "IX_login_history_createdAt" });

        await loginHistoryCollection.Indexes.CreateManyAsync(new[] { loginHistoryUserIdIndex, loginHistoryCreatedAtIndex });

        var rolesCollection = database.GetCollection<Role>("roles");

        var roleNameIndex = new CreateIndexModel<Role>(
            Builders<Role>.IndexKeys.Ascending(r => r.NormalizedName),
            new CreateIndexOptions { Unique = true, Name = "IX_roles_normalizedName" });

        await rolesCollection.Indexes.CreateOneAsync(roleNameIndex);

        var permissionsCollection = database.GetCollection<Permission>("permissions");

        var permissionNameIndex = new CreateIndexModel<Permission>(
            Builders<Permission>.IndexKeys.Ascending(p => p.NormalizedName),
            new CreateIndexOptions { Unique = true, Name = "IX_permissions_normalizedName" });

        await permissionsCollection.Indexes.CreateOneAsync(permissionNameIndex);

        var passwordResetCollection = database.GetCollection<PasswordResetToken>("password_reset_tokens");

        var prTokenIndex = new CreateIndexModel<PasswordResetToken>(
            Builders<PasswordResetToken>.IndexKeys.Ascending(prt => prt.Token),
            new CreateIndexOptions { Unique = true, Name = "IX_password_reset_tokens_token" });

        var prUserIdIndex = new CreateIndexModel<PasswordResetToken>(
            Builders<PasswordResetToken>.IndexKeys.Ascending(prt => prt.UserId),
            new CreateIndexOptions { Name = "IX_password_reset_tokens_userId" });

        var prCreatedAtIndex = new CreateIndexModel<PasswordResetToken>(
            Builders<PasswordResetToken>.IndexKeys.Ascending(prt => prt.CreatedAt),
            new CreateIndexOptions { Name = "IX_password_reset_tokens_createdAt" });

        await passwordResetCollection.Indexes.CreateManyAsync(new[] { prTokenIndex, prUserIdIndex, prCreatedAtIndex });

        var emailConfirmationCollection = database.GetCollection<EmailConfirmationToken>("email_confirmation_tokens");

        var ecTokenIndex = new CreateIndexModel<EmailConfirmationToken>(
            Builders<EmailConfirmationToken>.IndexKeys.Ascending(ect => ect.Token),
            new CreateIndexOptions { Unique = true, Name = "IX_email_confirmation_tokens_token" });

        var ecUserIdIndex = new CreateIndexModel<EmailConfirmationToken>(
            Builders<EmailConfirmationToken>.IndexKeys.Ascending(ect => ect.UserId),
            new CreateIndexOptions { Name = "IX_email_confirmation_tokens_userId" });

        var ecCreatedAtIndex = new CreateIndexModel<EmailConfirmationToken>(
            Builders<EmailConfirmationToken>.IndexKeys.Ascending(ect => ect.CreatedAt),
            new CreateIndexOptions { Name = "IX_email_confirmation_tokens_createdAt" });

        await emailConfirmationCollection.Indexes.CreateManyAsync(new[] { ecTokenIndex, ecUserIdIndex, ecCreatedAtIndex });
    }

    private static async Task SeedDefaultRolesAsync(IMongoDatabase database)
    {
        var rolesCollection = database.GetCollection<Role>("roles");

        var existingRoles = await rolesCollection.Find(_ => true).ToListAsync();

        if (!existingRoles.Any())
        {
            var defaultRoles = new List<Role>
            {
                new() { Name = "SuperAdmin", NormalizedName = "SUPERADMIN", Description = "Super administrator with full access" },
                new() { Name = "Admin", NormalizedName = "ADMIN", Description = "Administrator with management access" },
                new() { Name = "Moderator", NormalizedName = "MODERATOR", Description = "Moderator with limited admin access" },
                new() { Name = "User", NormalizedName = "USER", Description = "Standard user" }
            };

            await rolesCollection.InsertManyAsync(defaultRoles);
        }
    }
}
