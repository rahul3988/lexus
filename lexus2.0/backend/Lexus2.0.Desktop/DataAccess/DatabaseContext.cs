using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Lexus2_0.Desktop.Models;
using IrctcAccount = Lexus2_0.Desktop.Models.IrctcAccount;
using PaymentOption = Lexus2_0.Desktop.Models.PaymentOption;
using ProxySetting = Lexus2_0.Desktop.Models.ProxySetting;

namespace Lexus2_0.Desktop.DataAccess
{
    /// <summary>
    /// SQLite database context for ticket storage
    /// </summary>
    public class DatabaseContext : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private bool _disposed = false;

        public DatabaseContext()
        {
            // Store database in application data directory
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Lexus2.0",
                "Database"
            );

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _databasePath = Path.Combine(appDataPath, "lexus_tickets.db");
            _connectionString = $"Data Source={_databasePath}";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Tickets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TicketId TEXT NOT NULL UNIQUE,
                    Status TEXT NOT NULL,
                    AttemptCount INTEGER NOT NULL DEFAULT 0,
                    SuccessCount INTEGER NOT NULL DEFAULT 0,
                    FailureCount INTEGER NOT NULL DEFAULT 0,
                    CaptchaFailureCount INTEGER NOT NULL DEFAULT 0,
                    CreatedTimestamp TEXT NOT NULL,
                    LastUpdatedTimestamp TEXT NOT NULL,
                    ErrorMessage TEXT,
                    ConfigurationJson TEXT
                );

                CREATE TABLE IF NOT EXISTS IrctcAccounts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IrctcId TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    MobileNumber TEXT,
                    Status TEXT NOT NULL DEFAULT 'Active',
                    CreatedDate TEXT NOT NULL,
                    LastUsedDate TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS PaymentOptions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Gateway TEXT NOT NULL,
                    BankName TEXT,
                    CardNumber TEXT,
                    Status TEXT NOT NULL DEFAULT 'Active',
                    IsPriorBank INTEGER NOT NULL DEFAULT 0,
                    IsBackupBank INTEGER NOT NULL DEFAULT 0,
                    CreatedDate TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ProxySettings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IpAddress TEXT NOT NULL,
                    Port TEXT NOT NULL,
                    Username TEXT,
                    Password TEXT,
                    Type TEXT NOT NULL DEFAULT 'HTTP',
                    Status TEXT NOT NULL DEFAULT 'Active',
                    CreatedDate TEXT NOT NULL,
                    LastTestedDate TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_Tickets_TicketId ON Tickets(TicketId);
                CREATE INDEX IF NOT EXISTS IX_Tickets_Status ON Tickets(Status);
                CREATE INDEX IF NOT EXISTS IX_Tickets_LastUpdatedTimestamp ON Tickets(LastUpdatedTimestamp);
                CREATE INDEX IF NOT EXISTS IX_IrctcAccounts_IrctcId ON IrctcAccounts(IrctcId);
                CREATE INDEX IF NOT EXISTS IX_PaymentOptions_Name ON PaymentOptions(Name);
                CREATE INDEX IF NOT EXISTS IX_ProxySettings_IpAddress ON ProxySettings(IpAddress);
            ";

            command.ExecuteNonQuery();
            
            // Migrate existing database to add new columns
            MigrateDatabase();
        }

        private void MigrateDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                // Check if Tickets table exists first
                var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Tickets'";
                var tableExists = checkTableCommand.ExecuteScalar() != null;
                
                if (!tableExists)
                {
                    return; // Table doesn't exist yet, will be created by InitializeDatabase
                }

                var columnsToAdd = new Dictionary<string, string>
                {
                    { "Name", "TEXT DEFAULT ''" },
                    { "\"From\"", "TEXT DEFAULT ''" },
                    { "\"To\"", "TEXT DEFAULT ''" },
                    { "Date", "TEXT DEFAULT ''" },
                    { "QT", "TEXT DEFAULT ''" },
                    { "GN", "TEXT DEFAULT ''" },
                    { "CLS", "TEXT DEFAULT ''" },
                    { "SL", "TEXT DEFAULT ''" },
                    { "SLOT", "TEXT DEFAULT ''" },
                    { "Pair", "TEXT DEFAULT ''" },
                    { "TrainNo", "TEXT DEFAULT ''" },
                    { "Username", "TEXT DEFAULT ''" },
                    { "PaymentGateway", "TEXT DEFAULT ''" },
                    { "UpiId", "TEXT DEFAULT ''" },
                    { "EnableOtpReader", "INTEGER DEFAULT 0" },
                    { "TotalTicket", "INTEGER DEFAULT 0" },
                    { "WebCount", "INTEGER DEFAULT 0" },
                    { "AppCount", "INTEGER DEFAULT 0" }
                };

                foreach (var column in columnsToAdd)
                {
                    try
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $"ALTER TABLE Tickets ADD COLUMN {column.Key} {column.Value}";
                        command.ExecuteNonQuery();
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException)
                    {
                        // Column already exists or other SQLite error, skip
                    }
                    catch (Exception)
                    {
                        // Any other error, skip this column
                    }
                }
            }
            catch (Exception)
            {
                // Migration failed, but don't crash the application
                // The app can still work with existing columns
            }
        }

        public async Task<Ticket> CreateTicketAsync(Ticket ticket)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Tickets (
                    TicketId, Status, AttemptCount, SuccessCount, FailureCount, 
                    CaptchaFailureCount, CreatedTimestamp, LastUpdatedTimestamp, 
                    ErrorMessage,                     ConfigurationJson, Name, ""From"", ""To"", Date, QT, GN, CLS, SL, SLOT,
                    Pair, TrainNo, Username, PaymentGateway, UpiId, EnableOtpReader,
                    TotalTicket, WebCount, AppCount
                )
                VALUES (
                    @TicketId, @Status, @AttemptCount, @SuccessCount, @FailureCount,
                    @CaptchaFailureCount, @CreatedTimestamp, @LastUpdatedTimestamp,
                    @ErrorMessage, @ConfigurationJson, @Name, @From, @To, @Date, @QT, @GN, @CLS, @SL, @SLOT,
                    @Pair, @TrainNo, @Username, @PaymentGateway, @UpiId, @EnableOtpReader,
                    @TotalTicket, @WebCount, @AppCount
                );
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@TicketId", ticket.TicketId);
            command.Parameters.AddWithValue("@Status", ticket.Status.ToString());
            command.Parameters.AddWithValue("@AttemptCount", ticket.AttemptCount);
            command.Parameters.AddWithValue("@SuccessCount", ticket.SuccessCount);
            command.Parameters.AddWithValue("@FailureCount", ticket.FailureCount);
            command.Parameters.AddWithValue("@CaptchaFailureCount", ticket.CaptchaFailureCount);
            command.Parameters.AddWithValue("@CreatedTimestamp", ticket.CreatedTimestamp.ToString("O"));
            command.Parameters.AddWithValue("@LastUpdatedTimestamp", ticket.LastUpdatedTimestamp.ToString("O"));
            command.Parameters.AddWithValue("@ErrorMessage", (object?)ticket.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConfigurationJson", (object?)ticket.ConfigurationJson ?? DBNull.Value);
            command.Parameters.AddWithValue("@Name", ticket.Name ?? "");
            command.Parameters.AddWithValue("@From", ticket.From ?? "");
            command.Parameters.AddWithValue("@To", ticket.To ?? "");
            command.Parameters.AddWithValue("@Date", ticket.Date ?? "");
            command.Parameters.AddWithValue("@QT", ticket.QT ?? "");
            command.Parameters.AddWithValue("@GN", ticket.GN ?? "");
            command.Parameters.AddWithValue("@CLS", ticket.CLS ?? "");
            command.Parameters.AddWithValue("@SL", ticket.SL ?? "");
            command.Parameters.AddWithValue("@SLOT", ticket.SLOT ?? "");
            command.Parameters.AddWithValue("@Pair", ticket.Pair ?? "");
            command.Parameters.AddWithValue("@TrainNo", ticket.TrainNo ?? "");
            command.Parameters.AddWithValue("@Username", ticket.Username ?? "");
            command.Parameters.AddWithValue("@PaymentGateway", ticket.PaymentGateway ?? "");
            command.Parameters.AddWithValue("@UpiId", ticket.UpiId ?? "");
            command.Parameters.AddWithValue("@EnableOtpReader", ticket.EnableOtpReader ? 1 : 0);
            command.Parameters.AddWithValue("@TotalTicket", ticket.TotalTicket);
            command.Parameters.AddWithValue("@WebCount", ticket.WebCount);
            command.Parameters.AddWithValue("@AppCount", ticket.AppCount);

            var result = await command.ExecuteScalarAsync();
            if (result != null)
            {
                ticket.Id = (int)(long)result;
            }
            return ticket;
        }

        public async Task<List<Ticket>> GetAllTicketsAsync()
        {
            var tickets = new List<Ticket>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, TicketId, Status, AttemptCount, SuccessCount, FailureCount,
                       CaptchaFailureCount, CreatedTimestamp, LastUpdatedTimestamp,
                       ErrorMessage, ConfigurationJson, Name, ""From"", ""To"", Date, QT, GN, CLS, SL, SLOT,
                       Pair, TrainNo, Username, PaymentGateway, UpiId, EnableOtpReader,
                       TotalTicket, WebCount, AppCount
                FROM Tickets
                ORDER BY LastUpdatedTimestamp DESC
            ";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tickets.Add(MapReaderToTicket(reader));
            }

            return tickets;
        }

        public async Task<Ticket?> GetTicketByIdAsync(string ticketId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, TicketId, Status, AttemptCount, SuccessCount, FailureCount,
                       CaptchaFailureCount, CreatedTimestamp, LastUpdatedTimestamp,
                       ErrorMessage, ConfigurationJson, Name, ""From"", ""To"", Date, QT, GN, CLS, SL, SLOT,
                       Pair, TrainNo, Username, PaymentGateway, UpiId, EnableOtpReader,
                       TotalTicket, WebCount, AppCount
                FROM Tickets
                WHERE TicketId = @TicketId
            ";

            command.Parameters.AddWithValue("@TicketId", ticketId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToTicket(reader);
            }

            return null;
        }

        public async Task UpdateTicketAsync(Ticket ticket)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Tickets
                SET Status = @Status,
                    AttemptCount = @AttemptCount,
                    SuccessCount = @SuccessCount,
                    FailureCount = @FailureCount,
                    CaptchaFailureCount = @CaptchaFailureCount,
                    LastUpdatedTimestamp = @LastUpdatedTimestamp,
                    ErrorMessage = @ErrorMessage,
                    Name = @Name, ""From"" = @From, ""To"" = @To, Date = @Date, QT = @QT, GN = @GN, CLS = @CLS, SL = @SL, SLOT = @SLOT,
                    Pair = @Pair, TrainNo = @TrainNo, Username = @Username, PaymentGateway = @PaymentGateway, UpiId = @UpiId,
                    EnableOtpReader = @EnableOtpReader, TotalTicket = @TotalTicket, WebCount = @WebCount, AppCount = @AppCount
                WHERE TicketId = @TicketId
            ";

            command.Parameters.AddWithValue("@Status", ticket.Status.ToString());
            command.Parameters.AddWithValue("@AttemptCount", ticket.AttemptCount);
            command.Parameters.AddWithValue("@SuccessCount", ticket.SuccessCount);
            command.Parameters.AddWithValue("@FailureCount", ticket.FailureCount);
            command.Parameters.AddWithValue("@CaptchaFailureCount", ticket.CaptchaFailureCount);
            command.Parameters.AddWithValue("@LastUpdatedTimestamp", ticket.LastUpdatedTimestamp.ToString("O"));
            command.Parameters.AddWithValue("@ErrorMessage", (object?)ticket.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@Name", ticket.Name ?? "");
            command.Parameters.AddWithValue("@From", ticket.From ?? "");
            command.Parameters.AddWithValue("@To", ticket.To ?? "");
            command.Parameters.AddWithValue("@Date", ticket.Date ?? "");
            command.Parameters.AddWithValue("@QT", ticket.QT ?? "");
            command.Parameters.AddWithValue("@GN", ticket.GN ?? "");
            command.Parameters.AddWithValue("@CLS", ticket.CLS ?? "");
            command.Parameters.AddWithValue("@SL", ticket.SL ?? "");
            command.Parameters.AddWithValue("@SLOT", ticket.SLOT ?? "");
            command.Parameters.AddWithValue("@Pair", ticket.Pair ?? "");
            command.Parameters.AddWithValue("@TrainNo", ticket.TrainNo ?? "");
            command.Parameters.AddWithValue("@Username", ticket.Username ?? "");
            command.Parameters.AddWithValue("@PaymentGateway", ticket.PaymentGateway ?? "");
            command.Parameters.AddWithValue("@UpiId", ticket.UpiId ?? "");
            command.Parameters.AddWithValue("@EnableOtpReader", ticket.EnableOtpReader ? 1 : 0);
            command.Parameters.AddWithValue("@TotalTicket", ticket.TotalTicket);
            command.Parameters.AddWithValue("@WebCount", ticket.WebCount);
            command.Parameters.AddWithValue("@AppCount", ticket.AppCount);
            command.Parameters.AddWithValue("@TicketId", ticket.TicketId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteTicketAsync(string ticketId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Tickets WHERE TicketId = @TicketId";
            command.Parameters.AddWithValue("@TicketId", ticketId);

            await command.ExecuteNonQueryAsync();
        }

        private string GetColumnValue(SqliteDataReader reader, string columnName, string defaultValue)
        {
            try
            {
                if (reader.IsDBNull(columnName))
                    return defaultValue;
                return reader.GetString(columnName);
            }
            catch
            {
                return defaultValue;
            }
        }

        private Ticket MapReaderToTicket(SqliteDataReader reader)
        {
            var ticket = new Ticket
            {
                Id = reader.GetInt32("Id"),
                TicketId = reader.GetString("TicketId"),
                Status = Enum.Parse<TicketStatus>(reader.GetString("Status")),
                AttemptCount = reader.GetInt32("AttemptCount"),
                SuccessCount = reader.GetInt32("SuccessCount"),
                FailureCount = reader.GetInt32("FailureCount"),
                CaptchaFailureCount = reader.GetInt32("CaptchaFailureCount"),
                CreatedTimestamp = DateTime.Parse(reader.GetString("CreatedTimestamp")),
                LastUpdatedTimestamp = DateTime.Parse(reader.GetString("LastUpdatedTimestamp")),
                ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage"),
                ConfigurationJson = reader.IsDBNull("ConfigurationJson") ? null : reader.GetString("ConfigurationJson")
            };

            // Map new fields if they exist - use try-catch for each field
            try { ticket.Name = GetColumnValue(reader, "Name", ""); } catch { }
            try { ticket.From = GetColumnValue(reader, "\"From\"", GetColumnValue(reader, "From", "")); } catch { }
            try { ticket.To = GetColumnValue(reader, "\"To\"", GetColumnValue(reader, "To", "")); } catch { }
            try { ticket.Date = GetColumnValue(reader, "Date", ""); } catch { }
            try { ticket.QT = GetColumnValue(reader, "QT", ""); } catch { }
            try { ticket.GN = GetColumnValue(reader, "GN", ""); } catch { }
            try { ticket.CLS = GetColumnValue(reader, "CLS", ""); } catch { }
            try { ticket.SL = GetColumnValue(reader, "SL", ""); } catch { }
            try { ticket.SLOT = GetColumnValue(reader, "SLOT", ""); } catch { }
            try { ticket.Pair = GetColumnValue(reader, "Pair", ""); } catch { }
            try { ticket.TrainNo = GetColumnValue(reader, "TrainNo", ""); } catch { }
            try { ticket.Username = GetColumnValue(reader, "Username", ""); } catch { }
            try { ticket.PaymentGateway = GetColumnValue(reader, "PaymentGateway", ""); } catch { }
            try { ticket.UpiId = GetColumnValue(reader, "UpiId", ""); } catch { }
            try { ticket.EnableOtpReader = !reader.IsDBNull("EnableOtpReader") && reader.GetInt32("EnableOtpReader") == 1; } catch { }
            try { ticket.TotalTicket = reader.IsDBNull("TotalTicket") ? 0 : reader.GetInt32("TotalTicket"); } catch { }
            try { ticket.WebCount = reader.IsDBNull("WebCount") ? 0 : reader.GetInt32("WebCount"); } catch { }
            try { ticket.AppCount = reader.IsDBNull("AppCount") ? 0 : reader.GetInt32("AppCount"); } catch { }

            return ticket;
        }

        // IRCTC Accounts Methods
        public async Task<IrctcAccount> CreateIrctcAccountAsync(IrctcAccount account)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO IrctcAccounts (IrctcId, Password, MobileNumber, Status, CreatedDate, LastUsedDate)
                VALUES (@IrctcId, @Password, @MobileNumber, @Status, @CreatedDate, @LastUsedDate);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@IrctcId", account.IrctcId);
            command.Parameters.AddWithValue("@Password", account.Password);
            command.Parameters.AddWithValue("@MobileNumber", account.MobileNumber ?? "");
            command.Parameters.AddWithValue("@Status", account.Status);
            command.Parameters.AddWithValue("@CreatedDate", account.CreatedDate.ToString("O"));
            command.Parameters.AddWithValue("@LastUsedDate", account.LastUsedDate.ToString("O"));

            var result = await command.ExecuteScalarAsync();
            if (result != null)
            {
                account.Id = (int)(long)result;
            }
            return account;
        }

        public async Task<List<IrctcAccount>> GetAllIrctcAccountsAsync()
        {
            var accounts = new List<IrctcAccount>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, IrctcId, Password, MobileNumber, Status, CreatedDate, LastUsedDate FROM IrctcAccounts ORDER BY CreatedDate DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(new IrctcAccount
                {
                    Id = reader.GetInt32("Id"),
                    IrctcId = reader.GetString("IrctcId"),
                    Password = reader.GetString("Password"),
                    MobileNumber = reader.GetString("MobileNumber"),
                    Status = reader.GetString("Status"),
                    CreatedDate = DateTime.Parse(reader.GetString("CreatedDate")),
                    LastUsedDate = DateTime.Parse(reader.GetString("LastUsedDate"))
                });
            }

            return accounts;
        }

        public async Task DeleteIrctcAccountAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM IrctcAccounts WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }

        // Payment Options Methods
        public async Task<PaymentOption> CreatePaymentOptionAsync(PaymentOption option)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO PaymentOptions (Name, Type, Gateway, BankName, CardNumber, Status, IsPriorBank, IsBackupBank, CreatedDate)
                VALUES (@Name, @Type, @Gateway, @BankName, @CardNumber, @Status, @IsPriorBank, @IsBackupBank, @CreatedDate);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@Name", option.Name);
            command.Parameters.AddWithValue("@Type", option.Type);
            command.Parameters.AddWithValue("@Gateway", option.Gateway);
            command.Parameters.AddWithValue("@BankName", option.BankName ?? "");
            command.Parameters.AddWithValue("@CardNumber", option.CardNumber ?? "");
            command.Parameters.AddWithValue("@Status", option.Status);
            command.Parameters.AddWithValue("@IsPriorBank", option.IsPriorBank ? 1 : 0);
            command.Parameters.AddWithValue("@IsBackupBank", option.IsBackupBank ? 1 : 0);
            command.Parameters.AddWithValue("@CreatedDate", option.CreatedDate.ToString("O"));

            var result = await command.ExecuteScalarAsync();
            if (result != null)
            {
                option.Id = (int)(long)result;
            }
            return option;
        }

        public async Task<List<PaymentOption>> GetAllPaymentOptionsAsync()
        {
            var options = new List<PaymentOption>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Type, Gateway, BankName, CardNumber, Status, IsPriorBank, IsBackupBank, CreatedDate FROM PaymentOptions ORDER BY CreatedDate DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                options.Add(new PaymentOption
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Type = reader.GetString("Type"),
                    Gateway = reader.GetString("Gateway"),
                    BankName = reader.GetString("BankName"),
                    CardNumber = reader.GetString("CardNumber"),
                    Status = reader.GetString("Status"),
                    IsPriorBank = reader.GetInt32("IsPriorBank") == 1,
                    IsBackupBank = reader.GetInt32("IsBackupBank") == 1,
                    CreatedDate = DateTime.Parse(reader.GetString("CreatedDate"))
                });
            }

            return options;
        }

        public async Task DeletePaymentOptionAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM PaymentOptions WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }

        // Proxy Settings Methods
        public async Task<ProxySetting> CreateProxySettingAsync(ProxySetting setting)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ProxySettings (IpAddress, Port, Username, Password, Type, Status, CreatedDate, LastTestedDate)
                VALUES (@IpAddress, @Port, @Username, @Password, @Type, @Status, @CreatedDate, @LastTestedDate);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@IpAddress", setting.IpAddress);
            command.Parameters.AddWithValue("@Port", setting.Port);
            command.Parameters.AddWithValue("@Username", setting.Username ?? "");
            command.Parameters.AddWithValue("@Password", setting.Password ?? "");
            command.Parameters.AddWithValue("@Type", setting.Type);
            command.Parameters.AddWithValue("@Status", setting.Status);
            command.Parameters.AddWithValue("@CreatedDate", setting.CreatedDate.ToString("O"));
            command.Parameters.AddWithValue("@LastTestedDate", setting.LastTestedDate.ToString("O"));

            var result = await command.ExecuteScalarAsync();
            if (result != null)
            {
                setting.Id = (int)(long)result;
            }
            return setting;
        }

        public async Task<List<ProxySetting>> GetAllProxySettingsAsync()
        {
            var settings = new List<ProxySetting>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, IpAddress, Port, Username, Password, Type, Status, CreatedDate, LastTestedDate FROM ProxySettings ORDER BY CreatedDate DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                settings.Add(new ProxySetting
                {
                    Id = reader.GetInt32("Id"),
                    IpAddress = reader.GetString("IpAddress"),
                    Port = reader.GetString("Port"),
                    Username = reader.IsDBNull("Username") ? null : reader.GetString("Username"),
                    Password = reader.IsDBNull("Password") ? null : reader.GetString("Password"),
                    Type = reader.GetString("Type"),
                    Status = reader.GetString("Status"),
                    CreatedDate = DateTime.Parse(reader.GetString("CreatedDate")),
                    LastTestedDate = DateTime.Parse(reader.GetString("LastTestedDate"))
                });
            }

            return settings;
        }

        public async Task UpdateProxySettingAsync(ProxySetting setting)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ProxySettings
                SET IpAddress = @IpAddress, Port = @Port, Username = @Username, Password = @Password,
                    Type = @Type, Status = @Status, LastTestedDate = @LastTestedDate
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@IpAddress", setting.IpAddress);
            command.Parameters.AddWithValue("@Port", setting.Port);
            command.Parameters.AddWithValue("@Username", setting.Username ?? "");
            command.Parameters.AddWithValue("@Password", setting.Password ?? "");
            command.Parameters.AddWithValue("@Type", setting.Type);
            command.Parameters.AddWithValue("@Status", setting.Status);
            command.Parameters.AddWithValue("@LastTestedDate", setting.LastTestedDate.ToString("O"));
            command.Parameters.AddWithValue("@Id", setting.Id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteProxySettingAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ProxySettings WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}

