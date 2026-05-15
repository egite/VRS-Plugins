using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualRadar.Plugin.RegistrationData
{
    enum MatchType
    {
        Exact,
        Close,
        Possible
    }

    class PilotMatch
    {
        public Dictionary<string, string> AirmenRecord { get; set; }
        public List<Dictionary<string, string>> Certificates { get; set; }
        public int Score { get; set; }
        public MatchType MatchType { get; set; }
    }

    class PilotMatcher
    {
        private readonly RegistrationDatabase _Database;
        private readonly int _MaxDistance;

        public PilotMatcher(RegistrationDatabase database, int maxDistance)
        {
            _Database = database;
            _MaxDistance = maxDistance;
        }

        public List<PilotMatch> FindMatches(string ownerName, string street, string city, string state, string registrantType)
        {
            var results = new List<PilotMatch>();

            // Corporate registrations — skip name-based matching but still search by address
            bool skipNameMatch = false;
            if(!string.IsNullOrEmpty(registrantType)) {
                int regType;
                if(int.TryParse(registrantType, out regType) && regType >= 2 && regType <= 8 && regType != 2 && regType != 4) {
                    skipNameMatch = true; // 2 = Partnership, 4 = Co-Owned — still do name matching
                }
            }

            if(skipNameMatch || string.IsNullOrWhiteSpace(ownerName)) {
                // Address-only search for corporate/LLC/blank registrations
                var addrOnlyAirmen = _Database.GetAirmenByAddress(street, city, state);
                foreach(var airman in addrOnlyAirmen) {
                    int score = 10; // Base score for address match
                    string airmanStreet;
                    airman.TryGetValue("street1", out airmanStreet);
                    if(!string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(airmanStreet) &&
                       RegistrationDatabase.NormalizeStreet(street) == RegistrationDatabase.NormalizeStreet(airmanStreet)) {
                        score += 50;
                    }
                    if(!string.IsNullOrEmpty(city)) score += 30;
                    if(!string.IsNullOrEmpty(state)) score += 20;

                    string uniqueId;
                    airman.TryGetValue("unique_id", out uniqueId);
                    var certs = _Database.GetCertificatesForAirman(uniqueId ?? "");

                    results.Add(new PilotMatch {
                        AirmenRecord = airman,
                        Certificates = certs,
                        Score = score,
                        MatchType = score >= 70 ? MatchType.Close : MatchType.Possible,
                    });
                }
                return results.OrderByDescending(m => m.Score).Take(20).ToList();
            }

            string firstName, lastName, middleName;
            ParseOwnerName(ownerName.Trim(), out lastName, out firstName, out middleName);

            if(string.IsNullOrEmpty(lastName)) return results;

            var airmen = _Database.GetAirmenByLastName(lastName);
            var fuzzyAirmen = _Database.GetAirmenByLastNameFuzzy(lastName);
            var addressAirmen = _Database.GetAirmenByAddress(street, city, state);

            // Track seen unique_ids to avoid scoring the same airman twice
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Tag each candidate with its source: exact name, fuzzy name, or address-only
            // 0 = exact last name, 1 = fuzzy last name, 2 = address-only (no name match required)
            var scoredAirmen = new List<KeyValuePair<Dictionary<string, string>, int>>();
            foreach(var a in airmen) {
                string uid; a.TryGetValue("unique_id", out uid);
                if(seenIds.Add(uid ?? "")) scoredAirmen.Add(new KeyValuePair<Dictionary<string, string>, int>(a, 0));
            }
            foreach(var a in fuzzyAirmen) {
                string uid; a.TryGetValue("unique_id", out uid);
                if(seenIds.Add(uid ?? "")) scoredAirmen.Add(new KeyValuePair<Dictionary<string, string>, int>(a, 1));
            }
            foreach(var a in addressAirmen) {
                string uid; a.TryGetValue("unique_id", out uid);
                if(seenIds.Add(uid ?? "")) scoredAirmen.Add(new KeyValuePair<Dictionary<string, string>, int>(a, 2));
            }

            var upperMiddle = (middleName ?? "").ToUpperInvariant();

            foreach(var entry in scoredAirmen) {
                var airman = entry.Key;
                int source = entry.Value;
                bool addressOnly = source == 2;
                int score = 0;
                string airmanFirst;
                airman.TryGetValue("first_name", out airmanFirst);
                airmanFirst = (airmanFirst ?? "").Trim().ToUpperInvariant();

                // Last name scoring
                if(source == 1) {
                    score -= 15; // Penalty for fuzzy last name
                }

                if(addressOnly) {
                    // Address-only candidate: score based on name similarity if any,
                    // but don't require a name match
                    score += 10; // Base score for living at same address
                    if(!string.IsNullOrEmpty(firstName)) {
                        var upperFirst = firstName.ToUpperInvariant();
                        string airmanLast;
                        airman.TryGetValue("last_name", out airmanLast);
                        airmanLast = (airmanLast ?? "").Trim().ToUpperInvariant();

                        // Check if last name matches at all
                        if(airmanLast == lastName.ToUpperInvariant()) {
                            score += 40; // Same address + same last name
                        }
                        // Check first name similarity
                        if(airmanFirst == upperFirst) {
                            score += 100;
                        } else if(airmanFirst.StartsWith(upperFirst) || upperFirst.StartsWith(airmanFirst)) {
                            score += 70;
                        } else if(LevenshteinDistance(airmanFirst, upperFirst) <= _MaxDistance) {
                            score += 50;
                        }
                    }
                } else if(string.IsNullOrEmpty(firstName)) {
                    score += 20; // Last name only match
                } else {
                    var upperFirst = firstName.ToUpperInvariant();

                    if(airmanFirst == upperFirst) {
                        score += 100;
                    } else if(airmanFirst.StartsWith(upperFirst) || upperFirst.StartsWith(airmanFirst)) {
                        score += 70;
                    } else if(LevenshteinDistance(airmanFirst, upperFirst) <= _MaxDistance) {
                        score += 50;
                    } else {
                        continue; // No first name match at all
                    }
                }

                // Middle name scoring — compare owner's middle name/initial against
                // the airman's first_name field which may contain a middle component
                // (e.g. "JOHN ALBERT" or "JOHN A")
                if(!string.IsNullOrEmpty(upperMiddle)) {
                    var airmanMiddle = ExtractMiddleName(airmanFirst);
                    if(!string.IsNullOrEmpty(airmanMiddle)) {
                        if(upperMiddle.Length > 1 && airmanMiddle.Length > 1) {
                            // Both sides have full middle names — compare fully
                            if(airmanMiddle == upperMiddle) {
                                score += 30; // Full middle name match
                            } else if(airmanMiddle[0] == upperMiddle[0]) {
                                score += 10; // Same initial but different middle name
                            } else {
                                score -= 15; // Middle name conflicts
                            }
                        } else {
                            // One or both sides only have an initial — compare initials
                            if(airmanMiddle[0] == upperMiddle[0]) {
                                score += 25; // Middle initial matches
                            } else {
                                score -= 15; // Middle initial conflicts
                            }
                        }
                    }
                }

                string airmanStreet, airmanCity, airmanState;
                airman.TryGetValue("street1", out airmanStreet);
                airman.TryGetValue("city", out airmanCity);
                airman.TryGetValue("state", out airmanState);

                if(!string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(airmanStreet) &&
                   string.Equals(street.Trim(), airmanStreet.Trim(), StringComparison.OrdinalIgnoreCase)) {
                    score += 50;
                }

                if(!string.IsNullOrEmpty(city) && string.Equals(city, airmanCity, StringComparison.OrdinalIgnoreCase)) {
                    score += 30;
                }

                if(!string.IsNullOrEmpty(state) && string.Equals(state, airmanState, StringComparison.OrdinalIgnoreCase)) {
                    score += 20;
                }

                MatchType matchType;
                if(score >= 100) matchType = MatchType.Exact;
                else if(score >= 70) matchType = MatchType.Close;
                else matchType = MatchType.Possible;

                string uniqueId;
                airman.TryGetValue("unique_id", out uniqueId);
                var certs = _Database.GetCertificatesForAirman(uniqueId ?? "");

                results.Add(new PilotMatch {
                    AirmenRecord = airman,
                    Certificates = certs,
                    Score = score,
                    MatchType = matchType,
                });
            }

            return results.OrderByDescending(m => m.Score).Take(20).ToList();
        }

        /// <summary>
        /// Finds pilot matches for an experimental aircraft builder/manufacturer name.
        /// Tries both "LAST FIRST" and "FIRST LAST" orderings, and also includes
        /// last-name-only matches since the builder and pilot may be related (e.g. family).
        /// </summary>
        public List<PilotMatch> FindBuilderMatches(string builderName, string street, string city, string state)
        {
            var results = new List<PilotMatch>();
            if(string.IsNullOrWhiteSpace(builderName)) return results;

            var seenIds = new HashSet<string>();

            // Try standard "LAST FIRST" ordering
            var matches1 = FindMatches(builderName, street, city, state, "1");
            foreach(var m in matches1) {
                string uid;
                m.AirmenRecord.TryGetValue("unique_id", out uid);
                if(seenIds.Add(uid ?? "")) results.Add(m);
            }

            // Also try reversed "FIRST LAST" ordering
            var parts = builderName.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length >= 2) {
                var reversed = parts[1] + " " + parts[0];
                var matches2 = FindMatches(reversed, street, city, state, "1");
                foreach(var m in matches2) {
                    string uid;
                    m.AirmenRecord.TryGetValue("unique_id", out uid);
                    if(seenIds.Add(uid ?? "")) results.Add(m);
                }
            }

            // Last-name-only matching: builder may differ from pilot but share surname
            if(parts.Length >= 1) {
                var lastNames = new List<string> { parts[0] };
                if(parts.Length >= 2) lastNames.Add(parts[1]); // try both parts as potential last names

                foreach(var ln in lastNames) {
                    var airmen = _Database.GetAirmenByLastName(ln);
                    foreach(var airman in airmen) {
                        string uid;
                        airman.TryGetValue("unique_id", out uid);
                        if(seenIds.Contains(uid ?? "")) continue;

                        int score = 10; // Base score for last-name-only builder match
                        string airmanStreet, airmanCity, airmanState;
                        airman.TryGetValue("street1", out airmanStreet);
                        airman.TryGetValue("city", out airmanCity);
                        airman.TryGetValue("state", out airmanState);

                        if(!string.IsNullOrEmpty(street) && !string.IsNullOrEmpty(airmanStreet) &&
                           string.Equals(street.Trim(), airmanStreet.Trim(), StringComparison.OrdinalIgnoreCase)) {
                            score += 50;
                        }
                        if(!string.IsNullOrEmpty(city) && string.Equals(city, airmanCity, StringComparison.OrdinalIgnoreCase)) {
                            score += 30;
                        }
                        if(!string.IsNullOrEmpty(state) && string.Equals(state, airmanState, StringComparison.OrdinalIgnoreCase)) {
                            score += 20;
                        }

                        seenIds.Add(uid ?? "");
                        var certs = _Database.GetCertificatesForAirman(uid ?? "");
                        results.Add(new PilotMatch {
                            AirmenRecord = airman,
                            Certificates = certs,
                            Score = score,
                            MatchType = score >= 50 ? MatchType.Close : MatchType.Possible,
                        });
                    }
                }
            }

            return results.OrderByDescending(m => m.Score).Take(20).ToList();
        }

        private static void ParseOwnerName(string ownerName, out string lastName, out string firstName, out string middleName)
        {
            // FAA format is typically "LASTNAME FIRSTNAME MIDDLENAME" or "LASTNAME FIRSTNAME M"
            firstName = "";
            lastName = "";
            middleName = "";

            if(string.IsNullOrEmpty(ownerName)) return;

            var parts = ownerName.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length == 0) return;

            lastName = parts[0].Trim();
            if(parts.Length > 1) {
                firstName = parts[1].Trim();
            }
            if(parts.Length > 2) {
                middleName = parts[2].Trim();
            }
        }

        /// <summary>
        /// Extracts the middle name/initial from an airman first_name field.
        /// FAA airmen first_name may be "JOHN", "JOHN A", or "JOHN ALBERT".
        /// Returns the full second word, or empty string.
        /// </summary>
        private static string ExtractMiddleName(string airmanFirstName)
        {
            if(string.IsNullOrEmpty(airmanFirstName)) return "";
            var parts = airmanFirstName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length < 2) return "";
            return parts[1].ToUpperInvariant();
        }

        private static int LevenshteinDistance(string s, string t)
        {
            if(string.IsNullOrEmpty(s)) return (t ?? "").Length;
            if(string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            var d = new int[n + 1, m + 1];

            for(int i = 0; i <= n; i++) d[i, 0] = i;
            for(int j = 0; j <= m; j++) d[0, j] = j;

            for(int i = 1; i <= n; i++) {
                for(int j = 1; j <= m; j++) {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
