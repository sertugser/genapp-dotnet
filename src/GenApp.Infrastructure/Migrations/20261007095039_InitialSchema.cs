using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GenApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer",
                columns: table => new
                {
                    customer_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000001', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    last_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    house_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    house_number = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    postcode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    phone_home = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone_mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer", x => x.customer_number);
                });

            migrationBuilder.CreateTable(
                name: "customer_secure",
                columns: table => new
                {
                    customer_number = table.Column<int>(type: "integer", nullable: false),
                    customer_pass = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    state_indicator = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    pass_changes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_secure", x => x.customer_number);
                    table.ForeignKey(
                        name: "fk_customer_secure_customer_customer_number",
                        column: x => x.customer_number,
                        principalTable: "customer",
                        principalColumn: "customer_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "policy",
                columns: table => new
                {
                    policy_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000001', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_number = table.Column<int>(type: "integer", nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    policy_type = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    last_changed = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    broker_id = table.Column<int>(type: "integer", nullable: true),
                    brokers_reference = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    payment = table.Column<int>(type: "integer", nullable: true),
                    commission = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_policy", x => x.policy_number);
                    table.ForeignKey(
                        name: "fk_policy_customer_customer_number",
                        column: x => x.customer_number,
                        principalTable: "customer",
                        principalColumn: "customer_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claim",
                columns: table => new
                {
                    claim_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000001', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    policy_number = table.Column<int>(type: "integer", nullable: false),
                    claim_date = table.Column<DateOnly>(type: "date", nullable: true),
                    paid = table.Column<int>(type: "integer", nullable: true),
                    value = table.Column<int>(type: "integer", nullable: true),
                    cause = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observations = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_claim", x => x.claim_number);
                    table.ForeignKey(
                        name: "fk_claim_policy_policy_number",
                        column: x => x.policy_number,
                        principalTable: "policy",
                        principalColumn: "policy_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commercial",
                columns: table => new
                {
                    policy_number = table.Column<int>(type: "integer", nullable: false),
                    request_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    renewal_date = table.Column<DateOnly>(type: "date", nullable: true),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    zipcode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    latitude_n = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    longitude_w = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    customer = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    property_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    fire_peril = table.Column<short>(type: "smallint", nullable: true),
                    fire_premium = table.Column<int>(type: "integer", nullable: true),
                    crime_peril = table.Column<short>(type: "smallint", nullable: true),
                    crime_premium = table.Column<int>(type: "integer", nullable: true),
                    flood_peril = table.Column<short>(type: "smallint", nullable: true),
                    flood_premium = table.Column<int>(type: "integer", nullable: true),
                    weather_peril = table.Column<short>(type: "smallint", nullable: true),
                    weather_premium = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commercial", x => x.policy_number);
                    table.ForeignKey(
                        name: "fk_commercial_policy_policy_number",
                        column: x => x.policy_number,
                        principalTable: "policy",
                        principalColumn: "policy_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "endowment",
                columns: table => new
                {
                    policy_number = table.Column<int>(type: "integer", nullable: false),
                    equities = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    with_profits = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    managed_fund = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    fund_name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    term = table.Column<short>(type: "smallint", nullable: true),
                    sum_assured = table.Column<int>(type: "integer", nullable: true),
                    life_assured = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: true),
                    padding_data = table.Column<string>(type: "character varying(32606)", maxLength: 32606, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_endowment", x => x.policy_number);
                    table.ForeignKey(
                        name: "fk_endowment_policy_policy_number",
                        column: x => x.policy_number,
                        principalTable: "policy",
                        principalColumn: "policy_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "house",
                columns: table => new
                {
                    policy_number = table.Column<int>(type: "integer", nullable: false),
                    property_type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    bedrooms = table.Column<short>(type: "smallint", nullable: true),
                    value = table.Column<int>(type: "integer", nullable: true),
                    house_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    house_number = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    postcode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_house", x => x.policy_number);
                    table.ForeignKey(
                        name: "fk_house_policy_policy_number",
                        column: x => x.policy_number,
                        principalTable: "policy",
                        principalColumn: "policy_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motor",
                columns: table => new
                {
                    policy_number = table.Column<int>(type: "integer", nullable: false),
                    make = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    model = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    value = table.Column<int>(type: "integer", nullable: true),
                    reg_number = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    colour = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    cc = table.Column<short>(type: "smallint", nullable: true),
                    year_of_manufacture = table.Column<DateOnly>(type: "date", nullable: true),
                    premium = table.Column<int>(type: "integer", nullable: true),
                    accidents = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_motor", x => x.policy_number);
                    table.ForeignKey(
                        name: "fk_motor_policy_policy_number",
                        column: x => x.policy_number,
                        principalTable: "policy",
                        principalColumn: "policy_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_claim_policy_number",
                table: "claim",
                column: "policy_number");

            migrationBuilder.CreateIndex(
                name: "ix_policy_customer_number",
                table: "policy",
                column: "customer_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "claim");

            migrationBuilder.DropTable(
                name: "commercial");

            migrationBuilder.DropTable(
                name: "customer_secure");

            migrationBuilder.DropTable(
                name: "endowment");

            migrationBuilder.DropTable(
                name: "house");

            migrationBuilder.DropTable(
                name: "motor");

            migrationBuilder.DropTable(
                name: "policy");

            migrationBuilder.DropTable(
                name: "customer");
        }
    }
}
