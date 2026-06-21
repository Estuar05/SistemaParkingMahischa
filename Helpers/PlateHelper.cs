using System.Text;

namespace SistemaParkingMahischa.Helpers;

public static class PlateHelper
{
    public static string Normalize(string plate)
    {
        var builder = new StringBuilder();
        foreach (var character in plate.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}

