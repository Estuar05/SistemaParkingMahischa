namespace SistemaParkingMahischa.Helpers;

public static class MoneyHelper
{
    public static string Format(decimal amount) => amount.ToString("C0");
}

