ALTER TABLE recipes
    ADD COLUMN IF NOT EXISTS "Content" jsonb NOT NULL DEFAULT '{"recipe":{"cookingSteps":[]}}';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260629160000_AddRecipeContentJson', '10.0.0')
ON CONFLICT DO NOTHING;
