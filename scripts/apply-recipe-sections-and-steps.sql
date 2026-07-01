-- Adds recipe ingredient sections and cooking steps (migration 20260629150000_AddRecipeSectionsAndSteps)
-- Run this if `dotnet ef database update` cannot run while the API is locked.

ALTER TABLE recipe_ingredients
    ADD COLUMN IF NOT EXISTS "Section" character varying(100);

CREATE TABLE IF NOT EXISTS recipe_steps
(
    "Id" uuid NOT NULL,
    "RecipeId" uuid NOT NULL,
    "Text" character varying(2000) NOT NULL,
    "SortOrder" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_recipe_steps" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_recipe_steps_recipes_RecipeId" FOREIGN KEY ("RecipeId") REFERENCES recipes ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_recipe_steps_RecipeId_SortOrder"
    ON recipe_steps ("RecipeId", "SortOrder");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260629150000_AddRecipeSectionsAndSteps', '10.0.0')
ON CONFLICT DO NOTHING;
