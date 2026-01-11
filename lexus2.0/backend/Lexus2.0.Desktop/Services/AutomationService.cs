using System;
using System.Threading;
using System.Threading.Tasks;
using Lexus2_0.Automation;
using Lexus2_0.Automation.Captcha;
using Lexus2_0.Core.Logging;
using Lexus2_0.Core.Models;
using Lexus2_0.Core.StateMachine;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Services
{
    /// <summary>
    /// Service that wraps AutomationEngine and integrates with ticket system
    /// Provides real-time status updates and error handling
    /// </summary>
    public class AutomationService : IDisposable
    {
        private readonly AutomationEngine _automationEngine;
        private readonly TicketService _ticketService;
        private readonly BypassManager _bypassManager;
        private readonly ILogger _logger;
        private CancellationTokenSource? _currentCancellationTokenSource;
        private Ticket? _currentTicket;
        private bool _disposed = false;

        public event EventHandler<string>? LogMessage;
        public event EventHandler<TicketStatusChangedEventArgs>? TicketStatusChanged;

        public AutomationService(
            AutomationEngine automationEngine,
            TicketService ticketService,
            BypassManager bypassManager,
            ILogger logger)
        {
            _automationEngine = automationEngine ?? throw new ArgumentNullException(nameof(automationEngine));
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
            _bypassManager = bypassManager ?? throw new ArgumentNullException(nameof(bypassManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _automationEngine.LogMessage += OnAutomationLogMessage;
            _automationEngine.StateChanged += OnAutomationStateChanged;
            _bypassManager.SettingsChanged += OnBypassSettingsChanged;
        }

        /// <summary>
        /// Start automation for a ticket
        /// </summary>
        public async Task StartTicketAsync(Ticket ticket, BookingConfig config)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AutomationService));

            if (ticket == null)
                throw new ArgumentNullException(nameof(ticket));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (_currentTicket != null)
                throw new InvalidOperationException("A ticket is already running. Stop the current ticket first.");

            try
            {
                _currentTicket = ticket;
                _currentCancellationTokenSource = new CancellationTokenSource();

                // Update ticket status
                ticket.Status = TicketStatus.Running;
                ticket.LastUpdatedTimestamp = DateTime.UtcNow;
                await _ticketService.UpdateTicketAsync(ticket);

                TicketStatusChanged?.Invoke(this, new TicketStatusChangedEventArgs(ticket));
                LogMessage?.Invoke(this, $"Started ticket {ticket.TicketId}");

                // Apply bypass settings to config
                ApplyBypassSettingsToConfig(config);

                // Start automation in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _automationEngine.StartAsync(config, _currentCancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Info($"Ticket {ticket.TicketId} was cancelled");
                        LogMessage?.Invoke(this, $"Ticket {ticket.TicketId} cancelled");
                        
                        if (_currentTicket != null)
                        {
                            _currentTicket.Status = TicketStatus.Cancelled;
                            _currentTicket.LastUpdatedTimestamp = DateTime.UtcNow;
                            await _ticketService.UpdateTicketAsync(_currentTicket);
                            TicketStatusChanged?.Invoke(this, new TicketStatusChangedEventArgs(_currentTicket));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error executing ticket {ticket.TicketId}", ex);
                        LogMessage?.Invoke(this, $"Error in ticket {ticket.TicketId}: {ex.Message}");
                        
                        if (_currentTicket != null)
                        {
                            _currentTicket.Status = TicketStatus.Failed;
                            _currentTicket.FailureCount++;
                            _currentTicket.ErrorMessage = ex.Message;
                            _currentTicket.LastUpdatedTimestamp = DateTime.UtcNow;
                            
                            await _ticketService.UpdateTicketAsync(_currentTicket);
                            TicketStatusChanged?.Invoke(this, new TicketStatusChangedEventArgs(_currentTicket));
                        }
                    }
                    finally
                    {
                        _currentTicket = null;
                    }
                }, _currentCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to start ticket {ticket.TicketId}", ex);
                LogMessage?.Invoke(this, $"Failed to start ticket {ticket.TicketId}: {ex.Message}");
                
                ticket.Status = TicketStatus.Failed;
                ticket.ErrorMessage = ex.Message;
                await _ticketService.UpdateTicketAsync(ticket);
                
                _currentTicket = null;
                throw;
            }
        }

        /// <summary>
        /// Stop the currently running ticket
        /// </summary>
        public async Task StopCurrentTicketAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AutomationService));

            if (_currentTicket == null)
                return;

            try
            {
                _logger.Info($"Stopping ticket {_currentTicket.TicketId}");
                LogMessage?.Invoke(this, $"Stopping ticket {_currentTicket.TicketId}");

                _automationEngine.Stop();
                _currentCancellationTokenSource?.Cancel();

                _currentTicket.Status = TicketStatus.Cancelled;
                _currentTicket.LastUpdatedTimestamp = DateTime.UtcNow;
                await _ticketService.UpdateTicketAsync(_currentTicket);

                TicketStatusChanged?.Invoke(this, new TicketStatusChangedEventArgs(_currentTicket));
                _currentTicket = null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error stopping ticket", ex);
                LogMessage?.Invoke(this, $"Error stopping ticket: {ex.Message}");
                throw;
            }
        }

        public bool IsRunning => _automationEngine?.IsRunning ?? false;

        private void ApplyBypassSettingsToConfig(BookingConfig config)
        {
            var settings = _bypassManager.GetSettings();
            
            config.HeadlessMode = settings.BrowserMode == BrowserMode.Headless;
            config.CaptchaSolverType = settings.CaptchaSolverType switch
            {
                CaptchaSolverType.EasyOCR => "EasyOCR",
                CaptchaSolverType.Tesseract => "Tesseract",
                CaptchaSolverType.Manual => "Manual",
                _ => "EasyOCR"
            };
        }

        private void OnAutomationLogMessage(object? sender, string message)
        {
            LogMessage?.Invoke(this, message);
        }

        private async void OnAutomationStateChanged(object? sender, StateChangedEventArgs e)
        {
            if (_currentTicket == null) return;

            try
            {
                // Update ticket based on state
                _currentTicket.AttemptCount++;
                _currentTicket.LastUpdatedTimestamp = DateTime.UtcNow;

                // Map automation state to ticket status
                switch (e.CurrentState)
                {
                    case BookingState.Completed:
                        _currentTicket.Status = TicketStatus.Completed;
                        _currentTicket.SuccessCount++;
                        LogMessage?.Invoke(this, $"Ticket {_currentTicket.TicketId} completed successfully");
                        break;
                    case BookingState.Failed:
                        _currentTicket.Status = TicketStatus.Failed;
                        _currentTicket.FailureCount++;
                        LogMessage?.Invoke(this, $"Ticket {_currentTicket.TicketId} failed");
                        break;
                    case BookingState.Idle:
                        if (_currentTicket.Status == TicketStatus.Running)
                        {
                            _currentTicket.Status = TicketStatus.Completed;
                            _currentTicket.SuccessCount++;
                        }
                        break;
                }

                await _ticketService.UpdateTicketAsync(_currentTicket);
                TicketStatusChanged?.Invoke(this, new TicketStatusChangedEventArgs(_currentTicket));
            }
            catch (Exception ex)
            {
                _logger.Error($"Error updating ticket status", ex);
            }
        }

        private void OnBypassSettingsChanged(object? sender, BypassSettingsChangedEventArgs e)
        {
            _logger.Info("Bypass settings updated");
            LogMessage?.Invoke(this, "Bypass settings updated");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _currentCancellationTokenSource?.Cancel();
                _currentCancellationTokenSource?.Dispose();
            }
        }
    }

    /// <summary>
    /// Event args for ticket status changes
    /// </summary>
    public class TicketStatusChangedEventArgs : EventArgs
    {
        public Ticket Ticket { get; }

        public TicketStatusChangedEventArgs(Ticket ticket)
        {
            Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));
        }
    }
}
