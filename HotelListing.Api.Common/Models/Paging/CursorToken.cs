using System.Text;
using System.Text.Json;

namespace HotelListing.Api.Common.Models.Paging;

/// <summary>
/// Internal structure of our opaque cursor.
/// Encodes both the reference ID and the direction of navigation.
/// </summary>
public record CursorToken(int Id, bool IsBackward)
{
    public static string Encode(int id, bool isBackward)
    {
        var json = JsonSerializer.Serialize(new CursorToken(id, isBackward));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static CursorToken? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return JsonSerializer.Deserialize<CursorToken>(json);
        }
        catch
        {
            return null;
        }
    }
}
