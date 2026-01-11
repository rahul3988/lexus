using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexus2_0.Core.StateMachine
{
    /// <summary>
    /// Deterministic state machine engine for booking workflow
    /// Zero runtime decision - all transitions are predefined
    /// </summary>
    public class StateMachineEngine
    {
        private readonly Dictionary<(BookingState, BookingAction), BookingState> _transitions;
        private BookingState _currentState;

        public event EventHandler<StateChangedEventArgs>? StateChanged;

        public BookingState CurrentState => _currentState;

        public StateMachineEngine()
        {
            _currentState = BookingState.Idle;
            _transitions = InitializeTransitions();
        }

        /// <summary>
        /// Initialize all valid state transitions (deterministic mapping)
        /// </summary>
        private Dictionary<(BookingState, BookingAction), BookingState> InitializeTransitions()
        {
            return new Dictionary<(BookingState, BookingAction), BookingState>
            {
                // Start flow
                { (BookingState.Idle, BookingAction.Start), BookingState.Initializing },
                { (BookingState.Initializing, BookingAction.Next), BookingState.Authenticating },
                { (BookingState.Authenticating, BookingAction.Next), BookingState.LoggingIn },
                { (BookingState.LoggingIn, BookingAction.Next), BookingState.Searching },
                
                // Search flow - always go to WaitingForTatkal first (will be skipped if not Tatkal)
                { (BookingState.Searching, BookingAction.Next), BookingState.WaitingForTatkal },
                { (BookingState.WaitingForTatkal, BookingAction.Next), BookingState.SelectingTrain },
                
                // Common flow
                { (BookingState.SelectingTrain, BookingAction.Next), BookingState.FillingDetails },
                { (BookingState.FillingDetails, BookingAction.Next), BookingState.Payment },
                { (BookingState.Payment, BookingAction.Next), BookingState.Completed },
                
                // Error handling
                { (BookingState.Initializing, BookingAction.Error), BookingState.Failed },
                { (BookingState.Authenticating, BookingAction.Error), BookingState.Failed },
                { (BookingState.LoggingIn, BookingAction.Error), BookingState.Failed },
                { (BookingState.Searching, BookingAction.Error), BookingState.Failed },
                { (BookingState.WaitingForTatkal, BookingAction.Error), BookingState.Failed },
                { (BookingState.SelectingTrain, BookingAction.Error), BookingState.Failed },
                { (BookingState.FillingDetails, BookingAction.Error), BookingState.Failed },
                { (BookingState.Payment, BookingAction.Error), BookingState.Failed },
                
                // Retry mechanism
                { (BookingState.Failed, BookingAction.Retry), BookingState.Initializing },
                
                // Control actions
                { (BookingState.Initializing, BookingAction.Pause), BookingState.Paused },
                { (BookingState.Authenticating, BookingAction.Pause), BookingState.Paused },
                { (BookingState.LoggingIn, BookingAction.Pause), BookingState.Paused },
                { (BookingState.Searching, BookingAction.Pause), BookingState.Paused },
                { (BookingState.WaitingForTatkal, BookingAction.Pause), BookingState.Paused },
                { (BookingState.SelectingTrain, BookingAction.Pause), BookingState.Paused },
                { (BookingState.FillingDetails, BookingAction.Pause), BookingState.Paused },
                { (BookingState.Payment, BookingAction.Pause), BookingState.Paused },
                
                { (BookingState.Paused, BookingAction.Resume), BookingState.Searching }, // Resume to last active state
                
                { (BookingState.Initializing, BookingAction.Stop), BookingState.Stopped },
                { (BookingState.Authenticating, BookingAction.Stop), BookingState.Stopped },
                { (BookingState.LoggingIn, BookingAction.Stop), BookingState.Stopped },
                { (BookingState.Searching, BookingAction.Stop), BookingState.Stopped },
                { (BookingState.WaitingForTatkal, BookingAction.Stop), BookingState.Stopped },
                { (BookingState.SelectingTrain, BookingAction.Stop), BookingState.Stopped },
                { (BookingState.FillingDetails, BookingAction.Stop), BookingState.Stopped },
                { (BookingState.Payment, BookingAction.Stop), BookingState.Stopped },
                { (BookingState.Paused, BookingAction.Stop), BookingState.Stopped },
                
                // Reset
                { (BookingState.Completed, BookingAction.Start), BookingState.Initializing },
                { (BookingState.Failed, BookingAction.Start), BookingState.Initializing },
                { (BookingState.Stopped, BookingAction.Start), BookingState.Initializing }
            };
        }

        /// <summary>
        /// Execute a state transition (deterministic - no runtime decisions)
        /// </summary>
        public StateTransition ExecuteAction(BookingAction action)
        {
            var key = (_currentState, action);
            
            if (!_transitions.ContainsKey(key))
            {
                return new StateTransition
                {
                    FromState = _currentState,
                    Action = action,
                    ToState = _currentState,
                    IsValid = false,
                    ErrorMessage = $"Invalid transition from {_currentState} with action {action}"
                };
            }

            var previousState = _currentState;
            _currentState = _transitions[key];

            var transition = new StateTransition
            {
                FromState = previousState,
                Action = action,
                ToState = _currentState,
                IsValid = true
            };

            StateChanged?.Invoke(this, new StateChangedEventArgs
            {
                PreviousState = previousState,
                CurrentState = _currentState,
                Action = action
            });

            return transition;
        }

        /// <summary>
        /// Get all valid actions for current state
        /// </summary>
        public List<BookingAction> GetValidActions()
        {
            return _transitions
                .Where(t => t.Key.Item1 == _currentState)
                .Select(t => t.Key.Item2)
                .ToList();
        }

        /// <summary>
        /// Reset state machine to idle
        /// </summary>
        public void Reset()
        {
            _currentState = BookingState.Idle;
        }
    }

    public class StateChangedEventArgs : EventArgs
    {
        public BookingState PreviousState { get; set; }
        public BookingState CurrentState { get; set; }
        public BookingAction Action { get; set; }
    }
}

