using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

class Program
{
    public static string RunProcess(string command, string args)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(psi))
        {
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }
        return "";
    }

    public static int NumServersForPortProperties(int port)
    {
        int num = 0;
        try
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation info in tcpConnections)
            {
                if (info.LocalEndPoint.Port == port && info.RemoteEndPoint.Port == 0) num++;
            }
        }
        catch (Exception) {return -1;}
        return num;
    }

    public static int NumServersForPortNetstat(int port)
    {
        int num = 0;
        try
        {
            string netstatOutput = RunProcess("netstat", "-an -p tcp");
            Regex regex = new Regex(@"^.*(?i:tcp).*?[:.](?<LocalPort>\d+)\b\s.*\bLISTEN[ING]*\b", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(netstatOutput);
            foreach (Match match in matches)
            {
                if (int.Parse(match.Groups["LocalPort"].Value) == port) num++;
            }
        }
        catch (Exception) {return -1;}
        return num;
    }

    static void Main(string[] args)
    {
        int port = 13333;
        int num = NumServersForPortNetstat(port);
        Console.WriteLine(num);
        if (num == -1) num = NumServersForPortProperties(port);
        Console.WriteLine(num);
    }
}
