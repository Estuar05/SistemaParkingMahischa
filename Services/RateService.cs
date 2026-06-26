using Microsoft.Data.SqlClient;
using SistemaParkingMahischa.Data;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Services;

public sealed class RateService
{
    public List<ParkingRate> GetRates(bool activeOnly = false)
    {
        var rates = new List<ParkingRate>();
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT RateId, RateName, RateType, Amount, GraceMinutes, IsActive, SortOrder, BlockMinutes, BlockAmount
            FROM dbo.ParkingRates
            {(activeOnly ? "WHERE IsActive = 1" : string.Empty)}
            ORDER BY IsActive DESC, SortOrder, RateName;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rates.Add(MapRate(reader));
        }

        return rates;
    }

    public ParkingRate GetRate(int rateId)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RateId, RateName, RateType, Amount, GraceMinutes, IsActive, SortOrder, BlockMinutes, BlockAmount
            FROM dbo.ParkingRates
            WHERE RateId = @RateId;
            """;
        command.Parameters.AddWithValue("@RateId", rateId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("La tarifa seleccionada no existe.");
        }

        return MapRate(reader);
    }

    public void SaveRate(ParkingRate rate, int actingUserId)
    {
        var isNew = rate.RateId == 0;
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        if (rate.RateId == 0)
        {
            command.CommandText = """
                INSERT INTO dbo.ParkingRates(RateName, RateType, Amount, GraceMinutes, IsActive, SortOrder, BlockMinutes, BlockAmount)
                VALUES (@RateName, @RateType, @Amount, @GraceMinutes, @IsActive, @SortOrder, @BlockMinutes, @BlockAmount);
                """;
        }
        else
        {
            command.CommandText = """
                UPDATE dbo.ParkingRates
                SET RateName = @RateName,
                    RateType = @RateType,
                    Amount = @Amount,
                    GraceMinutes = @GraceMinutes,
                    IsActive = @IsActive,
                    SortOrder = @SortOrder,
                    BlockMinutes = @BlockMinutes,
                    BlockAmount = @BlockAmount,
                    UpdatedAt = SYSDATETIME()
                WHERE RateId = @RateId;
                """;
            command.Parameters.AddWithValue("@RateId", rate.RateId);
        }

        command.Parameters.AddWithValue("@RateName", rate.Name.Trim());
        command.Parameters.AddWithValue("@RateType", rate.RateType);
        command.Parameters.AddWithValue("@Amount", rate.Amount);
        command.Parameters.AddWithValue("@GraceMinutes", rate.GraceMinutes);
        command.Parameters.AddWithValue("@IsActive", rate.IsActive);
        command.Parameters.AddWithValue("@SortOrder", rate.SortOrder);
        command.Parameters.AddWithValue("@BlockMinutes", (object?)rate.BlockMinutes ?? DBNull.Value);
        command.Parameters.AddWithValue("@BlockAmount", (object?)rate.BlockAmount ?? DBNull.Value);
        command.ExecuteNonQuery();
        AuditService.Log(actingUserId, isNew ? "CrearTarifa" : "EditarTarifa", "ParkingRates",
            rate.RateId == 0 ? null : rate.RateId.ToString(), $"{rate.Name} ({rate.RateType}) {rate.Amount:0.00}");
    }

    private static ParkingRate MapRate(SqlDataReader reader) => new()
    {
        RateId = reader.GetInt32(reader.GetOrdinal("RateId")),
        Name = reader.GetString(reader.GetOrdinal("RateName")),
        RateType = reader.GetString(reader.GetOrdinal("RateType")),
        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
        GraceMinutes = reader.GetInt32(reader.GetOrdinal("GraceMinutes")),
        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
        SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
        BlockMinutes = reader.IsDBNull(reader.GetOrdinal("BlockMinutes")) ? null : reader.GetInt32(reader.GetOrdinal("BlockMinutes")),
        BlockAmount = reader.IsDBNull(reader.GetOrdinal("BlockAmount")) ? null : reader.GetDecimal(reader.GetOrdinal("BlockAmount"))
    };
}

