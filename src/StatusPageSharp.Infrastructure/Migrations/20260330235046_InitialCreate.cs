using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StatusPageSharp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    NormalizedName = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: true
                    ),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UserName = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    NormalizedUserName = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    Email = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    NormalizedEmail = table.Column<string>(
                        type: "character varying(256)",
                        maxLength: 256,
                        nullable: true
                    ),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: false
                    ),
                    Summary = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: false
                    ),
                    Postmortem = table.Column<string>(type: "text", nullable: true),
                    StartedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ResolvedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ScheduledMaintenances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: false
                    ),
                    Summary = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: false
                    ),
                    StartsUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    EndsUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledMaintenances", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ServiceGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: false
                    ),
                    Slug = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceGroups", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    SiteTitle = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: false
                    ),
                    LogoUrl = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: true
                    ),
                    PublicRefreshIntervalSeconds = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    DefaultRawRetentionDays = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AspNetUserLogins",
                        x => new { x.LoginProvider, x.ProviderKey }
                    );
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AspNetUserTokens",
                        x => new
                        {
                            x.UserId,
                            x.LoginProvider,
                            x.Name,
                        }
                    );
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "IncidentEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: false
                    ),
                    Body = table.Column<string>(type: "text", nullable: true),
                    IsSystemGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentEvents_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: false
                    ),
                    Slug = table.Column<string>(
                        type: "character varying(120)",
                        maxLength: 120,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CheckPeriodSeconds = table.Column<int>(type: "integer", nullable: false),
                    FailureThreshold = table.Column<int>(type: "integer", nullable: false),
                    RecoveryThreshold = table.Column<int>(type: "integer", nullable: false),
                    RawRetentionDaysOverride = table.Column<int>(type: "integer", nullable: true),
                    ConsecutiveFailureCount = table.Column<int>(type: "integer", nullable: false),
                    ConsecutiveSuccessCount = table.Column<int>(type: "integer", nullable: false),
                    LastCheckStartedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    LastCheckCompletedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    NextCheckUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    LastLatencyMilliseconds = table.Column<int>(type: "integer", nullable: true),
                    LastFailureKind = table.Column<int>(type: "integer", nullable: false),
                    LastFailureMessage = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_ServiceGroups_ServiceGroupId",
                        column: x => x.ServiceGroupId,
                        principalTable: "ServiceGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "CheckResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CompletedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    DurationMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    IsIgnoredForAvailability = table.Column<bool>(type: "boolean", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    FailureKind = table.Column<int>(type: "integer", nullable: false),
                    FailureMessage = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    ResponseSnippet = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckResults_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "DailyServiceRollups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalChecks = table.Column<int>(type: "integer", nullable: false),
                    SuccessfulChecks = table.Column<int>(type: "integer", nullable: false),
                    AvailabilityEligibleChecks = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    AvailabilitySuccessChecks = table.Column<int>(type: "integer", nullable: false),
                    TotalLatencyMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    AverageLatencyMilliseconds = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    P95LatencyMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    DowntimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaintenanceMinutes = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyServiceRollups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyServiceRollups_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "HourlyServiceRollups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    HourStartUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    TotalChecks = table.Column<int>(type: "integer", nullable: false),
                    SuccessfulChecks = table.Column<int>(type: "integer", nullable: false),
                    AvailabilityEligibleChecks = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    AvailabilitySuccessChecks = table.Column<int>(type: "integer", nullable: false),
                    TotalLatencyMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    AverageLatencyMilliseconds = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    P95LatencyMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    DowntimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaintenanceMinutes = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyServiceRollups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourlyServiceRollups_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "IncidentAffectedServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImpactLevel = table.Column<int>(type: "integer", nullable: false),
                    AddedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ResolvedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentAffectedServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentAffectedServices_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_IncidentAffectedServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "MonitorDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonitorType = table.Column<int>(type: "integer", nullable: false),
                    Host = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: true
                    ),
                    Port = table.Column<int>(type: "integer", nullable: true),
                    Url = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: true
                    ),
                    HttpMethod = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    RequestHeadersJson = table.Column<string>(type: "text", nullable: true),
                    RequestBody = table.Column<string>(type: "text", nullable: true),
                    ExpectedStatusCodes = table.Column<string>(
                        type: "character varying(80)",
                        maxLength: 80,
                        nullable: false
                    ),
                    ExpectedResponseSubstring = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    VerifyTlsCertificate = table.Column<bool>(type: "boolean", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitorDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitorDefinitions_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "MonthlySlaSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    EligibleMinutes = table.Column<int>(type: "integer", nullable: false),
                    DowntimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaintenanceMinutes = table.Column<int>(type: "integer", nullable: false),
                    UptimePercentage = table.Column<decimal>(
                        type: "numeric(5,2)",
                        precision: 5,
                        scale: 2,
                        nullable: false
                    ),
                    ComputedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlySlaSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlySlaSnapshots_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ScheduledMaintenanceServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledMaintenanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledMaintenanceServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledMaintenanceServices_ScheduledMaintenances_Schedule~",
                        column: x => x.ScheduledMaintenanceId,
                        principalTable: "ScheduledMaintenances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ScheduledMaintenanceServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId"
            );

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId"
            );

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail"
            );

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CheckResults_ServiceId_StartedUtc",
                table: "CheckResults",
                columns: new[] { "ServiceId", "StartedUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_DailyServiceRollups_ServiceId_Day",
                table: "DailyServiceRollups",
                columns: new[] { "ServiceId", "Day" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_HourlyServiceRollups_ServiceId_HourStartUtc",
                table: "HourlyServiceRollups",
                columns: new[] { "ServiceId", "HourStartUtc" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAffectedServices_IncidentId_ServiceId_IsResolved",
                table: "IncidentAffectedServices",
                columns: new[] { "IncidentId", "ServiceId", "IsResolved" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAffectedServices_ServiceId",
                table: "IncidentAffectedServices",
                column: "ServiceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEvents_IncidentId_CreatedUtc",
                table: "IncidentEvents",
                columns: new[] { "IncidentId", "CreatedUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status",
                table: "Incidents",
                column: "Status",
                unique: true,
                filter: "\"Status\" = 0"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status_StartedUtc",
                table: "Incidents",
                columns: new[] { "Status", "StartedUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MonitorDefinitions_ServiceId",
                table: "MonitorDefinitions",
                column: "ServiceId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySlaSnapshots_ServiceId_Month",
                table: "MonthlySlaSnapshots",
                columns: new[] { "ServiceId", "Month" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMaintenances_StartsUtc_EndsUtc",
                table: "ScheduledMaintenances",
                columns: new[] { "StartsUtc", "EndsUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMaintenanceServices_ScheduledMaintenanceId_Service~",
                table: "ScheduledMaintenanceServices",
                columns: new[] { "ScheduledMaintenanceId", "ServiceId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMaintenanceServices_ServiceId",
                table: "ScheduledMaintenanceServices",
                column: "ServiceId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ServiceGroups_Slug",
                table: "ServiceGroups",
                column: "Slug",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Services_IsEnabled_NextCheckUtc",
                table: "Services",
                columns: new[] { "IsEnabled", "NextCheckUtc" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceGroupId",
                table: "Services",
                column: "ServiceGroupId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Services_Slug",
                table: "Services",
                column: "Slug",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AspNetRoleClaims");

            migrationBuilder.DropTable(name: "AspNetUserClaims");

            migrationBuilder.DropTable(name: "AspNetUserLogins");

            migrationBuilder.DropTable(name: "AspNetUserRoles");

            migrationBuilder.DropTable(name: "AspNetUserTokens");

            migrationBuilder.DropTable(name: "CheckResults");

            migrationBuilder.DropTable(name: "DailyServiceRollups");

            migrationBuilder.DropTable(name: "HourlyServiceRollups");

            migrationBuilder.DropTable(name: "IncidentAffectedServices");

            migrationBuilder.DropTable(name: "IncidentEvents");

            migrationBuilder.DropTable(name: "MonitorDefinitions");

            migrationBuilder.DropTable(name: "MonthlySlaSnapshots");

            migrationBuilder.DropTable(name: "ScheduledMaintenanceServices");

            migrationBuilder.DropTable(name: "SiteSettings");

            migrationBuilder.DropTable(name: "AspNetRoles");

            migrationBuilder.DropTable(name: "AspNetUsers");

            migrationBuilder.DropTable(name: "Incidents");

            migrationBuilder.DropTable(name: "ScheduledMaintenances");

            migrationBuilder.DropTable(name: "Services");

            migrationBuilder.DropTable(name: "ServiceGroups");
        }
    }
}
