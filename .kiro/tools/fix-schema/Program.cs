using Npgsql;

// Get connection string from environment
var connectionString = Environment.GetEnvironmentVariable("TGP_DB_CONNECTION") 
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("ERROR: No connection string found. Set TGP_DB_CONNECTION or ConnectionStrings__DefaultConnection");
    return 1;
}

Console.WriteLine("Connecting to database...");

try
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    Console.WriteLine("Connected successfully.");

    // Add missing columns
    var commands = new[]
    {
        // IsIdentityVerified
        @"DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                           WHERE table_schema = 'tgp' AND table_name = 'Users' AND column_name = 'IsIdentityVerified') THEN
                ALTER TABLE tgp.""Users"" ADD COLUMN ""IsIdentityVerified"" boolean NOT NULL DEFAULT false;
                RAISE NOTICE 'Added IsIdentityVerified column';
            END IF;
        END $$;",
        
        // IsVerified on Tenants
        @"DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                           WHERE table_schema = 'tgp' AND table_name = 'Tenants' AND column_name = 'IsVerified') THEN
                ALTER TABLE tgp.""Tenants"" ADD COLUMN ""IsVerified"" boolean NOT NULL DEFAULT false;
                RAISE NOTICE 'Added IsVerified column';
            END IF;
        END $$;",
        
        // Soft delete columns
        @"DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                           WHERE table_schema = 'tgp' AND table_name = 'Tenants' AND column_name = 'Status') THEN
                ALTER TABLE tgp.""Tenants"" ADD COLUMN ""Status"" character varying(50) NOT NULL DEFAULT 'Active';
                RAISE NOTICE 'Added Status column';
            END IF;
        END $$;",
        
        @"DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                           WHERE table_schema = 'tgp' AND table_name = 'Tenants' AND column_name = 'SoftDeletedAt') THEN
                ALTER TABLE tgp.""Tenants"" ADD COLUMN ""SoftDeletedAt"" timestamp with time zone NULL;
                RAISE NOTICE 'Added SoftDeletedAt column';
            END IF;
        END $$;",
        
        @"DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                           WHERE table_schema = 'tgp' AND table_name = 'Tenants' AND column_name = 'DeletionEffectiveDate') THEN
                ALTER TABLE tgp.""Tenants"" ADD COLUMN ""DeletionEffectiveDate"" timestamp with time zone NULL;
                RAISE NOTICE 'Added DeletionEffectiveDate column';
            END IF;
        END $$;",
        
        // Terms columns
        @"DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                           WHERE table_schema = 'tgp' AND table_name = 'Users' AND column_name = 'TermsAccepted') THEN
                ALTER TABLE tgp.""Users"" ADD COLUMN ""TermsAccepted"" boolean NOT NULL DEFAULT false;
                RAISE NOTICE 'Added TermsAccepted column';
            END IF;
        END $$;",
        
        @"DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                           WHERE table_schema = 'tgp' AND table_name = 'Users' AND column_name = 'TermsAcceptedAt') THEN
                ALTER TABLE tgp.""Users"" ADD COLUMN ""TermsAcceptedAt"" timestamp with time zone NULL;
                RAISE NOTICE 'Added TermsAcceptedAt column';
            END IF;
        END $$;",
        
        // Update migrations history
        @"INSERT INTO tgp.""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
          SELECT '20251227040828_InitialCreate', '9.0.0'
          WHERE NOT EXISTS (SELECT 1 FROM tgp.""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20251227040828_InitialCreate');",
        
        @"INSERT INTO tgp.""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
          SELECT '20251227050145_AddParentalConsent', '9.0.0'
          WHERE NOT EXISTS (SELECT 1 FROM tgp.""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20251227050145_AddParentalConsent');",
        
        @"INSERT INTO tgp.""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
          SELECT '20251227055125_AddSubscriptions', '9.0.0'
          WHERE NOT EXISTS (SELECT 1 FROM tgp.""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20251227055125_AddSubscriptions');",
        
        @"INSERT INTO tgp.""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
          SELECT '20251227061343_AddTermsToUser', '9.0.0'
          WHERE NOT EXISTS (SELECT 1 FROM tgp.""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20251227061343_AddTermsToUser');",
        
        @"INSERT INTO tgp.""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
          SELECT '20251227062249_AddVerificationStatus', '9.0.0'
          WHERE NOT EXISTS (SELECT 1 FROM tgp.""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20251227062249_AddVerificationStatus');",
        
        @"INSERT INTO tgp.""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
          SELECT '20251228044047_AddTenantSoftDelete', '9.0.0'
          WHERE NOT EXISTS (SELECT 1 FROM tgp.""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20251228044047_AddTenantSoftDelete');"
    };

    foreach (var sql in commands)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    Console.WriteLine("Schema fix completed successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    return 1;
}
