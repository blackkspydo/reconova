-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create template database for tenants
CREATE DATABASE reconova_tenant_template;
