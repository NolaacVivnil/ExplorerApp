using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public class MapLinkResolver : MonoBehaviour
{
    async void Start()
    {
        string shortUrl = "https://maps.app.goo.gl/CB1c1V3dsyyGUEXS8";
        Debug.Log("Resolving map link...");

        var coords = await GetCoordinatesFromShortUrlAsync(shortUrl);
        
        if (coords.HasValue)
        {
            Debug.Log($"Found Coords - Lat: {coords.Value.Lat}, Lng: {coords.Value.Lng}");
        }
        else
        {
            Debug.LogError("Could not extract coordinates.");
        }
    }

    public async Task<(double Lat, double Lng)?> GetCoordinatesFromShortUrlAsync(string shortUrl)
    {
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler);
            
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            HttpResponseMessage response = await client.GetAsync(shortUrl);

            if ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399)
            {
                string expandedUrl = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(expandedUrl))
                {
                    return ExtractCoordsFromUrl(expandedUrl);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Network exception on Android: {ex.Message}");
        }

        return null;
    }

    private (double Lat, double Lng)? ExtractCoordsFromUrl(string url)
    {
        var match = Regex.Match(url, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
        if (match.Success && match.Groups.Count >= 3)
        {
            if (double.TryParse(match.Groups[1].Value, out double lat) &&
                double.TryParse(match.Groups[2].Value, out double lng))
            {
                return (lat, lng);
            }
        }
        return null;
    }
}