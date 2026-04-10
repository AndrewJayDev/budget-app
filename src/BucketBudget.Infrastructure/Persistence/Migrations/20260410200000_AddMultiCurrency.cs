using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BucketBudget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add RateType to ExchangeRates
            migrationBuilder.AddColumn<string>(
                name: "RateType",
                table: "ExchangeRates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Official");

            // 2. Rebuild the unique index to include RateType
            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_FromCurrencyCode_ToCurrencyCode_EffectiveDate",
                table: "ExchangeRates");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_FromCurrencyCode_ToCurrencyCode_RateType_EffectiveDate",
                table: "ExchangeRates",
                columns: new[] { "FromCurrencyCode", "ToCurrencyCode", "RateType", "EffectiveDate" },
                unique: true);

            // 3. Add cross-currency fields to Transactions
            migrationBuilder.AddColumn<Guid>(
                name: "ExchangeRateId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferPairId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            // 4. FK from Transactions.ExchangeRateId -> ExchangeRates.Id
            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ExchangeRates_ExchangeRateId",
                table: "Transactions",
                column: "ExchangeRateId",
                principalTable: "ExchangeRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // 5. Indexes
            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExchangeRateId",
                table: "Transactions",
                column: "ExchangeRateId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransferPairId",
                table: "Transactions",
                column: "TransferPairId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ExchangeRates_ExchangeRateId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ExchangeRateId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransferPairId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExchangeRateId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransferPairId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_FromCurrencyCode_ToCurrencyCode_RateType_EffectiveDate",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "RateType",
                table: "ExchangeRates");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_FromCurrencyCode_ToCurrencyCode_EffectiveDate",
                table: "ExchangeRates",
                columns: new[] { "FromCurrencyCode", "ToCurrencyCode", "EffectiveDate" },
                unique: true);
        }
    }
}
