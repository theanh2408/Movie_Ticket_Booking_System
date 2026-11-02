using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MovieTicketBooking.Api.Data;
using MovieTicketBooking.Api.DTOs;

namespace MovieTicketBooking.Api.Services;

public class AuthService
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;

    public AuthService(
        DbConnectionFactory connectionFactory,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message, object? User)>
        RegisterAsync(RegisterRequest request)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        const string checkSql = """
            SELECT COUNT(1)
            FROM dbo.Account
            WHERE Email = @Email;
            """;

        await using (var checkCommand =
            new SqlCommand(checkSql, connection))
        {
            checkCommand.Parameters.AddWithValue(
                "@Email", request.Email.Trim());

            var result = await checkCommand.ExecuteScalarAsync();
            var count = Convert.ToInt32(result);

            if (count > 0)
            {
                return (false, "Email already exists", null);
            }
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(
            request.Password);

        const string insertSql = """
            INSERT INTO dbo.Account
                (FullName, Phone, Email, DOB, Gender, PasswordHash, Role)
            OUTPUT
                INSERTED.UserID,
                INSERTED.FullName,
                INSERTED.Phone,
                INSERTED.Email,
                INSERTED.DOB,
                INSERTED.Gender,
                INSERTED.Role
            VALUES
                (@FullName, @Phone, @Email, @DOB, @Gender,
                 @PasswordHash, @Role);
            """;

        await using var insertCommand =
            new SqlCommand(insertSql, connection);

        insertCommand.Parameters.AddWithValue(
            "@FullName", request.FullName.Trim());

        insertCommand.Parameters.AddWithValue(
            "@Phone", request.Phone.Trim());

        insertCommand.Parameters.AddWithValue(
            "@Email", request.Email.Trim());

        insertCommand.Parameters.AddWithValue(
            "@DOB", (object?)request.Dob?.Date ?? DBNull.Value);

        insertCommand.Parameters.AddWithValue(
            "@Gender", (object?)request.Gender ?? DBNull.Value);

        insertCommand.Parameters.AddWithValue(
            "@PasswordHash", passwordHash);

        insertCommand.Parameters.AddWithValue(
            "@Role", "CUSTOMER");

        await using var reader =
            await insertCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "Could not retrieve the registered account.");
        }

        var user = new
        {
            userId = Convert.ToInt32(reader["UserID"]),
            fullName = reader["FullName"] as string,
            phone = reader["Phone"] as string,
            email = reader["Email"] as string,
            dob = reader["DOB"] == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(reader["DOB"]),
            gender = reader["Gender"] == DBNull.Value
                ? null
                : reader["Gender"] as string,
            role = reader["Role"] as string
        };

        return (true, "Registration successful", user);
    }

    public async Task<(
        string Token,
        object User,
        int ExpiresIn
    )?> LoginAsync(LoginRequest request)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        const string sql = """
            SELECT UserID, FullName, Email, PasswordHash, Role
            FROM dbo.Account
            WHERE Email = @Email;
            """;

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Email", request.Email.Trim());

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var passwordHash = reader["PasswordHash"] as string;

        if (string.IsNullOrEmpty(passwordHash) ||
            !BCrypt.Net.BCrypt.Verify(
                request.Password, passwordHash))
        {
            return null;
        }

        var userId = Convert.ToInt32(reader["UserID"]);
        var fullName = reader["FullName"] as string;
        var email = reader["Email"] as string ?? "";
        var role = reader["Role"] as string ?? "CUSTOMER";

        var key = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "JWT key is missing. Set Jwt:Key via user-secrets or the Jwt__Key environment variable.");
        }

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var expiresMinutes = int.Parse(
            _configuration["Jwt:ExpiresMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                userId.ToString()),
            new Claim(ClaimTypes.Name, fullName ?? ""),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        var user = new
        {
            userId,
            fullName,
            email,
            role
        };

        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            user,
            expiresMinutes * 60
        );
    }

    public async Task<bool> UpdateProfileAsync(
    int userId,
    UpdateProfileRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = """
        UPDATE dbo.Account
        SET FullName = @FullName,
            Phone = @Phone,
            DOB = @DOB,
            Gender = @Gender
        WHERE UserID = @UserID;
        """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 100)
            .Value = request.FullName.Trim();

        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20)
            .Value = request.Phone.Trim();

        command.Parameters.Add("@DOB", SqlDbType.Date)
            .Value = request.Dob.HasValue
                ? request.Dob.Value.Date
                : DBNull.Value;

        command.Parameters.Add("@Gender", SqlDbType.NVarChar, 20)
            .Value = string.IsNullOrWhiteSpace(request.Gender)
                ? DBNull.Value
                : request.Gender.Trim();

        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

        int rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }


    public async Task<bool?> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        const string sql = """
        SELECT PasswordHash
        FROM dbo.Account
        WHERE UserID = @UserID;
        """;

        string? passwordHash;

        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.Add("@UserID", SqlDbType.Int)
                .Value = userId;

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            passwordHash = result as string;
        }

        if (string.IsNullOrWhiteSpace(passwordHash) ||
            !BCrypt.Net.BCrypt.Verify(currentPassword, passwordHash))
        {
            return false;
        }

        var newPasswordHash =
            BCrypt.Net.BCrypt.HashPassword(newPassword);

        const string updateSql = """
        UPDATE dbo.Account
        SET PasswordHash = @PasswordHash
        WHERE UserID = @UserID;
        """;

        await using var updateCommand =
            new SqlCommand(updateSql, connection);

        updateCommand.Parameters.Add(
            "@PasswordHash", SqlDbType.NVarChar, 255)
            .Value = newPasswordHash;

        updateCommand.Parameters.Add("@UserID", SqlDbType.Int)
            .Value = userId;

        return await updateCommand.ExecuteNonQueryAsync() > 0;
    }


    public async Task<object?> GetProfileAsync(int userId)
    {
        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        const string sql = """
        SELECT UserID, FullName, Phone, Email, DOB, Gender, Role
        FROM dbo.Account
        WHERE UserID = @UserID;
        """;

        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new
        {
            userId = Convert.ToInt32(reader["UserID"]),
            fullName = reader["FullName"] as string,
            phone = reader["Phone"] == DBNull.Value
                ? null
                : reader["Phone"] as string,
            email = reader["Email"] as string,
            dob = reader["DOB"] == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(reader["DOB"]),
            gender = reader["Gender"] == DBNull.Value
                ? null
                : reader["Gender"] as string,
            role = reader["Role"] as string
        };
    }
}