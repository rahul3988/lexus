using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lexus2_0.Desktop.Services
{
    public class StationSearchService
    {
        private readonly HttpClient _httpClient;
        private List<StationInfo>? _cachedStations;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private const int CacheDurationMinutes = 60;
        private readonly string? _apiKey;

        public StationSearchService(string? apiKey = null)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _apiKey = apiKey;
        }

        public async Task<List<StationInfo>> SearchStationsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<StationInfo>();

            query = query.Trim();

            // Try GraphQL API first (Railway Backboard)
            var graphqlResults = await SearchStationsFromGraphQLAsync(query);
            if (graphqlResults != null && graphqlResults.Count > 0)
            {
                return graphqlResults;
            }

            // Try to fetch from Indian Rail API
            if (!string.IsNullOrEmpty(_apiKey))
            {
                var apiResults = await SearchStationsFromIndianRailApiAsync(query);
                if (apiResults != null && apiResults.Count > 0)
                {
                    return apiResults;
                }
            }

            // Fallback to cached/local search
            if (_cachedStations == null || DateTime.Now > _cacheExpiry)
            {
                await LoadStationsAsync();
            }

            if (_cachedStations == null)
                return new List<StationInfo>();

            query = query.ToUpper();

            // Search by station code or name
            return _cachedStations
                .Where(s => 
                    s.Code.StartsWith(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Code.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(s => s.Name)
                .Take(20)
                .ToList();
        }

        private async Task<List<StationInfo>?> SearchStationsFromGraphQLAsync(string query)
        {
            try
            {
                // GraphQL query for station search
                var graphqlQuery = new
                {
                    query = @"
                        query SearchStations($query: String!) {
                            stations(query: $query, limit: 20) {
                                code
                                name
                            }
                        }",
                    variables = new
                    {
                        query = query
                    }
                };

                var json = JsonSerializer.Serialize(graphqlQuery);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://backboard.railway.com/graphql/v2", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var graphqlResponse = JsonSerializer.Deserialize<GraphQLResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (graphqlResponse?.Data?.Stations != null)
                    {
                        return graphqlResponse.Data.Stations.Select(s => new StationInfo
                        {
                            Code = s.Code ?? "",
                            Name = s.Name ?? ""
                        }).ToList();
                    }
                }
            }
            catch
            {
                // GraphQL API failed, will use fallback
            }

            return null;
        }

        private async Task<List<StationInfo>?> SearchStationsFromIndianRailApiAsync(string query)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return null;

                // Indian Rail API Auto Complete Station endpoint
                // Format: http://indianrailapi.com/api/v2/AutoCompleteStation/apikey/{apikey}/StationCodeOrName/{query}/
                var encodedQuery = Uri.EscapeDataString(query);
                var apiUrl = $"http://indianrailapi.com/api/v2/AutoCompleteStation/apikey/{_apiKey}/StationCodeOrName/{encodedQuery}/";

                var response = await _httpClient.GetAsync(apiUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Parse the API response
                    var apiResponse = JsonSerializer.Deserialize<IndianRailApiResponse>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (apiResponse?.ResponseCode == 200 && apiResponse.Stations != null)
                    {
                        return apiResponse.Stations.Select(s => new StationInfo
                        {
                            Code = s.StationCode ?? "",
                            Name = s.StationName ?? ""
                        }).ToList();
                    }
                }
            }
            catch
            {
                // API failed, will use fallback
            }

            return null;
        }

        public async Task<StationInfo?> GetStationByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            // Try GraphQL API first
            try
            {
                var graphqlQuery = new
                {
                    query = @"
                        query GetStation($code: String!) {
                            station(code: $code) {
                                code
                                name
                            }
                        }",
                    variables = new
                    {
                        code = code.ToUpper()
                    }
                };

                var json = JsonSerializer.Serialize(graphqlQuery);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://backboard.railway.com/graphql/v2", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var graphqlResponse = JsonSerializer.Deserialize<GraphQLStationResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (graphqlResponse?.Data?.Station != null)
                    {
                        return new StationInfo
                        {
                            Code = graphqlResponse.Data.Station.Code ?? "",
                            Name = graphqlResponse.Data.Station.Name ?? ""
                        };
                    }
                }
            }
            catch
            {
                // GraphQL failed, use fallback
            }

            // Fallback to cached search
            if (_cachedStations == null || DateTime.Now > _cacheExpiry)
            {
                await LoadStationsAsync();
            }

            return _cachedStations?.FirstOrDefault(s => 
                s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<TrainInfo>> SearchTrainsAsync(string fromCode, string toCode, string date)
        {
            try
            {
                var graphqlQuery = new
                {
                    query = @"
                        query SearchTrains($from: String!, $to: String!, $date: String!) {
                            trains(from: $from, to: $to, date: $date) {
                                number
                                name
                                from
                                to
                                departure
                                arrival
                            }
                        }",
                    variables = new
                    {
                        from = fromCode.ToUpper(),
                        to = toCode.ToUpper(),
                        date = date
                    }
                };

                var json = JsonSerializer.Serialize(graphqlQuery);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://backboard.railway.com/graphql/v2", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var graphqlResponse = JsonSerializer.Deserialize<GraphQLTrainsResponse>(responseContent, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (graphqlResponse?.Data?.Trains != null)
                    {
                        return graphqlResponse.Data.Trains.Select(t => new TrainInfo
                        {
                            Number = t.Number ?? "",
                            Name = t.Name ?? "",
                            From = t.From ?? "",
                            To = t.To ?? "",
                            Departure = t.Departure ?? "",
                            Arrival = t.Arrival ?? ""
                        }).ToList();
                    }
                }
            }
            catch
            {
                // API failed
            }

            return new List<TrainInfo>();
        }

        private async Task LoadStationsAsync()
        {
            try
            {
                // Try to fetch from API first
                var stations = await FetchStationsFromApiAsync();
                
                if (stations == null || stations.Count == 0)
                {
                    // Fallback to hardcoded common stations
                    stations = GetCommonStations();
                }

                _cachedStations = stations;
                _cacheExpiry = DateTime.Now.AddMinutes(CacheDurationMinutes);
            }
            catch
            {
                // Use fallback stations on error
                _cachedStations = GetCommonStations();
                _cacheExpiry = DateTime.Now.AddMinutes(CacheDurationMinutes);
            }
        }

        private async Task<List<StationInfo>?> FetchStationsFromApiAsync()
        {
            try
            {
                // Try to fetch all stations from Indian Rail API if API key is available
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    // Note: Indian Rail API might not have a "get all stations" endpoint
                    // So we'll use the autocomplete with empty/minimal query or use fallback
                    // For now, we'll use the fallback list
                }
            }
            catch
            {
                // API failed, will use fallback
            }

            return null;
        }

        private List<StationInfo> GetCommonStations()
        {
            // Common Indian Railway stations with codes
            return new List<StationInfo>
            {
                new StationInfo { Code = "BSB", Name = "VARANASI JN" },
                new StationInfo { Code = "ST", Name = "SURAT" },
                new StationInfo { Code = "NDLS", Name = "NEW DELHI" },
                new StationInfo { Code = "MMCT", Name = "MUMBAI CENTRAL" },
                new StationInfo { Code = "CSTM", Name = "CSMT MUMBAI" },
                new StationInfo { Code = "PNBE", Name = "PATNA JN" },
                new StationInfo { Code = "HWH", Name = "HOWRAH JN" },
                new StationInfo { Code = "MAS", Name = "CHENNAI CENTRAL" },
                new StationInfo { Code = "SBC", Name = "BANGALORE CITY JN" },
                new StationInfo { Code = "ADI", Name = "AHMEDABAD JN" },
                new StationInfo { Code = "JP", Name = "JAIPUR" },
                new StationInfo { Code = "LKO", Name = "LUCKNOW NR" },
                new StationInfo { Code = "ALD", Name = "ALLAHABAD JN" },
                new StationInfo { Code = "CNB", Name = "KANPUR CENTRAL" },
                new StationInfo { Code = "BCT", Name = "MUMBAI BANDRA TERMINUS" },
                new StationInfo { Code = "BVI", Name = "BORIVALI" },
                new StationInfo { Code = "BDTS", Name = "BANDRA TERMINUS" },
                new StationInfo { Code = "PUNE", Name = "PUNE JN" },
                new StationInfo { Code = "HYB", Name = "HYDERABAD DECAN" },
                new StationInfo { Code = "SC", Name = "SECUNDERABAD JN" },
                new StationInfo { Code = "KGP", Name = "KHARAGPUR JN" },
                new StationInfo { Code = "BZA", Name = "VIJAYAWADA JN" },
                new StationInfo { Code = "VSKP", Name = "VISAKHAPATNAM" },
                new StationInfo { Code = "BBS", Name = "BHUBANESWAR" },
                new StationInfo { Code = "CTC", Name = "CUTTACK" },
                new StationInfo { Code = "RNC", Name = "RANCHI" },
                new StationInfo { Code = "JHS", Name = "JHANSI JN" },
                new StationInfo { Code = "GWL", Name = "GWALIOR JN" },
                new StationInfo { Code = "AGC", Name = "AGRA CANTT" },
                new StationInfo { Code = "MTJ", Name = "MATHURA JN" },
                new StationInfo { Code = "NZM", Name = "H NIZAMUDDIN" },
                new StationInfo { Code = "ANVT", Name = "ANAND VIHAR TERMINAL" },
                new StationInfo { Code = "DEE", Name = "DELHI S ROHILLA" },
                new StationInfo { Code = "DLI", Name = "OLD DELHI" },
                new StationInfo { Code = "JAT", Name = "JAMMU TAWI" },
                new StationInfo { Code = "UHP", Name = "UDHAMPUR" },
                new StationInfo { Code = "JRC", Name = "JALANDHAR CITY" },
                new StationInfo { Code = "ASR", Name = "AMRITSAR JN" },
                new StationInfo { Code = "LDH", Name = "LUDHIANA JN" },
                new StationInfo { Code = "CDG", Name = "CHANDIGARH" },
                new StationInfo { Code = "UMB", Name = "AMBALA CANT JN" },
                new StationInfo { Code = "KOTA", Name = "KOTA JN" },
                new StationInfo { Code = "RTM", Name = "RATLAM JN" },
                new StationInfo { Code = "INDB", Name = "INDORE JN BG" },
                new StationInfo { Code = "UJN", Name = "UJJAIN JN" },
                new StationInfo { Code = "BPL", Name = "BHOPAL JN" },
                new StationInfo { Code = "JBP", Name = "JABALPUR" },
                new StationInfo { Code = "NGP", Name = "NAGPUR" },
                new StationInfo { Code = "WR", Name = "WARDHA JN" },
                new StationInfo { Code = "AK", Name = "AKOLA JN" },
                new StationInfo { Code = "BSL", Name = "BHUSAVAL JN" },
                new StationInfo { Code = "NK", Name = "NASIK ROAD" },
                new StationInfo { Code = "MMR", Name = "MANMAD JN" },
                new StationInfo { Code = "JN", Name = "JALGAON JN" },
                new StationInfo { Code = "KOP", Name = "KOLHAPUR" },
                new StationInfo { Code = "MRJ", Name = "MIRAJ JN" },
                new StationInfo { Code = "SUR", Name = "SOLAPUR JN" },
                new StationInfo { Code = "GTL", Name = "GUNTAKAL JN" },
                new StationInfo { Code = "YPR", Name = "YESVANTPUR JN" },
                new StationInfo { Code = "MYS", Name = "MYSORE JN" },
                new StationInfo { Code = "SBC", Name = "BANGALORE CITY JN" },
                new StationInfo { Code = "KJM", Name = "KRISHNARAJAPURAM" },
                new StationInfo { Code = "BNC", Name = "BANGALORE CANT" },
                new StationInfo { Code = "YNK", Name = "YELAHANKA JN" },
                new StationInfo { Code = "TVC", Name = "TRIVANDRUM CNTL" },
                new StationInfo { Code = "ERS", Name = "ERNAKULAM JN" },
                new StationInfo { Code = "CLT", Name = "KOZHIKODE" },
                new StationInfo { Code = "CAN", Name = "KANNUR" },
                new StationInfo { Code = "MAQ", Name = "MANGALORE CNTL" },
                new StationInfo { Code = "MAO", Name = "MADGAON" },
                new StationInfo { Code = "VSG", Name = "VASCO DA GAMA" },
                new StationInfo { Code = "TCR", Name = "THRISUR" },
                new StationInfo { Code = "SRR", Name = "SHORANUR JN" },
                new StationInfo { Code = "QLN", Name = "QUILON JN" },
                new StationInfo { Code = "KCVL", Name = "KOCHUVELI" },
                new StationInfo { Code = "ALLP", Name = "ALLEPPEY" },
                new StationInfo { Code = "KTYM", Name = "KOTTAYAM" },
                new StationInfo { Code = "CNO", Name = "CHENNAI EGMORE" },
                new StationInfo { Code = "TBM", Name = "TAMBARAM" },
                new StationInfo { Code = "CGL", Name = "CHENGALPATTU" },
                new StationInfo { Code = "VM", Name = "VILLUPURAM JN" },
                new StationInfo { Code = "PDY", Name = "PUDUCHERRY" },
                new StationInfo { Code = "TPJ", Name = "TIRUCHIRAPALLI" },
                new StationInfo { Code = "MDU", Name = "MADURAI JN" },
                new StationInfo { Code = "DG", Name = "DINDIGUL JN" },
                new StationInfo { Code = "CAPE", Name = "KANYAKUMARI" },
                new StationInfo { Code = "NCJ", Name = "NAGERCOIL JN" },
                new StationInfo { Code = "TEN", Name = "TIRUNELVELI" },
                new StationInfo { Code = "VPT", Name = "VIRUDUNAGAR JN" },
                new StationInfo { Code = "SRT", Name = "SALEM JN" },
                new StationInfo { Code = "ED", Name = "ERODE JN" },
                new StationInfo { Code = "CBE", Name = "COIMBATORE JN" },
                new StationInfo { Code = "PGT", Name = "PALAKKAD" },
                new StationInfo { Code = "TCR", Name = "THRISUR" },
                new StationInfo { Code = "CLT", Name = "KOZHIKODE" },
                new StationInfo { Code = "CAN", Name = "KANNUR" },
                new StationInfo { Code = "MAQ", Name = "MANGALORE CNTL" },
                new StationInfo { Code = "MAO", Name = "MADGAON" },
                new StationInfo { Code = "VSG", Name = "VASCO DA GAMA" },
                new StationInfo { Code = "TCR", Name = "THRISUR" },
                new StationInfo { Code = "SRR", Name = "SHORANUR JN" },
                new StationInfo { Code = "QLN", Name = "QUILON JN" },
                new StationInfo { Code = "KCVL", Name = "KOCHUVELI" },
                new StationInfo { Code = "ALLP", Name = "ALLEPPEY" },
                new StationInfo { Code = "KTYM", Name = "KOTTAYAM" },
                new StationInfo { Code = "CNO", Name = "CHENNAI EGMORE" },
                new StationInfo { Code = "TBM", Name = "TAMBARAM" },
                new StationInfo { Code = "CGL", Name = "CHENGALPATTU" },
                new StationInfo { Code = "VM", Name = "VILLUPURAM JN" },
                new StationInfo { Code = "PDY", Name = "PUDUCHERRY" },
                new StationInfo { Code = "TPJ", Name = "TIRUCHIRAPALLI" },
                new StationInfo { Code = "MDU", Name = "MADURAI JN" },
                new StationInfo { Code = "DG", Name = "DINDIGUL JN" },
                new StationInfo { Code = "CAPE", Name = "KANYAKUMARI" },
                new StationInfo { Code = "NCJ", Name = "NAGERCOIL JN" },
                new StationInfo { Code = "TEN", Name = "TIRUNELVELI" },
                new StationInfo { Code = "VPT", Name = "VIRUDUNAGAR JN" },
                new StationInfo { Code = "SRT", Name = "SALEM JN" },
                new StationInfo { Code = "ED", Name = "ERODE JN" },
                new StationInfo { Code = "CBE", Name = "COIMBATORE JN" },
                new StationInfo { Code = "PGT", Name = "PALAKKAD" }
            };
        }

        private class GraphQLResponse
        {
            public GraphQLData? Data { get; set; }
            public List<GraphQLError>? Errors { get; set; }
        }

        private class GraphQLData
        {
            public List<GraphQLStation>? Stations { get; set; }
        }

        private class GraphQLStation
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
        }

        private class GraphQLError
        {
            public string? Message { get; set; }
        }

        private class GraphQLStationResponse
        {
            public GraphQLStationData? Data { get; set; }
            public List<GraphQLError>? Errors { get; set; }
        }

        private class GraphQLStationData
        {
            public GraphQLStation? Station { get; set; }
        }

        private class GraphQLTrainsResponse
        {
            public GraphQLTrainsData? Data { get; set; }
            public List<GraphQLError>? Errors { get; set; }
        }

        private class GraphQLTrainsData
        {
            public List<GraphQLTrain>? Trains { get; set; }
        }

        private class GraphQLTrain
        {
            public string? Number { get; set; }
            public string? Name { get; set; }
            public string? From { get; set; }
            public string? To { get; set; }
            public string? Departure { get; set; }
            public string? Arrival { get; set; }
        }

        private class IndianRailApiResponse
        {
            public int ResponseCode { get; set; }
            public string? Status { get; set; }
            public List<IndianRailStationItem>? Stations { get; set; }
        }

        private class IndianRailStationItem
        {
            public string? StationCode { get; set; }
            public string? StationName { get; set; }
        }
    }

    public class StationInfo
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";

        public override string ToString()
        {
            return $"{Code} - {Name}";
        }
    }

    public class TrainInfo
    {
        public string Number { get; set; } = "";
        public string Name { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Departure { get; set; } = "";
        public string Arrival { get; set; } = "";

        public override string ToString()
        {
            return $"{Number} - {Name} ({From} → {To})";
        }
    }
}

