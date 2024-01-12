using System.Net;
using System.Net.Sockets;
using NpgsqlTypes;

namespace SS14.Admin.Helpers
{
    public static class IPHelper
    {
        public static bool TryParseIpOrCidr(string str, out (IPAddress, byte?) cidr)
        {
            if (IPAddress.TryParse(str, out var addr))
            {
                cidr = (addr, null);
                return true;
            }

            var res = TryParseCidr(str, out var cidrParsed);
            cidr = (cidrParsed.Address, cidrParsed.Netmask);
            return res;
        }

        public static bool TryParseCidr(string str, out NpgsqlInet cidr)
        {
            cidr = default;

            IPAddress? address;
            byte mask;

            var split = str.Split("/");
            if (split.Length != 2)
                return false;

            if (!IPAddress.TryParse(split[0], out address))
                return false;

            if (!byte.TryParse(split[1], out mask))
                return false;

            cidr = new NpgsqlInet(address, mask);
            return true;
        }

        public static string FormatCidr(this NpgsqlInet cidr)
        {
            return $"{cidr.Address}/{cidr.Netmask}";
        }
    }
}
