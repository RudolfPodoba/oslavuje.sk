using Microsoft.Data.SqlClient;
using oslavuje.sk.Models;

namespace oslavuje.sk.Repositories;

public class MemberDataRepository
{
    private readonly string _connectionString;

    public MemberDataRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN") ??
            throw new InvalidOperationException("Connection string 'umbracoDbDSN' not found.");
    }

    public async Task SaveUserDataAsync(MemberData data)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = new SqlCommand(
                "INSERT INTO MemberData (MemberId, Email, UserInput, DateCreated) " +
                "VALUES (@MemberId, @Email, @UserInput, @DateCreated)", connection);

            command.Parameters.AddWithValue("@MemberId", data.MemberId);
            command.Parameters.AddWithValue("@Email", data.Email);
            command.Parameters.AddWithValue("@UserInput", data.UserInput);
            command.Parameters.AddWithValue("@DateCreated", data.DateCreated);

            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<MemberData>> GetMemberDataAsync(string memberId)
    {
        var result = new List<MemberData>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = new SqlCommand(
                "SELECT Id, MemberId, Email, UserInput, DateCreated " +
                "FROM MemberData WHERE MemberId = @MemberId " +
                "ORDER BY DateCreated DESC", connection);

            command.Parameters.AddWithValue("@MemberId", memberId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new MemberData
                {
                    Id = reader.GetInt32(0),
                    MemberId = reader.GetString(1),
                    Email = reader.GetString(2),
                    UserInput = reader.GetString(3),
                    DateCreated = reader.GetDateTime(4)
                });
            }
        }

        return result;
    }
}
