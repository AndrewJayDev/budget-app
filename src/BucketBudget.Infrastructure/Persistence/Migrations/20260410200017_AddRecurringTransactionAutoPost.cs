using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BucketBudget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTransactionAutoPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPost",
                table: "RecurringTransactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPost",
                table: "RecurringTransactions");
        }
    }
}
