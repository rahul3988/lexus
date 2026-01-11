using System;
using System.IO;
using System.Text.Json;
using Lexus2_0.Core.Logging;
using Lexus2_0.Core.Models;

namespace Lexus2_0.Core.CrashRecovery
{
    /// <summary>
    /// Manages crash recovery and restart logic
    /// </summary>
    public class CrashRecoveryManager
    {
        private readonly string _recoveryFile;
        private readonly ILogger _logger;

        public CrashRecoveryManager(ILogger logger)
        {
            _logger = logger;
            var recoveryDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lexus2.0", "Recovery");
            if (!Directory.Exists(recoveryDir))
            {
                Directory.CreateDirectory(recoveryDir);
            }
            _recoveryFile = Path.Combine(recoveryDir, "last_state.json");
        }

        /// <summary>
        /// Save current state for recovery
        /// </summary>
        public void SaveState(RecoveryState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_recoveryFile, json);
                _logger.Debug("Recovery state saved");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save recovery state", ex);
            }
        }

        /// <summary>
        /// Load last saved state
        /// </summary>
        public RecoveryState? LoadState()
        {
            try
            {
                if (!File.Exists(_recoveryFile))
                    return null;

                var json = File.ReadAllText(_recoveryFile);
                return JsonSerializer.Deserialize<RecoveryState>(json);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load recovery state", ex);
                return null;
            }
        }

        /// <summary>
        /// Clear recovery state
        /// </summary>
        public void ClearState()
        {
            try
            {
                if (File.Exists(_recoveryFile))
                    File.Delete(_recoveryFile);
                _logger.Debug("Recovery state cleared");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to clear recovery state", ex);
            }
        }
    }

    public class RecoveryState
    {
        public BookingConfig? Config { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int AttemptCount { get; set; }
        public string? LastError { get; set; }
    }
}

