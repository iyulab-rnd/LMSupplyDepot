﻿using LMSupplyDepots.Tools.HuggingFace.Client;
using LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog for file logging
var logPath = Path.Combine("logs/sample.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        logPath,
        shared: false,
        rollOnFileSizeLimit: false,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Create LoggerFactory with file logging only
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog(Log.Logger)
           .SetMinimumLevel(LogLevel.Information);
});

Console.WriteLine("Hugging Face API Sample Console Application");
Console.WriteLine("==========================================");

// Hugging Face API 토큰 입력 (선택사항)
Console.Write("Enter your Hugging Face API token (optional, press Enter to skip): ");
var token = Console.ReadLine();

// 클라이언트 설정
var options = new HuggingFaceClientOptions
{
    MaxConcurrentDownloads = 3,
    ProgressUpdateInterval = 100,
    MaxRetries = 3
};

// 토큰이 입력된 경우에만 설정
if (!string.IsNullOrWhiteSpace(token))
{
    options.Token = token;
    Console.WriteLine("API token set successfully.");
}
else
{
    Console.WriteLine("No API token provided. Some operations may be limited.");
}

using var client = new HuggingFaceClient(options, loggerFactory);

while (true)
{
    Console.WriteLine("\nSelect an operation:");
    Console.WriteLine("1. Search Models");
    Console.WriteLine("2. Download Model");
    Console.WriteLine("0. Exit");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await SampleSearchModels.RunAsync(client);
                break;
            case "2":
                await SampleDownloadModel.RunAsync(client);
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Invalid choice. Please try again.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}