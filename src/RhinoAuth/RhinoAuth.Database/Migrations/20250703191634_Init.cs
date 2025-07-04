using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RhinoAuth.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.CreateTable(
                name: "api_clients",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    logo = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    secret = table.Column<string>(type: "text", nullable: true),
                    domain = table.Column<string>(type: "text", nullable: false),
                    login_callback_uri = table.Column<string>(type: "text", nullable: false),
                    logout_callback_uri = table.Column<string>(type: "text", nullable: false),
                    backchannel_logout_uri = table.Column<string>(type: "text", nullable: true),
                    show_consent = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    supports_ecdsa = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_resources",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    logo = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    scopes = table.Column<List<string>>(type: "text[]", nullable: false),
                    symmetric_jwt_secret = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_resources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_claims",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    group = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_claims", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_json_web_keys",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    curve = table.Column<string>(type: "text", nullable: false),
                    x = table.Column<string>(type: "text", nullable: false),
                    y = table.Column<string>(type: "text", nullable: false),
                    d = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_json_web_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone_code = table.Column<int>(type: "integer", nullable: false),
                    allow_phone_number_resgistration = table.Column<bool>(type: "boolean", nullable: false),
                    allow_ip_resgistration = table.Column<bool>(type: "boolean", nullable: false),
                    allow_phone_number_login = table.Column<bool>(type: "boolean", nullable: false),
                    allow_ip_login = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_client_token_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: false),
                    is_refresh_token_used = table.Column<bool>(type: "boolean", nullable: false),
                    scopes = table.Column<List<string>>(type: "text[]", nullable: false),
                    refreshed_by = table.Column<string>(type: "text", nullable: true),
                    api_client_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_client_token_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_client_token_requests_api_clients_api_client_id",
                        column: x => x.api_client_id,
                        principalTable: "api_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_client_resources",
                columns: table => new
                {
                    api_client_id = table.Column<string>(type: "text", nullable: false),
                    api_resource_id = table.Column<string>(type: "text", nullable: false),
                    allowed_scopes = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_client_resources", x => new { x.api_client_id, x.api_resource_id });
                    table.ForeignKey(
                        name: "fk_api_client_resources_api_clients_api_client_id",
                        column: x => x.api_client_id,
                        principalTable: "api_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_client_resources_api_resources_api_resource_id",
                        column: x => x.api_resource_id,
                        principalTable: "api_resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "signup_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    country_phone_code = table.Column<int>(type: "integer", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    email_verification_code = table.Column<string>(type: "text", nullable: false),
                    sms_verification_code = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signup_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_signup_requests_countries_country_code",
                        column: x => x.country_code,
                        principalTable: "countries",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    country_phone_code = table.Column<int>(type: "integer", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    avatar = table.Column<string>(type: "text", nullable: true),
                    blocked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    totp_secret = table.Column<string>(type: "text", nullable: true),
                    domain_attributes = table.Column<Dictionary<string, string>>(type: "hstore", nullable: true),
                    unverified_country_code = table.Column<string>(type: "text", nullable: true),
                    unverified_country_phone_code = table.Column<int>(type: "integer", nullable: true),
                    unverified_phone_number = table.Column<string>(type: "text", nullable: true),
                    unverified_email = table.Column<string>(type: "text", nullable: true),
                    country_code = table.Column<string>(type: "text", nullable: false),
                    creator_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    profile_update_history = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_countries_country_code",
                        column: x => x.country_code,
                        principalTable: "countries",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_users_users_creator_id",
                        column: x => x.creator_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => new { x.role_id, x.claim_id });
                    table.ForeignKey(
                        name: "fk_role_claims_app_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "app_claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "token_request_api_resource",
                columns: table => new
                {
                    token_request_id = table.Column<string>(type: "text", nullable: false),
                    api_resource_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_token_request_api_resource", x => new { x.token_request_id, x.api_resource_id });
                    table.ForeignKey(
                        name: "fk_token_request_api_resource_api_client_token_requests_token_",
                        column: x => x.token_request_id,
                        principalTable: "api_client_token_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_token_request_api_resource_api_resources_api_resource_id",
                        column: x => x.api_resource_id,
                        principalTable: "api_resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logins",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    is_persistent = table.Column<bool>(type: "boolean", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    successful = table.Column<bool>(type: "boolean", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_by_external_login_id = table.Column<string>(type: "text", nullable: true),
                    logout_ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    totp_window = table.Column<long>(type: "bigint", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logins", x => x.id);
                    table.ForeignKey(
                        name: "fk_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "one_time_codes",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_one_time_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_one_time_codes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.role_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authorize_requests",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    request_type = table.Column<int>(type: "integer", nullable: false),
                    code_challenge = table.Column<string>(type: "text", nullable: false),
                    verifier_method = table.Column<int>(type: "integer", nullable: false),
                    scopes = table.Column<List<string>>(type: "text[]", nullable: false),
                    state = table.Column<string>(type: "text", nullable: true),
                    nonce = table.Column<string>(type: "text", nullable: true),
                    consented_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    login_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    api_client_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authorize_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_authorize_requests_api_clients_api_client_id",
                        column: x => x.api_client_id,
                        principalTable: "api_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_authorize_requests_logins_login_id",
                        column: x => x.login_id,
                        principalTable: "logins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_authorize_requests_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_logins",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    access_token = table.Column<string>(type: "text", nullable: true),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    id_token = table.Column<string>(type: "text", nullable: true),
                    open_id_scopes = table.Column<List<string>>(type: "text[]", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    previous_refresh_token = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    login_id = table.Column<string>(type: "text", nullable: false),
                    api_client_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_logins", x => x.id);
                    table.ForeignKey(
                        name: "fk_external_logins_api_clients_api_client_id",
                        column: x => x.api_client_id,
                        principalTable: "api_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_external_logins_logins_login_id",
                        column: x => x.login_id,
                        principalTable: "logins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_external_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authorize_request_api_resource",
                columns: table => new
                {
                    authorize_request_id = table.Column<string>(type: "text", nullable: false),
                    api_resource_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authorize_request_api_resource", x => new { x.authorize_request_id, x.api_resource_id });
                    table.ForeignKey(
                        name: "fk_authorize_request_api_resource_api_resources_api_resource_id",
                        column: x => x.api_resource_id,
                        principalTable: "api_resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_authorize_request_api_resource_authorize_requests_authorize",
                        column: x => x.authorize_request_id,
                        principalTable: "authorize_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_client_http_call",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: true),
                    response_code = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    external_login_id = table.Column<string>(type: "text", nullable: false),
                    api_client_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_client_http_call", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_client_http_call_api_clients_api_client_id",
                        column: x => x.api_client_id,
                        principalTable: "api_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_client_http_call_external_logins_external_login_id",
                        column: x => x.external_login_id,
                        principalTable: "external_logins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_login_api_resource",
                columns: table => new
                {
                    external_login_id = table.Column<string>(type: "text", nullable: false),
                    api_resource_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_external_login_api_resource", x => new { x.external_login_id, x.api_resource_id });
                    table.ForeignKey(
                        name: "fk_external_login_api_resource_api_resources_api_resource_id",
                        column: x => x.api_resource_id,
                        principalTable: "api_resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_external_login_api_resource_external_logins_external_login_",
                        column: x => x.external_login_id,
                        principalTable: "external_logins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_client_http_call_api_client_id",
                table: "api_client_http_call",
                column: "api_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_client_http_call_external_login_id",
                table: "api_client_http_call",
                column: "external_login_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_client_resources_api_resource_id",
                table: "api_client_resources",
                column: "api_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_client_token_requests_api_client_id",
                table: "api_client_token_requests",
                column: "api_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_authorize_request_api_resource_api_resource_id",
                table: "authorize_request_api_resource",
                column: "api_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_authorize_requests_api_client_id",
                table: "authorize_requests",
                column: "api_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_authorize_requests_login_id",
                table: "authorize_requests",
                column: "login_id");

            migrationBuilder.CreateIndex(
                name: "ix_authorize_requests_user_id",
                table: "authorize_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_login_api_resource_api_resource_id",
                table: "external_login_api_resource",
                column: "api_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_api_client_id",
                table: "external_logins",
                column: "api_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_login_id",
                table: "external_logins",
                column: "login_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_user_id",
                table: "external_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_logins_user_id",
                table: "logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_one_time_codes_user_id",
                table: "one_time_codes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_claim_id",
                table: "role_claims",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_signup_requests_country_code",
                table: "signup_requests",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_token_request_api_resource_api_resource_id",
                table: "token_request_api_resource",
                column: "api_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_country_code",
                table: "users",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_users_creator_id",
                table: "users",
                column: "creator_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_phone_number_country_phone_code",
                table: "users",
                columns: new[] { "phone_number", "country_phone_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_client_http_call");

            migrationBuilder.DropTable(
                name: "api_client_resources");

            migrationBuilder.DropTable(
                name: "app_json_web_keys");

            migrationBuilder.DropTable(
                name: "authorize_request_api_resource");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "external_login_api_resource");

            migrationBuilder.DropTable(
                name: "one_time_codes");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "signup_requests");

            migrationBuilder.DropTable(
                name: "token_request_api_resource");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "authorize_requests");

            migrationBuilder.DropTable(
                name: "external_logins");

            migrationBuilder.DropTable(
                name: "app_claims");

            migrationBuilder.DropTable(
                name: "api_client_token_requests");

            migrationBuilder.DropTable(
                name: "api_resources");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "logins");

            migrationBuilder.DropTable(
                name: "api_clients");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "countries");
        }
    }
}
