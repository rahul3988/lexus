using System.Collections.Generic;
using System.Text.Json.Serialization;
using Lexus2_0.Core.Proxy;

namespace Lexus2_0.Core.Models
{
    public class BookingConfig
    {
        [JsonPropertyName("TRAIN_NO")]
        public string TrainNo { get; set; } = string.Empty;
        
        [JsonPropertyName("TRAIN_COACH")]
        public string TrainCoach { get; set; } = "SL";
        
        [JsonPropertyName("SOURCE_STATION")]
        public string SourceStation { get; set; } = string.Empty;
        
        [JsonPropertyName("DESTINATION_STATION")]
        public string DestinationStation { get; set; } = string.Empty;
        
        [JsonPropertyName("TRAVEL_DATE")]
        public string TravelDate { get; set; } = string.Empty;
        
        [JsonPropertyName("BOARDING_STATION")]
        public string? BoardingStation { get; set; }
        
        [JsonPropertyName("TATKAL")]
        public bool Tatkal { get; set; } = false;
        
        [JsonPropertyName("PREMIUM_TATKAL")]
        public bool PremiumTatkal { get; set; } = false;
        
        [JsonPropertyName("PASSENGER_DETAILS")]
        public List<PassengerDetail> PassengerDetails { get; set; } = new();
        
        [JsonPropertyName("USERNAME")]
        public string Username { get; set; } = string.Empty;
        
        [JsonPropertyName("PASSWORD")]
        public string Password { get; set; } = string.Empty;
        
        [JsonPropertyName("UPI_ID")]
        public string? UpiId { get; set; }
        
        [JsonPropertyName("PROXY_CONFIG")]
        public ProxyConfiguration? ProxyConfig { get; set; }
        
        [JsonPropertyName("CAPTCHA_SOLVER_TYPE")]
        public string CaptchaSolverType { get; set; } = "EasyOCR"; // EasyOCR, Tesseract, Manual
        
        [JsonPropertyName("HEADLESS_MODE")]
        public bool HeadlessMode { get; set; } = true; // Run browser in headless mode (no window)
        
        [JsonPropertyName("TOKEN_CONFIG")]
        public TokenConfig? TokenConfig { get; set; } // TeslaX-style token configuration for API-based booking
    }

    public class PassengerDetail
    {
        [JsonPropertyName("NAME")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("AGE")]
        public int Age { get; set; }
        
        [JsonPropertyName("GENDER")]
        public string Gender { get; set; } = "Male";
        
        [JsonPropertyName("SEAT")]
        public string Seat { get; set; } = "No Preference";
        
        [JsonPropertyName("FOOD")]
        public string Food { get; set; } = "No Food";
    }
}

