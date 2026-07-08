using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koralytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Academies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    SecondaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    FoundedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Academies", x => x.Id);
                    table.CheckConstraint("CK_Academy_FoundedAt", "[FoundedAt] <= GETUTCDATE()");
                    table.ForeignKey(
                        name: "FK_Academies_AspNetUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Academies_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Academies_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AcademyRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AcademyName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContactPersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestStatus = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReviewedById = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademyRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcademyRequests_AspNetUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademyRequests_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AcademyRequests_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Coaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coaches_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrillCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillCategories_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrillCategories_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlatformAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetEntity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetEntityId = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformAuditLogs_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlatformAuditLogs_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformSettings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlatformSettings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredFoot = table.Column<int>(type: "int", nullable: false),
                    WeakFootRating = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    PlayStyleTag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ArchetypePlayerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ArchetypeText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AvailabilityStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scouters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scouters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scouters_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemAdmins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemAdmins_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcademyAdmins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademyAdmins_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademyAdmins_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcademyAnnouncements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    SentByUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyAnnouncements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademyAnnouncements_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademyAnnouncements_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcademyAnnouncements_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AcademyBadges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    BadgeType = table.Column<int>(type: "int", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademyBadges_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademyBadges_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AcademyLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMain = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademyLocations_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademyLocations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcademyLocations_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgeGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: false),
                    MaxAge = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeGroups", x => x.Id);
                    table.CheckConstraint("CK_AgeGroup_Ages", "[MaxAge] > [MinAge]");
                    table.CheckConstraint("CK_AgeGroup_MaxAge", "[MaxAge] <= 50");
                    table.CheckConstraint("CK_AgeGroup_MinAge", "[MinAge] >= 5");
                    table.ForeignKey(
                        name: "FK_AgeGroups_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgeGroups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AIReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: true),
                    ReportText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIReports_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIReports_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    PerformedByUserId = table.Column<int>(type: "int", nullable: false),
                    AffectedUserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleAuditLogs_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleAuditLogs_AspNetUsers_AffectedUserId",
                        column: x => x.AffectedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleAuditLogs_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoleAuditLogs_AspNetUsers_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleAuditLogs_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CoachAcademies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BiasScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    BiasLastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachAcademies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachAcademies_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachAcademies_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachAcademies_Coaches_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachTempAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    GrantedToUserId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachTempAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachTempAccesses_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachTempAccesses_AspNetUsers_GrantedToUserId",
                        column: x => x.GrantedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachTempAccesses_Coaches_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrillTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DrillMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillTemplates_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrillTemplates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrillTemplates_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrillTemplates_DrillCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "DrillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parents_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Parents_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlayerAcademies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAcademies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerAcademies_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerAcademies_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerAcademies_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AchievementType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAchievements", x => x.Id);
                    table.CheckConstraint("CK_PlayerAchievement_Polymorphic_Pair", "([ReferenceId] IS NULL AND [ReferenceType] IS NULL) OR ([ReferenceId] IS NOT NULL AND [ReferenceType] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    OverallRating = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OverallTrainingAvg = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OverallTournamentAvg = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TransferClassification = table.Column<int>(type: "int", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerCards_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerCards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Achieved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGoals", x => x.Id);
                    table.CheckConstraint("CK_PlayerGoal_TargetScore_Positive", "[TargetScore] >= 0");
                    table.ForeignKey(
                        name: "FK_PlayerGoals_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerGoals_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerGoals_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerHighlights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerHighlights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerHighlights_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerHighlights_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerHighlights_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerPositions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerPositions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    PaidByUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GraceUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSubscriptions", x => x.Id);
                    table.CheckConstraint("CK_PlayerSubscription_GraceUntil_After_PaidAt", "[GraceUntil] IS NULL OR [PaidAt] IS NULL OR [GraceUntil] >= [PaidAt]");
                    table.ForeignKey(
                        name: "FK_PlayerSubscriptions_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerSubscriptions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerSubscriptions_AspNetUsers_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerSubscriptions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScouterFollows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScouterUserId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScouterFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScouterFollows_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScouterFollows_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScouterFollows_Scouters_ScouterUserId",
                        column: x => x.ScouterUserId,
                        principalTable: "Scouters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScouterReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScouterUserId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    ReportText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScouterReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScouterReports_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScouterReports_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScouterReports_Scouters_ScouterUserId",
                        column: x => x.ScouterUserId,
                        principalTable: "Scouters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScouterShortlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScouterUserId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScouterShortlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScouterShortlists_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScouterShortlists_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScouterShortlists_Scouters_ScouterUserId",
                        column: x => x.ScouterUserId,
                        principalTable: "Scouters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScouterViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    ScouterId = table.Column<int>(type: "int", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScouterViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScouterViews_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScouterViews_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScouterViews_Scouters_ScouterId",
                        column: x => x.ScouterId,
                        principalTable: "Scouters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgeGroupId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_AcademyLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "AcademyLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_AgeGroups_AgeGroupId",
                        column: x => x.AgeGroupId,
                        principalTable: "AgeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teams_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Teams_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    Structure = table.Column<int>(type: "int", nullable: false),
                    AgeGroupId = table.Column<int>(type: "int", nullable: false),
                    HasTwoLegs = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                    table.CheckConstraint("CK_Tournament_Dates", "[EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_Tournaments_AgeGroups_AgeGroupId",
                        column: x => x.AgeGroupId,
                        principalTable: "AgeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tournaments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tournaments_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ParentPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentPlayers_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParentPlayers_Parents_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Parents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParentPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerCategoryRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerCardId = table.Column<int>(type: "int", nullable: false),
                    DrillCategoryId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCategoryRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerCategoryRatings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerCategoryRatings_DrillCategories_DrillCategoryId",
                        column: x => x.DrillCategoryId,
                        principalTable: "DrillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerCategoryRatings_PlayerCards_PlayerCardId",
                        column: x => x.PlayerCardId,
                        principalTable: "PlayerCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachTeams_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachTeams_Coaches_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrillSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    CoachId = table.Column<int>(type: "int", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillSessions_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrillSessions_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrillSessions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrillSessions_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrillSessions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerTeams_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlayerTeams_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDummy = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentGroups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentGroups_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentGroups_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentHallOfFames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AwardType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentHallOfFames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentHallOfFames_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentHallOfFames_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentHallOfFames_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentHallOfFames_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentRounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentRounds_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentRounds_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentSquads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentSquads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentSquads_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentSquads_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentSquads_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentSquads_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentSquads_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    SeedNumber = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentTeams_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentTeams_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Drills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    DrillTemplateId = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drills_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Drills_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Drills_DrillSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "DrillSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Drills_DrillTemplates_DrillTemplateId",
                        column: x => x.DrillTemplateId,
                        principalTable: "DrillTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HomeTeamId = table.Column<int>(type: "int", nullable: false),
                    AwayTeamId = table.Column<int>(type: "int", nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: true),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HomeScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AwayScore = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HomePenaltyScore = table.Column<int>(type: "int", nullable: true),
                    AwayPenaltyScore = table.Column<int>(type: "int", nullable: true),
                    WinningTeamId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.CheckConstraint("CK_Match_PenaltyScores", "([HomePenaltyScore] IS NULL AND [AwayPenaltyScore] IS NULL) OR ([HomePenaltyScore] IS NOT NULL AND [AwayPenaltyScore] IS NOT NULL)");
                    table.CheckConstraint("CK_Match_Scores", "[HomeScore] >= 0 AND [AwayScore] >= 0");
                    table.CheckConstraint("CK_Match_WinningTeam", "[WinningTeamId] IS NULL OR [WinningTeamId] = [HomeTeamId] OR [WinningTeamId] = [AwayTeamId]");
                    table.ForeignKey(
                        name: "FK_Matches_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Matches_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Matches_DrillSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "DrillSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_WinningTeamId",
                        column: x => x.WinningTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    playerId = table.Column<int>(type: "int", nullable: false),
                    IsPresent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAttendances_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionAttendances_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionAttendances_DrillSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "DrillSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionAttendances_Players_playerId",
                        column: x => x.playerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentGroupTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    TournamentTeamId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentGroupTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentGroupTeams_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentGroupTeams_TournamentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "TournamentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentGroupTeams_TournamentTeams_TournamentTeamId",
                        column: x => x.TournamentTeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentStandings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    TournamentTeamId = table.Column<int>(type: "int", nullable: false),
                    Played = table.Column<int>(type: "int", nullable: false),
                    Won = table.Column<int>(type: "int", nullable: false),
                    Drawn = table.Column<int>(type: "int", nullable: false),
                    Lost = table.Column<int>(type: "int", nullable: false),
                    GoalsFor = table.Column<int>(type: "int", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentStandings", x => x.Id);
                    table.CheckConstraint("CK_Standing_Drawn", "[Drawn] >= 0");
                    table.CheckConstraint("CK_Standing_Lost", "[Lost] >= 0");
                    table.CheckConstraint("CK_Standing_Played", "[Played] >= 0");
                    table.CheckConstraint("CK_Standing_Points", "[Points] >= 0");
                    table.CheckConstraint("CK_Standing_Won", "[Won] >= 0");
                    table.ForeignKey(
                        name: "FK_TournamentStandings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentStandings_TournamentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "TournamentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentStandings_TournamentTeams_TournamentTeamId",
                        column: x => x.TournamentTeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DrillResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DrillId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    ManualScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    DoneCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MissedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FinalScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CoachNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrillResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrillResults_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrillResults_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DrillResults_Drills_DrillId",
                        column: x => x.DrillId,
                        principalTable: "Drills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrillResults_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachUserId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AcademyId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    MatchId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachNotes_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachNotes_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachNotes_Coaches_CoachUserId",
                        column: x => x.CoachUserId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachNotes_DrillSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "DrillSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachNotes_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachNotes_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    AssistPlayerId = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Minute = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchEvents", x => x.Id);
                    table.CheckConstraint("CK_MatchEvent_Minute", "[Minute] >= 0 AND [Minute] <= 130");
                    table.CheckConstraint("CK_MatchEvent_Player_AssistPlayer", "[AssistPlayerId] IS NULL OR [PlayerId] <> [AssistPlayerId]");
                    table.ForeignKey(
                        name: "FK_MatchEvents_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MatchEvents_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchEvents_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchEvents_Players_AssistPlayerId",
                        column: x => x.AssistPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchEvents_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchEvents_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchLineups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    IsStarting = table.Column<bool>(type: "bit", nullable: false),
                    JerseyNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchLineups", x => x.Id);
                    table.CheckConstraint("CK_MatchLineup_JerseyNumber", "[JerseyNumber] IS NULL OR ([JerseyNumber] BETWEEN 1 AND 99)");
                    table.ForeignKey(
                        name: "FK_MatchLineups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchLineups_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchLineups_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchLineups_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchLineups_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchPlayerRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    CoachId = table.Column<int>(type: "int", nullable: false),
                    Goals = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Assists = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MinutesPlayed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsMOTM = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CoachNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPlayerRatings", x => x.Id);
                    table.CheckConstraint("CK_MatchPlayerRating_Assists", "[Assists] >= 0");
                    table.CheckConstraint("CK_MatchPlayerRating_Goals", "[Goals] >= 0");
                    table.CheckConstraint("CK_MatchPlayerRating_MinutesPlayed", "[MinutesPlayed] >= 0 AND [MinutesPlayed] <= 150");
                    table.ForeignKey(
                        name: "FK_MatchPlayerRatings_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayerRatings_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchPlayerRatings_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchPlayerRatings_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayerRatings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentFixtures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    RoundId = table.Column<int>(type: "int", nullable: true),
                    HomeTeamId = table.Column<int>(type: "int", nullable: false),
                    AwayTeamId = table.Column<int>(type: "int", nullable: false),
                    HomeScore = table.Column<int>(type: "int", nullable: true),
                    AwayScore = table.Column<int>(type: "int", nullable: true),
                    WinnerTeamId = table.Column<int>(type: "int", nullable: true),
                    LegNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TournamentGroupId = table.Column<int>(type: "int", nullable: true),
                    TournamentRoundId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentFixtures", x => x.Id);
                    table.CheckConstraint("CK_TournamentFixture_LegNumber", "[LegNumber] IN (1, 2)");
                    table.CheckConstraint("CK_TournamentFixture_RoundOrGroup", "([RoundId] IS NOT NULL AND [GroupId] IS NULL) OR ([GroupId] IS NOT NULL AND [RoundId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_TournamentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "TournamentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_TournamentGroups_TournamentGroupId",
                        column: x => x.TournamentGroupId,
                        principalTable: "TournamentGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_TournamentRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "TournamentRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_TournamentRounds_TournamentRoundId",
                        column: x => x.TournamentRoundId,
                        principalTable: "TournamentRounds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_TournamentTeams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_TournamentTeams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentFixtures_TournamentTeams_WinnerTeamId",
                        column: x => x.WinnerTeamId,
                        principalTable: "TournamentTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchPlayerCategoryRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchPlayerRatingId = table.Column<int>(type: "int", nullable: false),
                    DrillCategoryId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPlayerCategoryRatings", x => x.Id);
                    table.CheckConstraint("CK_MatchPlayerCategoryRating_Rating", "[Rating] >= 0 AND [Rating] <= 10");
                    table.ForeignKey(
                        name: "FK_MatchPlayerCategoryRatings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MatchPlayerCategoryRatings_DrillCategories_DrillCategoryId",
                        column: x => x.DrillCategoryId,
                        principalTable: "DrillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayerCategoryRatings_MatchPlayerRatings_MatchPlayerRatingId",
                        column: x => x.MatchPlayerRatingId,
                        principalTable: "MatchPlayerRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Academies_AdminUserId",
                table: "Academies",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Academies_CreatedByUserId",
                table: "Academies",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Academies_Name",
                table: "Academies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Academies_Status",
                table: "Academies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Academies_UpdatedByUserId",
                table: "Academies",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyAdmins_AcademyId",
                table: "AcademyAdmins",
                column: "AcademyId",
                unique: true,
                filter: "[AcademyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyAnnouncements_AcademyId",
                table: "AcademyAnnouncements",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyAnnouncements_CreatedByUserId",
                table: "AcademyAnnouncements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyAnnouncements_UpdatedByUserId",
                table: "AcademyAnnouncements",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyBadges_AcademyId",
                table: "AcademyBadges",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyBadges_CreatedByUserId",
                table: "AcademyBadges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyLocations_AcademyId",
                table: "AcademyLocations",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyLocations_CreatedByUserId",
                table: "AcademyLocations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyLocations_UpdatedByUserId",
                table: "AcademyLocations",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyRequests_CreatedByUserId",
                table: "AcademyRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyRequests_RequestedAt",
                table: "AcademyRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyRequests_RequestedById",
                table: "AcademyRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyRequests_RequestStatus",
                table: "AcademyRequests",
                column: "RequestStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyRequests_RequestStatus_RequestedAt",
                table: "AcademyRequests",
                columns: new[] { "RequestStatus", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademyRequests_ReviewedById",
                table: "AcademyRequests",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_AcademyRequests_UpdatedByUserId",
                table: "AcademyRequests",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgeGroups_AcademyId",
                table: "AgeGroups",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_AgeGroups_AcademyId_Name",
                table: "AgeGroups",
                columns: new[] { "AcademyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgeGroups_CreatedByUserId",
                table: "AgeGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIReports_AcademyId",
                table: "AIReports",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_AIReports_CreatedById",
                table: "AIReports",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AIReports_ReferenceId",
                table: "AIReports",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CreatedById",
                table: "AspNetUsers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FirstName",
                table: "AspNetUsers",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UpdatedById",
                table: "AspNetUsers",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CoachAcademies_AcademyId",
                table: "CoachAcademies",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachAcademies_CoachUserId",
                table: "CoachAcademies",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachAcademies_CreatedById",
                table: "CoachAcademies",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_AcademyId",
                table: "CoachNotes",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_CoachUserId",
                table: "CoachNotes",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_CreatedById",
                table: "CoachNotes",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_MatchId",
                table: "CoachNotes",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_PlayerId",
                table: "CoachNotes",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachNotes_SessionId",
                table: "CoachNotes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachTeams_CoachUserId",
                table: "CoachTeams",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachTeams_CreatedById",
                table: "CoachTeams",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CoachTeams_TeamId",
                table: "CoachTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachTempAccesses_CoachUserId",
                table: "CoachTempAccesses",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachTempAccesses_CreatedById",
                table: "CoachTempAccesses",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CoachTempAccesses_GrantedToUserId",
                table: "CoachTempAccesses",
                column: "GrantedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillCategories_CreatedByUserId",
                table: "DrillCategories",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillCategories_Name",
                table: "DrillCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrillCategories_UpdatedByUserId",
                table: "DrillCategories",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillResults_CreatedByUserId",
                table: "DrillResults",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillResults_DrillId",
                table: "DrillResults",
                column: "DrillId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillResults_DrillId_PlayerId",
                table: "DrillResults",
                columns: new[] { "DrillId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrillResults_PlayerId",
                table: "DrillResults",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillResults_UpdatedByUserId",
                table: "DrillResults",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_CreatedByUserId",
                table: "Drills",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_DrillTemplateId",
                table: "Drills",
                column: "DrillTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_SessionId",
                table: "Drills",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Drills_UpdatedByUserId",
                table: "Drills",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_AcademyId",
                table: "DrillSessions",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_AcademyId_SessionDate",
                table: "DrillSessions",
                columns: new[] { "AcademyId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_CoachId",
                table: "DrillSessions",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_CreatedByUserId",
                table: "DrillSessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_SessionDate",
                table: "DrillSessions",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_TeamId",
                table: "DrillSessions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_TeamId_SessionDate",
                table: "DrillSessions",
                columns: new[] { "TeamId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_Type",
                table: "DrillSessions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_DrillSessions_UpdatedByUserId",
                table: "DrillSessions",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillTemplates_AcademyId",
                table: "DrillTemplates",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillTemplates_AcademyId_IsShared",
                table: "DrillTemplates",
                columns: new[] { "AcademyId", "IsShared" });

            migrationBuilder.CreateIndex(
                name: "IX_DrillTemplates_CategoryId",
                table: "DrillTemplates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillTemplates_CreatedByUserId",
                table: "DrillTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DrillTemplates_IsShared",
                table: "DrillTemplates",
                column: "IsShared");

            migrationBuilder.CreateIndex(
                name: "IX_DrillTemplates_UpdatedByUserId",
                table: "DrillTemplates",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_CreatedById",
                table: "Matches",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId_AwayTeamId_MatchDate",
                table: "Matches",
                columns: new[] { "HomeTeamId", "AwayTeamId", "MatchDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatchDate",
                table: "Matches",
                column: "MatchDate");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_SessionId",
                table: "Matches",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Status",
                table: "Matches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TournamentId",
                table: "Matches",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_UpdatedByUserId",
                table: "Matches",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinningTeamId",
                table: "Matches",
                column: "WinningTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_AssistPlayerId",
                table: "MatchEvents",
                column: "AssistPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_CreatedById",
                table: "MatchEvents",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_MatchId",
                table: "MatchEvents",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_MatchId_EventType",
                table: "MatchEvents",
                columns: new[] { "MatchId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_MatchId_PlayerId",
                table: "MatchEvents",
                columns: new[] { "MatchId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_PlayerId",
                table: "MatchEvents",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_TeamId",
                table: "MatchEvents",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchEvents_UpdatedByUserId",
                table: "MatchEvents",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_CreatedByUserId",
                table: "MatchLineups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_MatchId_IsStarting",
                table: "MatchLineups",
                columns: new[] { "MatchId", "IsStarting" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_MatchId_PlayerId_TeamId",
                table: "MatchLineups",
                columns: new[] { "MatchId", "PlayerId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_PlayerId",
                table: "MatchLineups",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_TeamId",
                table: "MatchLineups",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_UpdatedByUserId",
                table: "MatchLineups",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerCategoryRatings_CreatedByUserId",
                table: "MatchPlayerCategoryRatings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerCategoryRatings_DrillCategoryId",
                table: "MatchPlayerCategoryRatings",
                column: "DrillCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerCategoryRatings_MatchPlayerRatingId_DrillCategoryId",
                table: "MatchPlayerCategoryRatings",
                columns: new[] { "MatchPlayerRatingId", "DrillCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerRatings_CoachId",
                table: "MatchPlayerRatings",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerRatings_CreatedById",
                table: "MatchPlayerRatings",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerRatings_MatchId",
                table: "MatchPlayerRatings",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerRatings_MatchId_PlayerId",
                table: "MatchPlayerRatings",
                columns: new[] { "MatchId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerRatings_PlayerId_IsMOTM",
                table: "MatchPlayerRatings",
                columns: new[] { "PlayerId", "IsMOTM" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayerRatings_UpdatedByUserId",
                table: "MatchPlayerRatings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPlayers_CreatedByUserId",
                table: "ParentPlayers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentPlayers_ParentId_PlayerId",
                table: "ParentPlayers",
                columns: new[] { "ParentId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentPlayers_PlayerId",
                table: "ParentPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Parents_PlayerId",
                table: "Parents",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_Action",
                table: "PlatformAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_CreatedByUserId",
                table: "PlatformAuditLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_TargetEntity",
                table: "PlatformAuditLogs",
                column: "TargetEntity");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_TargetEntity_TargetEntityId",
                table: "PlatformAuditLogs",
                columns: new[] { "TargetEntity", "TargetEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_TargetEntityId",
                table: "PlatformAuditLogs",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAuditLogs_UpdatedByUserId",
                table: "PlatformAuditLogs",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSettings_CreatedByUserId",
                table: "PlatformSettings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSettings_Key",
                table: "PlatformSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSettings_UpdatedByUserId",
                table: "PlatformSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAcademies_AcademyId",
                table: "PlayerAcademies",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAcademies_CreatedByUserId",
                table: "PlayerAcademies",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAcademies_PlayerId_AcademyId_LeftAt",
                table: "PlayerAcademies",
                columns: new[] { "PlayerId", "AcademyId", "LeftAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_CreatedById",
                table: "PlayerAchievements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_PlayerId",
                table: "PlayerAchievements",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCards_CreatedByUserId",
                table: "PlayerCards",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCards_PlayerId",
                table: "PlayerCards",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCategoryRatings_CreatedByUserId",
                table: "PlayerCategoryRatings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCategoryRatings_DrillCategoryId",
                table: "PlayerCategoryRatings",
                column: "DrillCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCategoryRatings_PlayerCardId_DrillCategoryId",
                table: "PlayerCategoryRatings",
                columns: new[] { "PlayerCardId", "DrillCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGoals_AcademyId",
                table: "PlayerGoals",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGoals_CreatedById",
                table: "PlayerGoals",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGoals_PlayerId",
                table: "PlayerGoals",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHighlights_AcademyId",
                table: "PlayerHighlights",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHighlights_CreatedById",
                table: "PlayerHighlights",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHighlights_PlayerId",
                table: "PlayerHighlights",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPositions_CreatedById",
                table: "PlayerPositions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPositions_PlayerId",
                table: "PlayerPositions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSubscriptions_AcademyId",
                table: "PlayerSubscriptions",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSubscriptions_CreatedById",
                table: "PlayerSubscriptions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSubscriptions_PaidByUserId",
                table: "PlayerSubscriptions",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSubscriptions_PlayerId",
                table: "PlayerSubscriptions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeams_CreatedByUserId",
                table: "PlayerTeams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeams_PlayerId",
                table: "PlayerTeams",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeams_TeamId",
                table: "PlayerTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuditLogs_AcademyId",
                table: "RoleAuditLogs",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuditLogs_AffectedUserId",
                table: "RoleAuditLogs",
                column: "AffectedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuditLogs_CreatedByUserId",
                table: "RoleAuditLogs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuditLogs_PerformedByUserId",
                table: "RoleAuditLogs",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuditLogs_UpdatedByUserId",
                table: "RoleAuditLogs",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterFollows_CreatedById",
                table: "ScouterFollows",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterFollows_PlayerId",
                table: "ScouterFollows",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterFollows_ScouterUserId",
                table: "ScouterFollows",
                column: "ScouterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterReports_CreatedById",
                table: "ScouterReports",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterReports_PlayerId",
                table: "ScouterReports",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterReports_ScouterUserId",
                table: "ScouterReports",
                column: "ScouterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterShortlists_CreatedById",
                table: "ScouterShortlists",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterShortlists_PlayerId",
                table: "ScouterShortlists",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterShortlists_ScouterUserId",
                table: "ScouterShortlists",
                column: "ScouterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterViews_CreatedById",
                table: "ScouterViews",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterViews_PlayerId",
                table: "ScouterViews",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScouterViews_ScouterId",
                table: "ScouterViews",
                column: "ScouterId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttendances_CreatedByUserId",
                table: "SessionAttendances",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttendances_playerId",
                table: "SessionAttendances",
                column: "playerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttendances_SessionId",
                table: "SessionAttendances",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttendances_SessionId_playerId",
                table: "SessionAttendances",
                columns: new[] { "SessionId", "playerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttendances_UpdatedByUserId",
                table: "SessionAttendances",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_AgeGroupId_Name",
                table: "Teams",
                columns: new[] { "AgeGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatedByUserId",
                table: "Teams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LocationId",
                table: "Teams",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_UpdatedByUserId",
                table: "Teams",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_AwayTeamId",
                table: "TournamentFixtures",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_CreatedByUserId",
                table: "TournamentFixtures",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_GroupId",
                table: "TournamentFixtures",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_HomeTeamId_AwayTeamId",
                table: "TournamentFixtures",
                columns: new[] { "HomeTeamId", "AwayTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_MatchId",
                table: "TournamentFixtures",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_RoundId",
                table: "TournamentFixtures",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_TournamentGroupId",
                table: "TournamentFixtures",
                column: "TournamentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_TournamentRoundId",
                table: "TournamentFixtures",
                column: "TournamentRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentFixtures_WinnerTeamId",
                table: "TournamentFixtures",
                column: "WinnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroups_CreatedByUserId",
                table: "TournamentGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroups_TournamentId",
                table: "TournamentGroups",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroups_UpdatedByUserId",
                table: "TournamentGroups",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroupTeams_CreatedByUserId",
                table: "TournamentGroupTeams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroupTeams_GroupId_TournamentTeamId",
                table: "TournamentGroupTeams",
                columns: new[] { "GroupId", "TournamentTeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentGroupTeams_TournamentTeamId",
                table: "TournamentGroupTeams",
                column: "TournamentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentHallOfFames_CreatedByUserId",
                table: "TournamentHallOfFames",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentHallOfFames_PlayerId",
                table: "TournamentHallOfFames",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentHallOfFames_TournamentId_PlayerId_AwardType",
                table: "TournamentHallOfFames",
                columns: new[] { "TournamentId", "PlayerId", "AwardType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentHallOfFames_UpdatedByUserId",
                table: "TournamentHallOfFames",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRounds_CreatedByUserId",
                table: "TournamentRounds",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRounds_TournamentId",
                table: "TournamentRounds",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRounds_TournamentId_RoundNumber",
                table: "TournamentRounds",
                columns: new[] { "TournamentId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_AgeGroupId",
                table: "Tournaments",
                column: "AgeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_CreatedByUserId",
                table: "Tournaments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Name",
                table: "Tournaments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Status",
                table: "Tournaments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_UpdatedByUserId",
                table: "Tournaments",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentSquads_CreatedByUserId",
                table: "TournamentSquads",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentSquads_PlayerId",
                table: "TournamentSquads",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentSquads_TeamId",
                table: "TournamentSquads",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentSquads_TournamentId_PlayerId",
                table: "TournamentSquads",
                columns: new[] { "TournamentId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentSquads_TournamentId_TeamId",
                table: "TournamentSquads",
                columns: new[] { "TournamentId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentSquads_UpdatedByUserId",
                table: "TournamentSquads",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStandings_CreatedByUserId",
                table: "TournamentStandings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStandings_GroupId_TournamentTeamId",
                table: "TournamentStandings",
                columns: new[] { "GroupId", "TournamentTeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentStandings_TournamentTeamId",
                table: "TournamentStandings",
                column: "TournamentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_CreatedByUserId",
                table: "TournamentTeams",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_Status",
                table: "TournamentTeams",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TeamId",
                table: "TournamentTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_TournamentId_TeamId",
                table: "TournamentTeams",
                columns: new[] { "TournamentId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTeams_UpdatedByUserId",
                table: "TournamentTeams",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademyAdmins");

            migrationBuilder.DropTable(
                name: "AcademyAnnouncements");

            migrationBuilder.DropTable(
                name: "AcademyBadges");

            migrationBuilder.DropTable(
                name: "AcademyRequests");

            migrationBuilder.DropTable(
                name: "AIReports");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CoachAcademies");

            migrationBuilder.DropTable(
                name: "CoachNotes");

            migrationBuilder.DropTable(
                name: "CoachTeams");

            migrationBuilder.DropTable(
                name: "CoachTempAccesses");

            migrationBuilder.DropTable(
                name: "DrillResults");

            migrationBuilder.DropTable(
                name: "MatchEvents");

            migrationBuilder.DropTable(
                name: "MatchLineups");

            migrationBuilder.DropTable(
                name: "MatchPlayerCategoryRatings");

            migrationBuilder.DropTable(
                name: "ParentPlayers");

            migrationBuilder.DropTable(
                name: "PlatformAuditLogs");

            migrationBuilder.DropTable(
                name: "PlatformSettings");

            migrationBuilder.DropTable(
                name: "PlayerAcademies");

            migrationBuilder.DropTable(
                name: "PlayerAchievements");

            migrationBuilder.DropTable(
                name: "PlayerCategoryRatings");

            migrationBuilder.DropTable(
                name: "PlayerGoals");

            migrationBuilder.DropTable(
                name: "PlayerHighlights");

            migrationBuilder.DropTable(
                name: "PlayerPositions");

            migrationBuilder.DropTable(
                name: "PlayerSubscriptions");

            migrationBuilder.DropTable(
                name: "PlayerTeams");

            migrationBuilder.DropTable(
                name: "RoleAuditLogs");

            migrationBuilder.DropTable(
                name: "ScouterFollows");

            migrationBuilder.DropTable(
                name: "ScouterReports");

            migrationBuilder.DropTable(
                name: "ScouterShortlists");

            migrationBuilder.DropTable(
                name: "ScouterViews");

            migrationBuilder.DropTable(
                name: "SessionAttendances");

            migrationBuilder.DropTable(
                name: "SystemAdmins");

            migrationBuilder.DropTable(
                name: "TournamentFixtures");

            migrationBuilder.DropTable(
                name: "TournamentGroupTeams");

            migrationBuilder.DropTable(
                name: "TournamentHallOfFames");

            migrationBuilder.DropTable(
                name: "TournamentSquads");

            migrationBuilder.DropTable(
                name: "TournamentStandings");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Coaches");

            migrationBuilder.DropTable(
                name: "Drills");

            migrationBuilder.DropTable(
                name: "MatchPlayerRatings");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "PlayerCards");

            migrationBuilder.DropTable(
                name: "Scouters");

            migrationBuilder.DropTable(
                name: "TournamentRounds");

            migrationBuilder.DropTable(
                name: "TournamentGroups");

            migrationBuilder.DropTable(
                name: "TournamentTeams");

            migrationBuilder.DropTable(
                name: "DrillTemplates");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "DrillCategories");

            migrationBuilder.DropTable(
                name: "DrillSessions");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "AcademyLocations");

            migrationBuilder.DropTable(
                name: "AgeGroups");

            migrationBuilder.DropTable(
                name: "Academies");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
