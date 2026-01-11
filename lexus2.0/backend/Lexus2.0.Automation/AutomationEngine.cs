using System;
using System.Threading;
using System.Threading.Tasks;
using Lexus2_0.Core.Models;
using Lexus2_0.Core.Logging;
using Lexus2_0.Automation.Workflows;
using Lexus2_0.Core.StateMachine;
using Lexus2_0.Automation.Captcha;

namespace Lexus2_0.Automation
{
    /// <summary>
    /// Main automation engine - orchestrates multithreading, task scheduling, and workflow execution
    /// </summary>
    public class AutomationEngine
    {
        private readonly ILogger _logger;
        private BookingWorkflow? _currentWorkflow;
        private readonly object _lockObject = new object();

        public bool IsRunning { get; private set; }
        public BookingState CurrentState => _currentWorkflow?.CurrentState ?? BookingState.Idle;

        public event EventHandler<string>? LogMessage;
        public event EventHandler<StateChangedEventArgs>? StateChanged;

        public AutomationEngine(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Start booking automation (thread-safe)
        /// </summary>
        public async Task StartAsync(BookingConfig config, CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                if (IsRunning)
                {
                    throw new InvalidOperationException("Automation engine is already running");
                }
                IsRunning = true;
            }

            try
            {
                _logger.Info("Starting automation engine...");
                LogMessage?.Invoke(this, "Automation engine started");

                // Convert string to enum for captcha solver type
                var captchaType = config.CaptchaSolverType switch
                {
                    "Tesseract" => CaptchaSolverType.Tesseract,
                    "Manual" => CaptchaSolverType.Manual,
                    _ => CaptchaSolverType.EasyOCR
                };
                
                _currentWorkflow = new BookingWorkflow(_logger, config.ProxyConfig, captchaType);
                _currentWorkflow.LogMessage += (sender, msg) => 
                {
                    _logger.Info(msg);
                    LogMessage?.Invoke(this, msg);
                };
                _currentWorkflow.StateChanged += (sender, e) => StateChanged?.Invoke(this, e);

                // Run workflow in background task
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _currentWorkflow.StartAsync(config, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Error in workflow execution", ex);
                        LogMessage?.Invoke(this, $"Error: {ex.Message}");
                    }
                    finally
                    {
                        lock (_lockObject)
                        {
                            IsRunning = false;
                        }
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    IsRunning = false;
                }
                _logger.Error("Failed to start automation engine", ex);
                throw;
            }
        }

        /// <summary>
        /// Pause automation
        /// </summary>
        public void Pause()
        {
            if (!IsRunning || _currentWorkflow == null)
                return;

            _currentWorkflow.Pause();
            _logger.Info("Automation paused");
            LogMessage?.Invoke(this, "Automation paused");
        }

        /// <summary>
        /// Resume automation
        /// </summary>
        public void Resume()
        {
            if (!IsRunning || _currentWorkflow == null)
                return;

            _currentWorkflow.Resume();
            _logger.Info("Automation resumed");
            LogMessage?.Invoke(this, "Automation resumed");
        }

        /// <summary>
        /// Stop automation
        /// </summary>
        public void Stop()
        {
            if (!IsRunning || _currentWorkflow == null)
                return;

            _currentWorkflow.Stop();
            
            lock (_lockObject)
            {
                IsRunning = false;
            }

            _logger.Info("Automation stopped");
            LogMessage?.Invoke(this, "Automation stopped");
        }
    }
}

