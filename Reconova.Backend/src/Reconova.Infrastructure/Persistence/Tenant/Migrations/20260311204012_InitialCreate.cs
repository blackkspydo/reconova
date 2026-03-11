using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reconova.Infrastructure.Persistence.Tenant.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserEmail = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsAdminAction = table.Column<bool>(type: "boolean", nullable: false),
                    IsImpersonation = table.Column<bool>(type: "boolean", nullable: false),
                    ImpersonatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "compliance_frameworks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_system_framework = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    control_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    grace_period_days = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_frameworks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    verification_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    verification_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    verification_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_scan_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domains", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    webhook_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    encrypted_api_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    configuration = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_failure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_compliance_selections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    framework_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    disabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_compliance_selections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_cve_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    digest_time_utc = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "09:00"),
                    digest_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_cve_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    steps = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system_template = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_credit_cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "compliance_controls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    framework_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    automation_query = table.Column<string>(type: "jsonb", nullable: true),
                    is_automatable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    min_security_recommendations_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_controls", x => x.id);
                    table.ForeignKey(
                        name: "FK_compliance_controls_compliance_frameworks_framework_id",
                        column: x => x.framework_id,
                        principalTable: "compliance_frameworks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "compliance_assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    framework_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    overall_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    compliance_score = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    total_controls = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    passed_controls = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_controls = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    not_assessed_controls = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    assessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assessed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scan_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    control_results = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_compliance_assessments_compliance_frameworks_framework_id",
                        column: x => x.framework_id,
                        principalTable: "compliance_frameworks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_compliance_assessments_domains_domain_id",
                        column: x => x.domain_id,
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scan_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    credit_cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration = table.Column<string>(type: "jsonb", nullable: true),
                    steps_json = table.Column<string>(type: "jsonb", nullable: true),
                    total_credits = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_step = table.Column<int>(type: "integer", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    result_count = table.Column<int>(type: "integer", nullable: true),
                    vulnerability_count = table.Column<int>(type: "integer", nullable: true),
                    progress_percentage = table.Column<double>(type: "double precision", nullable: true),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_scan_jobs_domains_domain_id",
                        column: x => x.domain_id,
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subdomains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    is_alive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    web_server = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    discovered_by_scan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subdomains", x => x.id);
                    table.ForeignKey(
                        name: "FK_subdomains_domains_domain_id",
                        column: x => x.domain_id,
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    integration_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    min_severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    filters = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_rules_integration_configs_integration_config_id",
                        column: x => x.integration_config_id,
                        principalTable: "integration_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_step = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_steps = table.Column<int>(type: "integer", nullable: false),
                    initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    step_results = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflows", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflows_domains_domain_id",
                        column: x => x.domain_id,
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflows_workflow_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "workflow_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "control_check_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    control_id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity_threshold = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pass_condition_json = table.Column<string>(type: "jsonb", nullable: true),
                    recommendation_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_check_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_control_check_mappings_compliance_controls_control_id",
                        column: x => x.control_id,
                        principalTable: "compliance_controls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "control_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NotAssessed"),
                    evidence_json = table.Column<string>(type: "jsonb", nullable: true),
                    recommendation_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_control_results_compliance_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "compliance_assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_control_results_compliance_controls_control_id",
                        column: x => x.control_id,
                        principalTable: "compliance_controls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scan_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    scan_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_scan_results_scan_jobs_scan_job_id",
                        column: x => x.scan_job_id,
                        principalTable: "scan_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subdomain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    port_number = table.Column<int>(type: "integer", nullable: false),
                    protocol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "tcp"),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    service_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    banner = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discovered_by_scan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ports", x => x.id);
                    table.ForeignKey(
                        name: "FK_ports_subdomains_subdomain_id",
                        column: x => x.subdomain_id,
                        principalTable: "subdomains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "screenshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subdomain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    taken_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_screenshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_screenshots_subdomains_subdomain_id",
                        column: x => x.subdomain_id,
                        principalTable: "subdomains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "technologies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    subdomain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    discovered_by_scan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technologies", x => x.id);
                    table.ForeignKey(
                        name: "FK_technologies_subdomains_subdomain_id",
                        column: x => x.subdomain_id,
                        principalTable: "subdomains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vulnerabilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    scan_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subdomain_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cve_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cwe_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cvss_score = table.Column<double>(type: "double precision", nullable: true),
                    affected_component = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    remediation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    evidence = table.Column<string>(type: "jsonb", nullable: true),
                    reference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vulnerabilities", x => x.id);
                    table.ForeignKey(
                        name: "FK_vulnerabilities_scan_jobs_scan_job_id",
                        column: x => x.scan_job_id,
                        principalTable: "scan_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vulnerabilities_subdomains_subdomain_id",
                        column: x => x.subdomain_id,
                        principalTable: "subdomains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "scan_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    estimated_credits = table.Column<int>(type: "integer", nullable: false),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_scan_schedules_domains_domain_id",
                        column: x => x.domain_id,
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scan_schedules_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vulnerability_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cve_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    technology_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subdomain_id = table.Column<Guid>(type: "uuid", nullable: true),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    matched_product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    matched_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_acknowledged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    auto_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vulnerability_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_vulnerability_alerts_subdomains_subdomain_id",
                        column: x => x.subdomain_id,
                        principalTable: "subdomains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vulnerability_alerts_technologies_technology_id",
                        column: x => x.technology_id,
                        principalTable: "technologies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_assessments_domain_id",
                table: "compliance_assessments",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_assessments_framework_id",
                table: "compliance_assessments",
                column: "framework_id");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_assessments_tenant_id_framework_id_domain_id",
                table: "compliance_assessments",
                columns: new[] { "tenant_id", "framework_id", "domain_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_controls_framework_id_control_id",
                table: "compliance_controls",
                columns: new[] { "framework_id", "control_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compliance_frameworks_code",
                table: "compliance_frameworks",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "IX_control_check_mappings_control_id",
                table: "control_check_mappings",
                column: "control_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_results_assessment_id_control_id",
                table: "control_results",
                columns: new[] { "assessment_id", "control_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_results_control_id",
                table: "control_results",
                column: "control_id");

            migrationBuilder.CreateIndex(
                name: "IX_domains_tenant_id",
                table: "domains",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_domains_tenant_id_name",
                table: "domains",
                columns: new[] { "tenant_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_integration_configs_tenant_id",
                table: "integration_configs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_rules_integration_config_id",
                table: "notification_rules",
                column: "integration_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_rules_tenant_id",
                table: "notification_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ports_subdomain_id_port_number_protocol",
                table: "ports",
                columns: new[] { "subdomain_id", "port_number", "protocol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scan_jobs_created_at",
                table: "scan_jobs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_scan_jobs_domain_id",
                table: "scan_jobs",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_scan_jobs_status",
                table: "scan_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_scan_jobs_tenant_id",
                table: "scan_jobs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_scan_results_scan_job_id",
                table: "scan_results",
                column: "scan_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_scan_schedules_domain_id_workflow_id",
                table: "scan_schedules",
                columns: new[] { "domain_id", "workflow_id" });

            migrationBuilder.CreateIndex(
                name: "IX_scan_schedules_workflow_id",
                table: "scan_schedules",
                column: "workflow_id");

            migrationBuilder.CreateIndex(
                name: "IX_screenshots_subdomain_id",
                table: "screenshots",
                column: "subdomain_id");

            migrationBuilder.CreateIndex(
                name: "IX_subdomains_domain_id",
                table: "subdomains",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_subdomains_domain_id_name",
                table: "subdomains",
                columns: new[] { "domain_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_technologies_subdomain_id_name",
                table: "technologies",
                columns: new[] { "subdomain_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_compliance_selections_tenant_id_framework_id",
                table: "tenant_compliance_selections",
                columns: new[] { "tenant_id", "framework_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_cve_settings_tenant_id",
                table: "tenant_cve_settings",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vulnerabilities_cve_id",
                table: "vulnerabilities",
                column: "cve_id");

            migrationBuilder.CreateIndex(
                name: "IX_vulnerabilities_scan_job_id",
                table: "vulnerabilities",
                column: "scan_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_vulnerabilities_severity",
                table: "vulnerabilities",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "IX_vulnerabilities_subdomain_id",
                table: "vulnerabilities",
                column: "subdomain_id");

            migrationBuilder.CreateIndex(
                name: "IX_vulnerability_alerts_cve_id",
                table: "vulnerability_alerts",
                column: "cve_id");

            migrationBuilder.CreateIndex(
                name: "IX_vulnerability_alerts_severity",
                table: "vulnerability_alerts",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "IX_vulnerability_alerts_subdomain_id",
                table: "vulnerability_alerts",
                column: "subdomain_id");

            migrationBuilder.CreateIndex(
                name: "IX_vulnerability_alerts_technology_id",
                table: "vulnerability_alerts",
                column: "technology_id");

            migrationBuilder.CreateIndex(
                name: "IX_vulnerability_alerts_tenant_id",
                table: "vulnerability_alerts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_templates_tenant_id",
                table: "workflow_templates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_domain_id",
                table: "workflows",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_status",
                table: "workflows",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_template_id",
                table: "workflows",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflows_tenant_id",
                table: "workflows",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "control_check_mappings");

            migrationBuilder.DropTable(
                name: "control_results");

            migrationBuilder.DropTable(
                name: "notification_rules");

            migrationBuilder.DropTable(
                name: "ports");

            migrationBuilder.DropTable(
                name: "scan_results");

            migrationBuilder.DropTable(
                name: "scan_schedules");

            migrationBuilder.DropTable(
                name: "screenshots");

            migrationBuilder.DropTable(
                name: "tenant_compliance_selections");

            migrationBuilder.DropTable(
                name: "tenant_cve_settings");

            migrationBuilder.DropTable(
                name: "vulnerabilities");

            migrationBuilder.DropTable(
                name: "vulnerability_alerts");

            migrationBuilder.DropTable(
                name: "compliance_assessments");

            migrationBuilder.DropTable(
                name: "compliance_controls");

            migrationBuilder.DropTable(
                name: "integration_configs");

            migrationBuilder.DropTable(
                name: "workflows");

            migrationBuilder.DropTable(
                name: "scan_jobs");

            migrationBuilder.DropTable(
                name: "technologies");

            migrationBuilder.DropTable(
                name: "compliance_frameworks");

            migrationBuilder.DropTable(
                name: "workflow_templates");

            migrationBuilder.DropTable(
                name: "subdomains");

            migrationBuilder.DropTable(
                name: "domains");
        }
    }
}
