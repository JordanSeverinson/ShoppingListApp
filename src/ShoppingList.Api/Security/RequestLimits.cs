namespace ShoppingList.Api.Security;

public static class RequestLimits
{
    public const int MaxBulkOperationIds = 500;
    public const int MaxRecipeSteps = 500;
    public const int MaxRecipeContentJsonChars = 1_000_000;
}
