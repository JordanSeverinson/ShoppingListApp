using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using Microsoft.EntityFrameworkCore;

using ShoppingList.Api.Contracts;

using ShoppingList.Api.Security;

using ShoppingList.Api.Services;

using ShoppingList.Application.Parsing;

using ShoppingList.Application.Recipes;

using ShoppingList.Domain.Entities;

using ShoppingList.Domain.Recipes;

using System.Text.Json;

using ShoppingList.Infrastructure.Persistence;



namespace ShoppingList.Api.Controllers;



[ApiController]

[Route("api/recipes")]

[Authorize]

public class RecipesController(

    ApplicationDbContext db,

    IIngredientParserService ingredientParser,

    CurrentUserService currentUser,

    RecipeAccessService recipeAccess,

    RecipeSharingService recipeSharing,

    OcrProcessingGate ocrGate,

    ILogger<RecipesController> logger) : ControllerBase

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

                r.OwnerId == userId,

                r.UpdatedAt ?? r.CreatedAt,

                r.Ingredients.Count))

            .ToListAsync(cancellationToken);



        var pendingShares = await recipeSharing.GetPendingInvitationsAsync(userId, cancellationToken);



        return Ok(new RecipeSummaryResponse(summaries, pendingShares));

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

            CreatedAt = DateTime.UtcNow

        };



        db.Recipes.Add(recipe);

        await db.SaveChangesAsync(cancellationToken);



        return CreatedAtAction(nameof(GetRecipe), new { recipeId = recipe.Id }, RecipeAccessService.ToSummary(recipe, userId));

    }



    [HttpPost("{recipeId:guid}/shares")]

    [ProducesResponseType(typeof(ShareRecipeResponse), StatusCodes.Status200OK)]

    public async Task<ActionResult<ShareRecipeResponse>> ShareRecipe(

        Guid recipeId,

        [FromBody] ShareRecipeRequest request,

        CancellationToken cancellationToken)

    {

        try

        {

            var userId = currentUser.GetUserId();

            var result = await recipeSharing.ShareWithFriendsAsync(

                recipeId,

                userId,

                request.FriendUserIds ?? [],

                cancellationToken);

            return Ok(result);

        }

        catch (InvalidOperationException ex)

        {

            logger.LogWarning(ex, "Share recipe failed for {RecipeId}", recipeId);

            return BadRequest(new { error = ApiErrors.ShareFailed });

        }

    }



    [HttpPost("shares/{permissionId:guid}/accept")]

    [ProducesResponseType(typeof(RecipeSummaryDto), StatusCodes.Status200OK)]

    public async Task<ActionResult<RecipeSummaryDto>> AcceptShare(

        Guid permissionId,

        CancellationToken cancellationToken)

    {

        try

        {

            var userId = currentUser.GetUserId();

            var summary = await recipeSharing.AcceptShareAsync(permissionId, userId, cancellationToken);

            return Ok(summary);

        }

        catch (InvalidOperationException ex)

        {

            logger.LogWarning(ex, "Accept recipe share failed for {PermissionId}", permissionId);

            return BadRequest(new { error = ApiErrors.ShareFailed });

        }

    }



    [HttpPost("shares/{permissionId:guid}/decline")]

    [ProducesResponseType(StatusCodes.Status204NoContent)]

    public async Task<IActionResult> DeclineShare(

        Guid permissionId,

        CancellationToken cancellationToken)

    {

        try

        {

            var userId = currentUser.GetUserId();

            await recipeSharing.DeclineShareAsync(permissionId, userId, cancellationToken);

            return NoContent();

        }

        catch (InvalidOperationException ex)

        {

            logger.LogWarning(ex, "Decline recipe share failed for {PermissionId}", permissionId);

            return BadRequest(new { error = ApiErrors.ShareFailed });

        }

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

            .OrderBy(i => i.Section)

            .ThenBy(i => i.SortOrder)

            .Select(RecipeAccessService.ToIngredientDto)

            .ToList();



        var steps = recipe.Steps

            .OrderBy(s => s.SortOrder)

            .Select(RecipeAccessService.ToStepDto)

            .ToList();



        return Ok(new RecipeDetailResponse(

            recipe.Id,

            recipe.Name,

            recipe.OwnerId == userId,

            RecipeAccessService.ResolveContent(recipe),

            ingredients,

            steps));

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



        var editableCheck = RecipeAccessService.RequireEditable(recipe, userId);

        if (editableCheck is not null)

        {

            return editableCheck;

        }



        recipe.Name = request.Name.Trim();

        recipe.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);



        return Ok(RecipeAccessService.ToSummary(recipe, userId));

    }



    [HttpDelete("{recipeId:guid}")]

    [ProducesResponseType(StatusCodes.Status204NoContent)]

    public async Task<IActionResult> DeleteRecipe(Guid recipeId, CancellationToken cancellationToken)

    {

        var userId = currentUser.GetUserId();

        var recipe = await db.Recipes

            .AsNoTracking()

            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);



        if (recipe is null || recipe.OwnerId != userId)

        {

            return NotFound(new { error = ApiErrors.RecipeNotFound });

        }



        await db.Recipes

            .Where(r => r.Id == recipeId)

            .ExecuteDeleteAsync(cancellationToken);



        return NoContent();

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



        var createIngredientEditableCheck = RecipeAccessService.RequireEditable(recipe, userId);

        if (createIngredientEditableCheck is not null)

        {

            return createIngredientEditableCheck;

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

            Section = string.IsNullOrWhiteSpace(request.Section) ? null : request.Section.Trim(),

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



        var deleteIngredientEditableCheck = RecipeAccessService.RequireEditable(recipe, userId);

        if (deleteIngredientEditableCheck is not null)

        {

            return deleteIngredientEditableCheck;

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



        var deleteManyEditableCheck = RecipeAccessService.RequireEditable(recipe, userId);

        if (deleteManyEditableCheck is not null)

        {

            return deleteManyEditableCheck;

        }



        var requestedIds = (request.IngredientIds ?? [])

            .Where(id => id != Guid.Empty)

            .Distinct()

            .ToList();



        if (requestedIds.Count == 0)

        {

            return Ok(new DeleteRecipeIngredientsResponse(0, []));

        }



        if (requestedIds.Count > RequestLimits.MaxBulkOperationIds)

        {

            return BadRequest(new { error = $"At most {RequestLimits.MaxBulkOperationIds} ingredients can be deleted per request." });

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



    [HttpPut("{recipeId:guid}/steps")]

    [ProducesResponseType(typeof(IReadOnlyList<RecipeStepDto>), StatusCodes.Status200OK)]

    public async Task<ActionResult<IReadOnlyList<RecipeStepDto>>> ReplaceSteps(

        Guid recipeId,

        [FromBody] ReplaceRecipeStepsRequest request,

        CancellationToken cancellationToken)

    {

        var userId = currentUser.GetUserId();

        var recipe = await recipeAccess.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);



        if (recipe is null)

        {

            return NotFound(new { error = "Recipe not found." });

        }



        var replaceStepsEditableCheck = RecipeAccessService.RequireEditable(recipe, userId);

        if (replaceStepsEditableCheck is not null)

        {

            return replaceStepsEditableCheck;

        }



        var stepTexts = (request.Steps ?? [])

            .Select(text => text.Trim())

            .Where(text => text.Length > 0)

            .ToList();



        if (stepTexts.Count > RequestLimits.MaxRecipeSteps)

        {

            return BadRequest(new { error = $"At most {RequestLimits.MaxRecipeSteps} steps are allowed." });

        }



        await db.RecipeSteps

            .Where(s => s.RecipeId == recipeId)

            .ExecuteDeleteAsync(cancellationToken);



        var now = DateTime.UtcNow;

        var entities = stepTexts

            .Select((text, index) => new RecipeStep

            {

                Id = Guid.NewGuid(),

                RecipeId = recipeId,

                Text = text,

                SortOrder = index + 1,

                CreatedAt = now

            })

            .ToList();



        if (entities.Count > 0)

        {

            db.RecipeSteps.AddRange(entities);

        }



        RecipeContentBuilder.SetCookingSteps(recipe.Content, stepTexts);

        recipe.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);



        var dtos = entities.Select(RecipeAccessService.ToStepDto).ToList();

        return Ok(dtos);

    }



    [HttpPut("{recipeId:guid}/content")]

    [ProducesResponseType(typeof(RecipeContentDocument), StatusCodes.Status200OK)]

    public async Task<ActionResult<RecipeContentDocument>> SaveContent(

        Guid recipeId,

        [FromBody] SaveRecipeContentRequest request,

        CancellationToken cancellationToken)

    {

        var userId = currentUser.GetUserId();

        var recipe = await recipeAccess.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);



        if (recipe is null)

        {

            return NotFound(new { error = "Recipe not found." });

        }



        var saveContentEditableCheck = RecipeAccessService.RequireEditable(recipe, userId);

        if (saveContentEditableCheck is not null)

        {

            return saveContentEditableCheck;

        }



        var serializedContent = JsonSerializer.Serialize(request.Content ?? new RecipeContentDocument());

        if (serializedContent.Length > RequestLimits.MaxRecipeContentJsonChars)

        {

            return BadRequest(new { error = "Recipe content is too large." });

        }



        recipe.Content = request.Content ?? new RecipeContentDocument();

        recipe.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);



        return Ok(recipe.Content);

    }



    [HttpPost("{recipeId:guid}/upload-image")]

    [EnableRateLimiting("ocr")]

    [RequestSizeLimit(10 * 1024 * 1024)]

    [Consumes("multipart/form-data")]

    [ProducesResponseType(typeof(UploadRecipeImageResponse), StatusCodes.Status200OK)]

    public async Task<ActionResult<UploadRecipeImageResponse>> UploadImage(

        Guid recipeId,

        IFormFile? image,

        [FromForm] string? importMode,

        CancellationToken cancellationToken)

    {

        var userId = currentUser.GetUserId();

        var recipe = await recipeAccess.GetAccessibleRecipeAsync(recipeId, userId, cancellationToken);



        if (recipe is null)

        {

            return NotFound(new { error = "Recipe not found." });

        }



        var uploadEditableCheck = RecipeAccessService.RequireEditable(recipe, userId);

        if (uploadEditableCheck is not null)

        {

            return uploadEditableCheck;

        }



        if (image is null || image.Length == 0)

        {

            return BadRequest(new { error = "An image file is required (form field: image)." });

        }



        if (!ImageFileValidator.IsAllowedContentType(image.ContentType))

        {

            return BadRequest(new { error = "Unsupported image type." });

        }



        var mode = RecipeImageImportMode.FullRecipeWithSteps;

        if (!string.IsNullOrWhiteSpace(importMode)

            && !Enum.TryParse(importMode, ignoreCase: true, out mode))

        {

            return BadRequest(new { error = $"Invalid import mode: {importMode}" });

        }



        IReadOnlyList<ParsedIngredientDto> parsedIngredients;

        IReadOnlyList<string> parsedSteps;

        ParsedRecipeContentDto parsed;

        try

        {

            await using var stream = image.OpenReadStream();

            var header = new byte[12];

            var headerLength = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

            if (!ImageFileValidator.HasValidImageSignature(header.AsSpan(0, headerLength)))

            {

                return BadRequest(new { error = "The uploaded file is not a supported image." });

            }



            await using var uploadStream = new MemoryStream();

            if (headerLength > 0)

            {

                await uploadStream.WriteAsync(header.AsMemory(0, headerLength), cancellationToken);

            }



            await stream.CopyToAsync(uploadStream, cancellationToken);

            uploadStream.Position = 0;



            parsed = await ocrGate.RunAsync(

                () => ingredientParser.ParseFromStreamAsync(uploadStream, mode, cancellationToken),

                cancellationToken);



            parsedIngredients = parsed.Ingredients;

            parsedSteps = parsed.Steps;

        }

        catch (DirectoryNotFoundException)

        {

            return StatusCode(

                StatusCodes.Status503ServiceUnavailable,

                new { error = "Image import is unavailable because OCR data is not installed." });

        }

        catch (InvalidOperationException)

        {

            return StatusCode(

                StatusCodes.Status500InternalServerError,

                new { error = "Could not process the uploaded image." });

        }



        if (mode == RecipeImageImportMode.IngredientsOnly && parsedIngredients.Count == 0)

        {

            return Ok(new UploadRecipeImageResponse(

                recipeId,

                recipe.Content,

                [],

                [],

                "No ingredients could be extracted from the image."));

        }



        if (mode == RecipeImageImportMode.CookingStepsOnly && parsedSteps.Count == 0)

        {

            return Ok(new UploadRecipeImageResponse(

                recipeId,

                recipe.Content,

                [],

                [],

                "No cooking steps could be extracted from the image."));

        }



        if (parsedIngredients.Count == 0 && parsedSteps.Count == 0)

        {

            return Ok(new UploadRecipeImageResponse(

                recipeId,

                recipe.Content,

                [],

                [],

                "No ingredients or steps could be extracted from the image."));

        }



        var importedContent = RecipeContentBuilder.FromParsed(parsed);

        recipe.Content = RecipeContentBuilder.MergeImportedContent(recipe.Content, importedContent);



        var nextSortOrder = await db.RecipeIngredients

            .Where(i => i.RecipeId == recipeId)

            .Select(i => (int?)i.SortOrder)

            .MaxAsync(cancellationToken) ?? 0;



        var now = DateTime.UtcNow;

        var entities = new List<RecipeIngredient>();



        foreach (var item in parsedIngredients)

        {

            nextSortOrder++;

            entities.Add(new RecipeIngredient

            {

                Id = Guid.NewGuid(),

                RecipeId = recipeId,

                Name = item.Name,

                Quantity = string.IsNullOrWhiteSpace(item.Quantity) ? null : item.Quantity,

                Category = item.Category,

                Section = item.Section,

                SortOrder = nextSortOrder,

                CreatedAt = now

            });

        }



        if (entities.Count > 0)

        {

            db.RecipeIngredients.AddRange(entities);

        }



        var stepDtos = new List<RecipeStepDto>();

        if (parsedSteps.Count > 0)

        {

            await db.RecipeSteps

                .Where(s => s.RecipeId == recipeId)

                .ExecuteDeleteAsync(cancellationToken);



            var stepEntities = parsedSteps

                .Select((text, index) => new RecipeStep

                {

                    Id = Guid.NewGuid(),

                    RecipeId = recipeId,

                    Text = text,

                    SortOrder = index + 1,

                    CreatedAt = now

                })

                .ToList();



            db.RecipeSteps.AddRange(stepEntities);

            stepDtos = stepEntities.Select(RecipeAccessService.ToStepDto).ToList();

        }



        recipe.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);



        var ingredientDtos = entities.Select(RecipeAccessService.ToIngredientDto).ToList();

        var messageParts = new List<string>();

        if (ingredientDtos.Count > 0)

        {

            messageParts.Add($"Added {ingredientDtos.Count} ingredient(s)");

        }



        if (stepDtos.Count > 0)

        {

            messageParts.Add($"Added {stepDtos.Count} step(s)");

        }



        return Ok(new UploadRecipeImageResponse(

            recipeId,

            recipe.Content,

            ingredientDtos,

            stepDtos,

            $"{string.Join(" and ", messageParts)} from image."));

    }

}


