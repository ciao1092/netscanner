

using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace netscanner;


static class ArpHelper
{
	public static string GetMacAddressFromIp(IPAddress target)
	{
		try
		{
			// Execute arp -a command
			ProcessStartInfo psi = new ProcessStartInfo("arp", "-a")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using (Process process = Process.Start(psi))
			{
				if (process == null)
				{
					throw new Exception("Failed to start arp process.");
				}

				// Read the output
				string output = process.StandardOutput.ReadToEnd();

				// Parse the output to find the MAC address
				string? line = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
									.FirstOrDefault(l => l.Contains(target.ToString()));

				if (line != null)
				{
					int startIndex = line.IndexOf("at ") + 3;
					int endIndex = line.IndexOf(" ", startIndex);
					string macAddress = line.Substring(startIndex, endIndex - startIndex);
					return macAddress;
				}
				else
				{
					return null; // IP address not found in ARP table
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred: {ex.Message}");
			return null;
		}
	}

}

class Program
{
	public static async Task Main(string[] args)
	{
		Console.WriteLine("Working...");
		var tasks = new List<Task>();
		var replies = new List<PingReply?>();
		List<int> numbers = [];
		for (int i = 0; i < 255; i++) numbers.Add(i);
		foreach (var i in numbers)
		{
			tasks.Add(
				Task.Run(async () =>
				{
					replies.Add(await Pinger(IPAddress.Parse($"192.168.1." + i), 10500));
				})
			);
		}

		await Task.WhenAll(tasks);

		foreach (var reply in replies)
		{
			if (reply is not null && reply.Status == IPStatus.Success)
			{
				string? hostName = await ResolveIPAddress(reply.Address);
				Console.WriteLine($"{reply.Address}" + (hostName is not null ? $" ({hostName})" : string.Empty) + $" is online. ({reply.RoundtripTime}) MAC: {ArpHelper.GetMacAddressFromIp(reply.Address)}");
			}
		}
	}

	private static async Task<PingReply?> Pinger(IPAddress iPAddress, int timeout = 1000)
	{
		try
		{
			return await new Ping().SendPingAsync(iPAddress, timeout);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return null;
		}
	}

	private static async Task<string?> ResolveIPAddress(IPAddress iPAddress)
	{
		try
		{
			return (await Dns.GetHostEntryAsync(iPAddress)).HostName;
		}
		catch
		{
			return default;
		}
	}
}