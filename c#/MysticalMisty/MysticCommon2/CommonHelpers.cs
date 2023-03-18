namespace MysticCommon
{
    public class CommonHelpers
    {
        public static bool IsValidIp(string ip)
        {
            return !string.IsNullOrWhiteSpace(ip) && ip.Split(".").Length == 4 && !ip.Trim().Equals("0.0.0.0") && !ip.Trim().Equals("255.255.255.255");
        }
    }
}