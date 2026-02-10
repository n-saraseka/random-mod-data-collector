using System.Globalization;

namespace random_mod_data_collector;

public class Utils
{
    public static string ToPointDecimalString(double value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}