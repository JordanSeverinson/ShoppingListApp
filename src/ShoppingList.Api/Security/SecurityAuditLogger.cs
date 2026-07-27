namespace ShoppingList.Api.Security;

public static class SecurityAuditLogger
{
    public static void LogLoginSuccess(ILogger logger, Guid userId) =>
        logger.LogInformation("Security audit: login success UserId={UserId}", userId);

    public static void LogLoginFailure(ILogger logger, string email)
    {
        var at = email.LastIndexOf('@');
        var domain = at >= 0 && at < email.Length - 1 ? email[(at + 1)..] : "unknown";
        logger.LogWarning("Security audit: login failure EmailDomain={EmailDomain}", domain);
    }

    public static void LogLogout(ILogger logger, Guid userId) =>
        logger.LogInformation("Security audit: logout UserId={UserId}", userId);

    public static void LogPasswordChanged(ILogger logger, Guid userId) =>
        logger.LogInformation("Security audit: password changed UserId={UserId}", userId);

    public static void LogPasswordReset(ILogger logger, Guid userId) =>
        logger.LogInformation("Security audit: password reset UserId={UserId}", userId);

    public static void LogAccessDenied(ILogger logger, Guid userId, string resource, Guid resourceId) =>
        logger.LogWarning(
            "Security audit: access denied UserId={UserId} Resource={Resource} ResourceId={ResourceId}",
            userId,
            resource,
            resourceId);

    public static void LogListShareRevoked(ILogger logger, Guid userId, Guid listId) =>
        logger.LogInformation(
            "Security audit: list hub access revoked UserId={UserId} ListId={ListId}",
            userId,
            listId);
}
