-- Set up schema for Neon PostgreSQL
SET search_path TO public;

-- Create the migrations history table first
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218191624_InitialIdentityCreate') THEN
    CREATE TABLE "AuditLogs" (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "Action" character varying(100) NOT NULL,
        "EntityType" character varying(100),
        "EntityId" uuid,
        "Changes" character varying(4000),
        "IpAddress" character varying(45),
        "UserAgent" character varying(500),
        "Timestamp" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218191624_InitialIdentityCreate') THEN
    CREATE TABLE "LoginAttempts" (
        "Id" uuid NOT NULL,
        "UserId" uuid,
        "Username" character varying(50),
        "IpAddress" character varying(45),
        "UserAgent" character varying(500),
        "Success" boolean NOT NULL,
        "FailureReason" character varying(255),
        "AttemptedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LoginAttempts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218191624_InitialIdentityCreate') THEN
    CREATE TABLE "Permissions" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(255),
        "Category" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Permissions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;
