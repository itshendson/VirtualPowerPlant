using System.Globalization;
using System.Net.WebSockets;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Ingestion.Protos;

namespace TelemetrySender;

internal static class Program
{
    private const string DefaultUrl = "ws://localhost:5000/ws/telemetry";
    private static readonly string[] StatusOptions = { "Charging", "Discharging", "Idle", "Fault" };

    public static async Task<int> Main(string[] args)
    {
        Options options;

        try
        {
            options = Options.Parse(args, DefaultUrl);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            PrintHelp();
            return 1;
        }

        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        PromptForMissingOptions(options);

        using var socket = new ClientWebSocket();
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await socket.ConnectAsync(new Uri(options.Url), cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to connect: {ex.Message}");
            return 1;
        }

        var random = new Random();

        for (var i = 0; i < options.Count; i++)
        {
            var reading = options.Randomize == true
                ? GenerateRandomTelemetry(random)
                : PromptTelemetry();

            await SendTelemetryAsync(socket, reading, cts.Token);

            if (options.IntervalMs > 0 && i < options.Count - 1)
            {
                try
                {
                    await Task.Delay(options.IntervalMs, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
        }

        return 0;
    }

    private static async Task SendTelemetryAsync(ClientWebSocket socket, TelemetryReading reading, CancellationToken cancellationToken)
    {
        var payload = reading.ToByteArray();
        await socket.SendAsync(payload, WebSocketMessageType.Binary, true, cancellationToken);
        Console.WriteLine("Sent telemetry: MeterId={0}, Kw={1}, SoC={2}, Status={3}",
            reading.MeterId,
            reading.Kw.ToString("0.###", CultureInfo.InvariantCulture),
            reading.StateOfChargePercent.ToString("0.###", CultureInfo.InvariantCulture),
            reading.Status);
    }

    private static TelemetryReading GenerateRandomTelemetry(Random random)
    {
        var meterId = $"meter-{random.Next(1000, 9999)}";
        var gatewayId = $"gateway-{random.Next(1, 4)}";
        var firmware = $"1.0.{random.Next(0, 50)}";
        var status = StatusOptions[random.Next(StatusOptions.Length)];

        var kw = Math.Round(random.NextDouble() * 8.0, 3);
        var soc = Math.Round(random.NextDouble() * 100.0, 2);
        var temperature = Math.Round(15.0 + random.NextDouble() * 20.0, 2);

        var reading = new TelemetryReading
        {
            MeterId = meterId,
            ReadingTime = Timestamp.FromDateTime(DateTime.UtcNow),
            Kw = kw,
            StateOfChargePercent = soc,
            Status = status,
            GatewayId = gatewayId,
            FirmwareVersion = firmware
        };

        if (random.NextDouble() > 0.2)
        {
            reading.TemperatureC = temperature;
        }

        return reading;
    }

    private static TelemetryReading PromptTelemetry()
    {
        var meterId = PromptRequired("MeterId", "meter-1");
        var readingTime = PromptTimestamp();
        var kw = PromptDouble("Kw", 0, double.MaxValue, 1.5);
        var soc = PromptDouble("StateOfChargePercent", 0, 100, 55);
        var status = PromptStatus("Idle");
        var gatewayId = PromptRequired("GatewayId", "gateway-1");
        var firmware = PromptRequired("FirmwareVersion", "1.0.0");
        var temperature = PromptOptionalDouble("TemperatureC");

        var reading = new TelemetryReading
        {
            MeterId = meterId,
            ReadingTime = readingTime,
            Kw = kw,
            StateOfChargePercent = soc,
            Status = status,
            GatewayId = gatewayId,
            FirmwareVersion = firmware
        };

        if (temperature.HasValue)
        {
            reading.TemperatureC = temperature.Value;
        }

        return reading;
    }

    private static void PromptForMissingOptions(Options options)
    {
        if (string.IsNullOrWhiteSpace(options.Url))
        {
            options.Url = PromptRequired($"WebSocket URL [{DefaultUrl}]", DefaultUrl);
        }

        if (!options.Randomize.HasValue)
        {
            Console.Write("Mode (m)anual/(r)andom [m]: ");
            var input = Console.ReadLine();
            options.Randomize = input?.Trim().StartsWith("r", StringComparison.OrdinalIgnoreCase) == true;
        }

        if (options.Count <= 0)
        {
            options.Count = PromptInt("Message count", 1);
        }
        else
        {
            options.Count = PromptInt("Message count", options.Count);
        }

        if (options.Randomize == true)
        {
            if (options.IntervalMs <= 0)
            {
                options.IntervalMs = PromptInt("Interval ms between messages", 500);
            }
            else
            {
                options.IntervalMs = PromptInt("Interval ms between messages", options.IntervalMs);
            }
        }
        else
        {
            options.IntervalMs = 0;
        }
    }

    private static Timestamp PromptTimestamp()
    {
        while (true)
        {
            Console.Write("ReadingTimeUtc (blank for now): ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return Timestamp.FromDateTime(DateTime.UtcNow);
            }

            if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value))
            {
                return Timestamp.FromDateTime(value.UtcDateTime);
            }

            Console.WriteLine("Invalid time. Try ISO 8601 like 2024-08-01T12:34:56Z.");
        }
    }

    private static string PromptRequired(string label, string? defaultValue = null)
    {
        while (true)
        {
            var prompt = string.IsNullOrWhiteSpace(defaultValue) ? $"{label}: " : $"{label} [{defaultValue}]: ";
            Console.Write(prompt);
            var input = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            if (!string.IsNullOrWhiteSpace(defaultValue))
            {
                return defaultValue;
            }
        }
    }

    private static string PromptStatus(string defaultValue)
    {
        while (true)
        {
            Console.Write($"Status ({string.Join("/", StatusOptions)}) [{defaultValue}]: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }

            return input;
        }
    }

    private static double PromptDouble(string label, double min, double max, double defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue.ToString(CultureInfo.InvariantCulture)}]: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }

            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine($"Enter a number between {min} and {max}.");
        }
    }

    private static double? PromptOptionalDouble(string label)
    {
        while (true)
        {
            Console.Write($"{label} (blank to omit): ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            Console.WriteLine("Enter a number or leave blank.");
        }
    }

    private static int PromptInt(string label, int defaultValue)
    {
        while (true)
        {
            Console.Write($"{label} [{defaultValue}]: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue;
            }

            if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0)
            {
                return value;
            }

            Console.WriteLine("Enter a positive integer.");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("TelemetrySender - send telemetry protobuf messages over WebSocket.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project TelemetrySender -- --url ws://localhost:5000/ws/telemetry --random --count 5 --interval-ms 500");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -u, --url           WebSocket URL (default: ws://localhost:5000/ws/telemetry)");
        Console.WriteLine("  -c, --count         Number of messages to send (default: 1)");
        Console.WriteLine("  -r, --random        Auto-generate readings instead of prompting");
        Console.WriteLine("  -i, --interval-ms   Delay between messages when random (default: 500)");
        Console.WriteLine("  -h, --help          Show help");
    }

    private sealed class Options
    {
        public string Url { get; set; }
        public int Count { get; set; } = 1;
        public bool? Randomize { get; set; }
        public int IntervalMs { get; set; }
        public bool ShowHelp { get; set; }

        public static Options Parse(string[] args, string defaultUrl)
        {
            var options = new Options { Url = defaultUrl };

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "-u":
                    case "--url":
                        options.Url = GetValue(args, ref i, "url");
                        break;
                    case "-c":
                    case "--count":
                        options.Count = ParseInt(GetValue(args, ref i, "count"), "count");
                        break;
                    case "-r":
                    case "--random":
                        options.Randomize = true;
                        break;
                    case "--manual":
                        options.Randomize = false;
                        break;
                    case "-i":
                    case "--interval-ms":
                        options.IntervalMs = ParseInt(GetValue(args, ref i, "interval-ms"), "interval-ms");
                        break;
                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;
                    default:
                        throw new ArgumentException($"Unknown argument: {arg}");
                }
            }

            return options;
        }

        private static string GetValue(string[] args, ref int index, string name)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {name}.");
            }

            index++;
            return args[index];
        }

        private static int ParseInt(string value, string name)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
            {
                throw new ArgumentException($"Invalid {name} value: {value}");
            }

            return parsed;
        }
    }
}
