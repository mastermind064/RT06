using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RTMultiTenant.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rt",
                columns: table => new
                {
                    rt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rt_number = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rw_number = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    village_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subdistrict_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    province_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    address_detail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rt", x => x.rt_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "monthly_cash_summary",
                columns: table => new
                {
                    summary_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    year = table.Column<int>(type: "int", nullable: false),
                    month = table.Column<int>(type: "int", nullable: false),
                    total_contribution_in = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    total_expense_out = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    balance_end = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_cash_summary", x => x.summary_id);
                    table.ForeignKey(
                        name: "FK_monthly_cash_summary_rt_rt_id",
                        column: x => x.rt_id,
                        principalTable: "rt",
                        principalColumn: "rt_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "residents",
                columns: table => new
                {
                    resident_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    national_id_number = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    birth_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    gender = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    address = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone_number = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kk_document_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approval_status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approval_note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_residents", x => x.resident_id);
                    table.ForeignKey(
                        name: "FK_residents_rt_rt_id",
                        column: x => x.rt_id,
                        principalTable: "rt",
                        principalColumn: "rt_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contributions",
                columns: table => new
                {
                    contribution_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    resident_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    period_start = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    period_end = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    payment_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    proof_image_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    admin_note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contributions", x => x.contribution_id);
                    table.ForeignKey(
                        name: "FK_contributions_residents_resident_id",
                        column: x => x.resident_id,
                        principalTable: "residents",
                        principalColumn: "resident_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contributions_rt_rt_id",
                        column: x => x.rt_id,
                        principalTable: "rt",
                        principalColumn: "rt_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "resident_family_members",
                columns: table => new
                {
                    family_member_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    resident_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    birth_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    gender = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    relationship = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resident_family_members", x => x.family_member_id);
                    table.ForeignKey(
                        name: "FK_resident_family_members_residents_resident_id",
                        column: x => x.resident_id,
                        principalTable: "residents",
                        principalColumn: "resident_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resident_family_members_rt_rt_id",
                        column: x => x.rt_id,
                        principalTable: "rt",
                        principalColumn: "rt_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    resident_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_users_residents_resident_id",
                        column: x => x.resident_id,
                        principalTable: "residents",
                        principalColumn: "resident_id");
                    table.ForeignKey(
                        name: "FK_users_rt_rt_id",
                        column: x => x.rt_id,
                        principalTable: "rt",
                        principalColumn: "rt_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cash_expenses",
                columns: table => new
                {
                    expense_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    rt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    expense_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_expenses", x => x.expense_id);
                    table.ForeignKey(
                        name: "FK_cash_expenses_rt_rt_id",
                        column: x => x.rt_id,
                        principalTable: "rt",
                        principalColumn: "rt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cash_expenses_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "event_store",
                columns: table => new
                {
                    event_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    rt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    aggregate_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    aggregate_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    event_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    event_payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    caused_by_user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    aggregate_version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_store", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_event_store_rt_rt_id",
                        column: x => x.rt_id,
                        principalTable: "rt",
                        principalColumn: "rt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_event_store_users_caused_by_user_id",
                        column: x => x.caused_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_rt_active",
                table: "cash_expenses",
                columns: new[] { "rt_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_rt_expense_date",
                table: "cash_expenses",
                columns: new[] { "rt_id", "expense_date" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_expenses_created_by_user_id",
                table: "cash_expenses",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_rt_contrib",
                table: "contributions",
                column: "rt_id");

            migrationBuilder.CreateIndex(
                name: "idx_rt_period",
                table: "contributions",
                columns: new[] { "rt_id", "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "idx_rt_resident",
                table: "contributions",
                columns: new[] { "rt_id", "resident_id" });

            migrationBuilder.CreateIndex(
                name: "idx_rt_status",
                table: "contributions",
                columns: new[] { "rt_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_contributions_resident_id",
                table: "contributions",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "idx_rt_agg_ver",
                table: "event_store",
                columns: new[] { "rt_id", "aggregate_id", "aggregate_version" });

            migrationBuilder.CreateIndex(
                name: "idx_rt_type_time",
                table: "event_store",
                columns: new[] { "rt_id", "aggregate_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_rt_user_time",
                table: "event_store",
                columns: new[] { "rt_id", "caused_by_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_event_store_caused_by_user_id",
                table: "event_store",
                column: "caused_by_user_id");

            migrationBuilder.CreateIndex(
                name: "uq_rt_year_month",
                table: "monthly_cash_summary",
                columns: new[] { "rt_id", "year", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_rt_family",
                table: "resident_family_members",
                columns: new[] { "rt_id", "resident_id" });

            migrationBuilder.CreateIndex(
                name: "IX_resident_family_members_resident_id",
                table: "resident_family_members",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "idx_rt_resident",
                table: "residents",
                column: "rt_id");

            migrationBuilder.CreateIndex(
                name: "idx_rt_status",
                table: "residents",
                columns: new[] { "rt_id", "approval_status" });

            migrationBuilder.CreateIndex(
                name: "uq_rt_context",
                table: "rt",
                columns: new[] { "rt_number", "rw_number", "village_name", "subdistrict_name", "city_name", "province_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_resident_id",
                table: "users",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_rt_id",
                table: "users",
                column: "rt_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_expenses");

            migrationBuilder.DropTable(
                name: "contributions");

            migrationBuilder.DropTable(
                name: "event_store");

            migrationBuilder.DropTable(
                name: "monthly_cash_summary");

            migrationBuilder.DropTable(
                name: "resident_family_members");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "residents");

            migrationBuilder.DropTable(
                name: "rt");
        }
    }
}
