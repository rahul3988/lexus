namespace Lexus2_0.Core.StateMachine
{
    /// <summary>
    /// Represents all possible states in the booking workflow
    /// </summary>
    public enum BookingState
    {
        Idle,
        Initializing,
        Authenticating,
        LoggingIn,
        Searching,
        WaitingForTatkal,
        SelectingTrain,
        FillingDetails,
        Payment,
        Completed,
        Failed,
        Paused,
        Stopped
    }

    /// <summary>
    /// Represents actions that can be triggered
    /// </summary>
    public enum BookingAction
    {
        Start,
        Stop,
        Pause,
        Resume,
        Retry,
        Next,
        Error
    }

    /// <summary>
    /// State transition result
    /// </summary>
    public class StateTransition
    {
        public BookingState FromState { get; set; }
        public BookingAction Action { get; set; }
        public BookingState ToState { get; set; }
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

