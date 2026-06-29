using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingList.Api.Contracts;
using ShoppingList.Api.Services;
using ShoppingList.Application.Parsing;
using ShoppingList.Application.Recipes;
using ShoppingList.Domain.Entities;
using ShoppingList.Domain.Enums;
using ShoppingList.Infrastructure.Persistence;

namespace ShoppingList.Api.Controllers;

[ApiController]
[Route("api/recipes")]
[AllowAnonymous]
public class RecipesController(
    ApplicationDbContext db,
    IIngredientParserService ingredientParser,
    CurrentUserService currentUser,
    RecipeAccessService recipeAccess,
    ShareCodeAllocationService shareCodes) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(RecipeSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecipeSummaryResponse>> GetMyRecipes(CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var summaries = await recipeAccess.AccessibleRecipes(userId)
            .AsNoTracking()
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Select(r => new RecipeSummaryDto(
                r.Id,
                r.Name,
                r.ShareCode,
                r.OwnerId == userId,
                r.UpdatedAt ?? r.CreatedAt,
                r.Ingredients.Count))
            .ToListAsync(cancellationToken);

        return Ok(new RecipeSummaryResponse(summaries));
    }

    [HttpPost]
    [ProducesResponseType(typeof(RecipeSummaryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RecipeSummaryDto>> CreateRecipe(
        [FromBody] CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var name = string.IsNullOrWhiteSpace(request.Name) ? "New recipe" : request.Name.Trim();

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerId = userId,
            ShareCode = await shareCodes.AllocateUniqueShareCodeAsync(cancellationToken),
            CreatedAt = DateTime.UtcNow
        };

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRecipe), new { recipeId = recipe.Id }, RecipeAccessService.ToSummary(recipe, userId));
    }

    [HttpPost("join")]
    [ProducesResponseType(typeof(RecipeSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecipeSummaryDto>> JoinRecipe(
        [FromBody] JoinRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var code = request.ShareCode?.Trim().ToUpperInvariant() ?? string.Empty;

        if (code.Length != 11)
        {
            return BadRequest(new { error = "Share code must be exactly 11 characters." });
        }

        var recipe = await db.Recipes
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.ShareCode == code, cancellationToken);

        if (recipe is null)
        {
            return NotFound(new { error = "No recipe found for that share code." });
        }

        if (recipe.OwnerId == userId)
        {
            return Ok(RecipeAccessService.ToSummary(recipe, userId));
        }

        var alreadyJoined = await db.RecipeSharedPermissions
            .AnyAsync(p => p.UserId == userId && p.RecipeId == recipe.Id, cancellationToken);

        if (!alreadyJoined)
        {
            db.RecipeSharedPermissions.Add(new RecipeSharedPermission
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RecipeId = recipe.Id,
                PermissionLevel = PermissionLevel.Edit,
                GrantedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(RecipeAccessService.ToSummary(recipe, userId));
    }

    [HttpGet("{recipeId:guid}")]
    [ProducesResponseType(typeof(RecipeDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecipeDetailResponse>> GetRecipe(Guid recipeId, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var recipe = await recipeAccess.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);

        if (recipe is null)
        {
            return NotFound(new { error = "Recipe not found." });
        }

        var ingredients = recipe.Ingredients
            .OrderBy(i => i.Category)
            .ThenBy(i => i.SortOrder)
            .Select(RecipeAccessService.ToIngredientDto)
            .ToList();

        return Ok(new RecipeDetailResponse(recipe.Id, recipe.Name, recipe.ShareCode, ingredients));
    }

    [HttpPatch("{recipeId:guid}")]
    [ProducesResponseType(typeof(RecipeSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RecipeSummaryDto>> RenameRecipe(
        Guid recipeId,
        [FromBody] RenameRecipeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Recipe name is required." });
        }

        var userId = currentUser.GetUserId();
        var recipe = await recipeAccess.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);

        if (recipe is null)
        {
            return NotFound(new { error = "Recipe not found." });
        }

        recipe.Name = request.Name.Trim();
        recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(RecipeAccessService.ToSummary(recipe, userId));
    }

    [HttpPost("{recipeId:guid}/ingredients")]
    [ProducesResponseType(typeof(RecipeIngredientDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RecipeIngredientDto>> CreateIngredient(
        Guid recipeId,
        [FromBody] CreateRecipeIngredientRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Ingredient name is required." });
        }

        var userId = currentUser.GetUserId();
        var recipe = await recipeAccess.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);

        if (recipe is null)
        {
            return NotFound(new { error = "Recipe not found." });
        }

        var nextSortOrder = await db.RecipeIngredients
            .Where(i => i.RecipeId == recipeId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var ingredient = new RecipeIngredient
        {
            Id = Guid.NewGuid(),
            RecipeId = recipeId,
            Name = request.Name.Trim(),
            Quantity = string.IsNullOrWhiteSpace(request.Quantity) ? null : request.Quantity.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Other" : request.Category.Trim(),
            SortOrder = nextSortOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        db.RecipeIngredients.Add(ingredient);
        recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRecipe), new { recipeId }, RecipeAccessService.ToIngredientDto(ingredient));
    }

    [HttpDelete("{recipeId:guid}/ingredients/{ingredientId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteIngredient(
        Guid recipeId,
        Guid ingredientId,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var recipe = await recipeAccess.GetAccessibleRecipeMetadataAsync(recipeId, userId, cancellationToken);

        if (recipe is null)
        {
            return NotFound(new { error = "Recipe not found." });
        }

        var rowsDeleted = await db.RecipeIngredients
            .Where(i => i.RecipeId == recipeId && i.Id == ingredientId)
            .ExecuteDeleteAsync(cancellationToken);

        if (rowsDeleted == 0)
        {
            return NotFound(new { error = "Ingredient not found." });
        }

        var now = DateTime.UtcNow;
        await db.Recipes
            .Where(r => r.Id == recipeId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(r => r.UpdatedAt, now),
                cancellationToken);

        return NoContent();
    }

    [HttpPost("{recipeId:guid}/ingredients/delete-many")]
    [ProducesResponseType(typeof(DeleteRecipeIngredientsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeleteRecipeIngredientsResponse>> DeleteIngredients(
        Guid recipeId,
        [FromBody] DeleteRecipeIngredientsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var recipe = await recipeAccess.GetAccessibleRecipeMetadataAsync(recipeId, userId, cancellationToken);

        if (recipe is null)
        {
            return NotFound(new { error = "Recipe not found." });
        }

        var requestedIds = (request.IngredientIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            return Ok(new DeleteRecipeIngredientsResponse(0, []));
        }

        var ingredientIds = await db.RecipeIngredients
            .Where(i => i.RecipeId == recipeId && requestedIds.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        if (ingredientIds.Count == 0)
        {
            return NotFound(new { error = "No matching ingredients found." });
        }

        await db.RecipeIngredients
            .Where(i => i.RecipeId == recipeId && ingredientIds.Contains(i.Id))
            .ExecuteDeleteAsync(cancellationToken);

        var now = DateTime.UtcNow;
        await db.Recipes
            .Where(r => r.Id == recipeId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(r => r.UpdatedAt, now),
                cancellationToken);

        return Ok(new DeleteRecipeIngredientsResponse(ingredientIds.Count, ingredientIds));
    }

    [HttpPost("{recipeId:guid}/upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadRecipeImageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UploadRecipeImageResponse>> UploadImage(
        Guid recipeId,
        IFormFile? image,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var recipe = await recipeAccess.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);

        if (recipe is null)
        {
            return NotFound(new { error = "Recipe not found." });
        }

        if (image is null || image.Length == 0)
        {
            return BadRequest(new { error = "An image file is required (form field: image)." });
        }

        if (!AllowedContentTypes.Contains(image.ContentType))
        {
            return BadRequest(new { error = $"Unsupported content type: {image.ContentType}" });
        }

        IReadOnlyList<ParsedIngredientDto> parsed;
        try
        {
            await using var stream = image.OpenReadStream();
            parsed = await ingredientParser.ParseFromStreamAsync(stream, cancellationToken);
        }
        catch (DirectoryNotFoundException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }

        if (parsed.Count == 0)
        {
            return Ok(new UploadRecipeImageResponse(recipeId, [], "No ingredients could be extracted from the image."));
        }

        var nextSortOrder = await db.RecipeIngredients
            .Where(i => i.RecipeId == recipeId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTime.UtcNow;
        var entities = new List<RecipeIngredient>();

        foreach (var item in parsed)
        {
            nextSortOrder++;
            entities.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                RecipeId = recipeId,
                Name = item.Name,
                Quantity = string.IsNullOrWhiteSpace(item.Quantity) ? null : item.Quantity,
                Category = item.Category,
                SortOrder = nextSortOrder,
                CreatedAt = now
            });
        }

        db.RecipeIngredients.AddRange(entities);
        recipe.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        var dtos = entities.Select(RecipeAccessService.ToIngredientDto).ToList();

        return Ok(new UploadRecipeImageResponse(
            recipeId,
            dtos,
            $"Added {dtos.Count} ingredient(s) from image."));
    }

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/bmp", "image/tiff"
    };
}
