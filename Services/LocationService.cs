using System;
using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Services
{
    /// <summary>
    /// Service for coordinate resolution, Haversine distance calculations,
    /// and distance-based delivery fee computation.
    /// </summary>
    public static class LocationService
    {
        public const decimal DefaultRatePerKm = 30m;
        public const decimal MinDeliveryFee = 40m;
        public const decimal DefaultIntraCityDistanceKm = 3.2m;
        public const decimal MaxDirectDistanceKm = 40m;

        // Coordinates database for major cities and popular localities in Pakistan
        private static readonly Dictionary<string, (double Lat, double Lng)> CityCentroids =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Islamabad", (33.6844, 73.0479) },
                { "Rawalpindi", (33.5651, 73.0169) },
                { "Lahore", (31.5204, 74.3587) },
                { "Karachi", (24.8607, 67.0011) },
                { "Peshawar", (34.0151, 71.5249) },
                { "Faisalabad", (31.4504, 73.1350) },
                { "Multan", (30.1575, 71.5249) },
                { "Gujranwala", (32.1877, 74.1945) },
                { "Quetta", (30.1798, 66.9750) },
                { "Sialkot", (32.4945, 74.5229) }
            };

        private static readonly Dictionary<string, (double Lat, double Lng)> AreaCoordinates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Islamabad Areas
                { "F-6", (33.7290, 73.0740) },
                { "F-7", (33.7215, 73.0566) },
                { "F-8", (33.7118, 73.0375) },
                { "F-10", (33.6925, 73.0035) },
                { "F-11", (33.6844, 72.9866) },
                { "G-6", (33.7125, 73.0850) },
                { "G-7", (33.7042, 73.0644) },
                { "G-8", (33.6936, 73.0455) },
                { "G-9", (33.6888, 73.0270) },
                { "G-10", (33.6766, 73.0076) },
                { "G-11", (33.6681, 72.9890) },
                { "Blue Area", (33.7099, 73.0583) },
                { "I-8", (33.6687, 73.0754) },
                { "I-9", (33.6601, 73.0543) },
                { "I-10", (33.6495, 73.0336) },
                { "E-11", (33.7010, 72.9750) },
                { "DHA Islamabad", (33.5220, 73.1550) },
                { "Bahria Town Islamabad", (33.5350, 73.1190) },

                // Rawalpindi Areas
                { "Saddar", (33.5930, 73.0550) },
                { "Satellite Town", (33.6335, 73.0678) },
                { "Commercial Market", (33.6385, 73.0710) },
                { "Westridge", (33.6015, 73.0235) },
                { "Bahria Town Rawalpindi", (33.5350, 73.1190) },
                { "Chaklala Scheme 3", (33.5850, 73.0870) },
                { "Adyala Road", (33.5310, 73.0280) },
                { "Peshawar Road", (33.6095, 73.0120) },

                // Lahore Areas
                { "Gulberg", (31.5126, 74.3444) },
                { "DHA Lahore", (31.4700, 74.3980) },
                { "Model Town", (31.4850, 74.3210) },
                { "Johar Town", (31.4697, 74.2728) },
                { "Bahria Town Lahore", (31.3650, 74.1800) },
                { "Cantt", (31.5450, 74.3850) },
                { "Mall Road", (31.5600, 74.3250) },
                { "Iqbal Town", (31.5110, 74.2880) },

                // Karachi Areas
                { "Clifton", (24.8138, 67.0300) },
                { "DHA Karachi", (24.8050, 67.0650) },
                { "Gulshan-e-Iqbal", (24.9200, 67.0900) },
                { "North Nazimabad", (24.9400, 67.0400) },
                { "PECHS", (24.8700, 67.0600) },
                { "Saddar Karachi", (24.8550, 67.0150) },
                { "Korangi", (24.8300, 67.1300) },
                { "Malir", (24.8900, 67.2000) },

                // Peshawar Areas
                { "Hayatabad", (33.9900, 71.4400) },
                { "University Town", (34.0050, 71.5000) },
                { "Saddar Peshawar", (34.0080, 71.5450) },

                // Faisalabad Areas
                { "D Ground", (31.4120, 73.1030) },
                { "Madina Town", (31.4350, 73.1200) },
                { "Peoples Colony", (31.4050, 73.1100) },

                // Multan Areas
                { "Cantt Multan", (30.1900, 71.4450) },
                { "Gulgasht Colony", (30.2250, 71.4800) },
                { "Shah Rukn-e-Alam", (30.1850, 71.5000) }
            };

        /// <summary>
        /// Resolves approximate latitude and longitude given city and area names.
        /// </summary>
        public static (double Lat, double Lng) ResolveCoordinates(string? city, string? area)
        {
            // 1. Try matching area first
            if (!string.IsNullOrWhiteSpace(area))
            {
                string cleanArea = area.Trim();
                if (AreaCoordinates.TryGetValue(cleanArea, out var areaCoords))
                {
                    return areaCoords;
                }

                // Check if any area key is a substring
                foreach (var kvp in AreaCoordinates)
                {
                    if (cleanArea.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Contains(cleanArea, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }
            }

            // 2. Try matching city
            if (!string.IsNullOrWhiteSpace(city))
            {
                string cleanCity = city.Trim();
                if (CityCentroids.TryGetValue(cleanCity, out var cityCoords))
                {
                    return cityCoords;
                }

                foreach (var kvp in CityCentroids)
                {
                    if (cleanCity.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }
            }

            // Default centroid: Islamabad
            return (33.6844, 73.0479);
        }

        /// <summary>
        /// Calculates distance in KM using the Haversine formula.
        /// </summary>
        public static decimal CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            if ((lat1 == 0 && lon1 == 0) || (lat2 == 0 && lon2 == 0))
            {
                return DefaultIntraCityDistanceKm;
            }

            const double earthRadiusKm = 6371.0;

            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double rLat1 = DegreesToRadians(lat1);
            double rLat2 = DegreesToRadians(lat2);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(rLat1) * Math.Cos(rLat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = earthRadiusKm * c;

            return Math.Round((decimal)distance, 2, MidpointRounding.AwayFromZero);
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        /// <summary>
        /// Calculates distance, delivery fee and total with fallbacks and minimum base fee.
        /// DeliveryFee = Math.Max(40, Math.Round(distanceKm * ratePerKm))
        /// </summary>
        public static (decimal DistanceKm, decimal DeliveryFee, decimal Total) CalculateDelivery(
            double custLat,
            double custLng,
            double kitchenLat,
            double kitchenLng,
            decimal ratePerKm,
            decimal subtotal,
            string? custCity = null,
            string? custArea = null,
            string? kitchenCity = null,
            string? kitchenArea = null)
        {
            // Resolve missing kitchen coordinates
            if (kitchenLat == 0 && kitchenLng == 0)
            {
                var kCoords = ResolveCoordinates(kitchenCity, kitchenArea);
                kitchenLat = kCoords.Lat;
                kitchenLng = kCoords.Lng;
            }

            // Resolve missing customer coordinates
            if (custLat == 0 && custLng == 0)
            {
                var cCoords = ResolveCoordinates(custCity, custArea);
                custLat = cCoords.Lat;
                custLng = cCoords.Lng;
            }

            decimal distanceKm = CalculateDistanceKm(custLat, custLng, kitchenLat, kitchenLng);

            // Safeguard & Fallback: If distance > 40 KM or coordinates were 0, use intra-city average (3.2 KM)
            if (distanceKm > MaxDirectDistanceKm || distanceKm <= 0)
            {
                distanceKm = DefaultIntraCityDistanceKm;
            }

            decimal rate = ratePerKm > 0 ? ratePerKm : DefaultRatePerKm;
            decimal fee = Math.Max(MinDeliveryFee, Math.Round(distanceKm * rate, 0, MidpointRounding.AwayFromZero));
            decimal total = Math.Round(subtotal + fee, 2, MidpointRounding.AwayFromZero);

            return (distanceKm, fee, total);
        }
    }
}
