using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;

using oslavuje.sk.Models.GiftRegistry;

namespace oslavuje.sk.Repositories;

public class GiftRegistryRepository
{
    private readonly string _connectionString;
    private readonly ILogger<GiftRegistryRepository> _logger;

    public GiftRegistryRepository(
        IConfiguration configuration,
        ILogger<GiftRegistryRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN") ??
            throw new InvalidOperationException("Connection string 'umbracoDbDSN' not found.");
        _logger = logger;
    }

    public async Task<bool> SubdomainExistsAsync(string subdomain)
    {
        var registry = await GetRegistryBySubdomainAsync(subdomain);
        return registry != null;
    }

    // Get registry by owner ID
    public async Task<SubdomainGiftRegistry?> GetRegistryByOwnerIdAsync(string ownerId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                "SELECT * FROM SubdomainGiftRegistry WHERE OwnerId = @OwnerId",
                connection);
            command.Parameters.AddWithValue("@OwnerId", ownerId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SubdomainGiftRegistry
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SubdomainName = reader.GetString(reader.GetOrdinal("SubdomainName")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    OwnerId = reader.GetInt32(reader.GetOrdinal("OwnerId")),
                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                    ExpiryDate = reader.IsDBNull(reader.GetOrdinal("ExpiryDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gift registry for owner {OwnerId}", ownerId);
            return null;
        }
    }

    public async Task<SubdomainGiftRegistry?> GetRegistryByIdAsync(int registryId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                "SELECT * FROM SubdomainGiftRegistry WHERE Id = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", registryId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SubdomainGiftRegistry
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SubdomainName = reader.GetString(reader.GetOrdinal("SubdomainName")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    OwnerId = reader.GetInt32(reader.GetOrdinal("OwnerId")),
                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                    ExpiryDate = reader.IsDBNull(reader.GetOrdinal("ExpiryDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gift registry by ID {RegistryId}", registryId);
            return null;
        }
    }


    // Update existing registry
    public async Task<bool> UpdateRegistryAsync(SubdomainGiftRegistry registry)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                @"UPDATE SubdomainGiftRegistry 
            SET Title = @Title, 
                Description = @Description, 
                ExpiryDate = @ExpiryDate
            WHERE Id = @Id",
                connection);

            command.Parameters.AddWithValue("@Id", registry.Id);
            command.Parameters.AddWithValue("@Title", registry.Title);
            command.Parameters.AddWithValue("@Description", registry.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ExpiryDate", registry.ExpiryDate ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gift registry");
            return false;
        }
    }

    // Update existing gift
    public async Task<bool> UpdateGiftAsync(Gift gift)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                @"UPDATE Gift 
            SET Name = @Name, 
                Description = @Description, 
                Price = @Price, 
                ImageUrl = @ImageUrl
            WHERE Id = @Id AND RegistryId = @RegistryId",
                connection);

            command.Parameters.AddWithValue("@Id", gift.Id);
            command.Parameters.AddWithValue("@Name", gift.Name);
            command.Parameters.AddWithValue("@Description", gift.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Price", gift.Price ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ImageUrl", gift.ImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RegistryId", gift.RegistryId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gift");
            return false;
        }
    }

    // Delete a gift registry and all associated gifts
    public async Task<bool> DeleteRegistryAsync(int registryId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Begin transaction because we're deleting from multiple tables
            using var transaction = connection.BeginTransaction();
            try
            {
                // First, delete all gifts associated with this registry
                var deleteGiftsCommand = new SqlCommand(
                    "DELETE FROM Gift WHERE RegistryId = @RegistryId",
                    connection, transaction);
                deleteGiftsCommand.Parameters.AddWithValue("@RegistryId", registryId);
                await deleteGiftsCommand.ExecuteNonQueryAsync();

                // Then delete the registry itself
                var deleteRegistryCommand = new SqlCommand(
                    "DELETE FROM SubdomainGiftRegistry WHERE Id = @Id",
                    connection, transaction);
                deleteRegistryCommand.Parameters.AddWithValue("@Id", registryId);
                await deleteRegistryCommand.ExecuteNonQueryAsync();

                // Commit transaction
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                // Rollback on error
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gift registry");
            return false;
        }
    }

    // Get registry by subdomain
    public async Task<SubdomainGiftRegistry?> GetRegistryBySubdomainAsync(string subdomain)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                "SELECT * FROM SubdomainGiftRegistry WHERE SubdomainName = @SubdomainName",
                connection);
            command.Parameters.AddWithValue("@SubdomainName", subdomain);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SubdomainGiftRegistry
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SubdomainName = reader.GetString(reader.GetOrdinal("SubdomainName")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    OwnerId = reader.GetInt32(reader.GetOrdinal("OwnerId")),
                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                    ExpiryDate = reader.IsDBNull(reader.GetOrdinal("ExpiryDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ExpiryDate")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gift registry for subdomain {Subdomain}", subdomain);
            return null;
        }
    }

    // Delete a specific gift
    public async Task<bool> DeleteGiftAsync(int giftId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                "DELETE FROM Gift WHERE Id = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", giftId);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gift");
            return false;
        }
    }


    // Get all gifts for a registry
    public async Task<List<Gift>> GetGiftsForRegistryAsync(int registryId)
    {
        var result = new List<Gift>();
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                "SELECT * FROM Gift WHERE RegistryId = @RegistryId AND IsConfirmed = 0",
                connection);
            command.Parameters.AddWithValue("@RegistryId", registryId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new Gift
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? null : reader.GetDecimal(reader.GetOrdinal("Price")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
                    RegistryId = reader.GetInt32(reader.GetOrdinal("RegistryId")),
                    ReservedByEmail = reader.IsDBNull(reader.GetOrdinal("ReservedByEmail")) ? null : reader.GetString(reader.GetOrdinal("ReservedByEmail")),
                    ReservationDate = reader.IsDBNull(reader.GetOrdinal("ReservationDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReservationDate")),
                    IsConfirmed = reader.GetBoolean(reader.GetOrdinal("IsConfirmed")),
                    ConfirmationToken = reader.IsDBNull(reader.GetOrdinal("ConfirmationToken")) ? null : reader.GetString(reader.GetOrdinal("ConfirmationToken"))
                });
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gifts for registry {RegistryId}", registryId);
            return result;
        }
    }

    // Create a new registry
    public async Task<bool> CreateRegistryAsync(SubdomainGiftRegistry registry)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                @"INSERT INTO SubdomainGiftRegistry 
                (SubdomainName, Title, Description, OwnerId, CreatedDate, ExpiryDate, IsActive)
                VALUES 
                (@SubdomainName, @Title, @Description, @OwnerId, @CreatedDate, @ExpiryDate, @IsActive)",
                connection);

            command.Parameters.AddWithValue("@SubdomainName", registry.SubdomainName);
            command.Parameters.AddWithValue("@Title", registry.Title);
            command.Parameters.AddWithValue("@Description", registry.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OwnerId", registry.OwnerId);
            command.Parameters.AddWithValue("@CreatedDate", registry.CreatedDate);
            command.Parameters.AddWithValue("@ExpiryDate", registry.ExpiryDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", registry.IsActive);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift registry");
            return false;
        }
    }

    // Add a gift to a registry
    public async Task<(bool Success, int? Id)> AddGiftAsync(Gift gift)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(
                @"INSERT INTO Gift 
                (Name, Description, Price, ImageUrl, RegistryId)
                VALUES 
                (@Name, @Description, @Price, @ImageUrl, @RegistryId);
                SELECT SCOPE_IDENTITY();",
                connection);

            command.Parameters.AddWithValue("@Name", gift.Name);
            command.Parameters.AddWithValue("@Description", gift.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Price", gift.Price ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ImageUrl", gift.ImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RegistryId", gift.RegistryId);

            var result = await command.ExecuteScalarAsync();
            var id = result != null ? Convert.ToInt32(result) : (int?)null;

            return (true, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding gift");
            return (false, null);
        }
    }

    // Reserve a gift
    public async Task<(bool Success, string? ConfirmationToken)> ReserveGiftAsync(int giftId, string email, string name)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if the gift is already reserved
            var checkCommand = new SqlCommand(
                "SELECT * FROM Gift WHERE Id = @GiftId",
                connection);
            checkCommand.Parameters.AddWithValue("@GiftId", giftId);

            Gift? gift = null;
            using (var reader = await checkCommand.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    gift = new Gift
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        ReservedByEmail = reader.IsDBNull(reader.GetOrdinal("ReservedByEmail")) ? null : reader.GetString(reader.GetOrdinal("ReservedByEmail"))
                    };
                }
            }

            if (gift == null || !string.IsNullOrEmpty(gift.ReservedByEmail))
            {
                return (false, null);
            }

            // Generate confirmation token
            string token = WebEncoders.Base64UrlEncode(Guid.NewGuid().ToByteArray());

            // Update the gift with reservation info
            var updateCommand = new SqlCommand(
                @"UPDATE Gift 
            SET ReservedByEmail = @Email,
                ReservedByName = @Name,
                ReservationDate = @ReservationDate, 
                ConfirmationToken = @Token 
            WHERE Id = @GiftId",
                connection);

            updateCommand.Parameters.AddWithValue("@Email", email);
            updateCommand.Parameters.AddWithValue("@Name", name);
            updateCommand.Parameters.AddWithValue("@ReservationDate", DateTime.UtcNow);
            updateCommand.Parameters.AddWithValue("@Token", token);
            updateCommand.Parameters.AddWithValue("@GiftId", giftId);

            await updateCommand.ExecuteNonQueryAsync();
            return (true, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving gift");
            return (false, null);
        }
    }

    // Confirm a gift reservation
    public async Task<bool> ConfirmGiftReservationAsync(string token)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Find gift by token
            var checkCommand = new SqlCommand(
                "SELECT * FROM Gift WHERE ConfirmationToken = @Token",
                connection);
            checkCommand.Parameters.AddWithValue("@Token", token);

            bool giftExists = false;
            using (var reader = await checkCommand.ExecuteReaderAsync())
            {
                giftExists = await reader.ReadAsync();
            }

            if (!giftExists)
            {
                return false;
            }

            // Mark as confirmed
            var updateCommand = new SqlCommand(
                @"UPDATE Gift 
                SET IsConfirmed = 1
                WHERE ConfirmationToken = @Token",
                connection);

            updateCommand.Parameters.AddWithValue("@Token", token);
            await updateCommand.ExecuteNonQueryAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming gift reservation");
            return false;
        }
    }

    
    public async Task<string?> GetRegistryOwnerEmailAsync(int registryId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new SqlCommand(@"
            SELECT m.Email 
            FROM SubdomainGiftRegistry r
            JOIN cmsMember m ON r.OwnerId = m.nodeId
            WHERE r.Id = @RegistryId",
                connection);
            command.Parameters.AddWithValue("@RegistryId", registryId);

            return (string?)await command.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving registry owner email for registry {RegistryId}", registryId);
            return null;
        }
    }

}