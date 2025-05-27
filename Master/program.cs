// -------------------------
// Master.cs
// -------------------------
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Diagnostics;


class Master
{
    static ConcurrentDictionary<string, Dictionary<string, int>> aggregatedData = new();

    static void Main(string[] args) //entry point
    {

        // Set this process to use only CPU core 1
        Process currentProcess = Process.GetCurrentProcess();
        currentProcess.ProcessorAffinity = (IntPtr)0x4; // Core 2
        Console.WriteLine("Master is pinned to CPU core: 2")

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Master.exe <PipeName1> <PipeName2>");
            return;
        }

        string pipeName1 = args[0];
        string pipeName2 = args[1];

        //creates two thread for each pipe
        Thread t1 = new(() => ListenOnPipe(pipeName1));
        Thread t2 = new(() => ListenOnPipe(pipeName2));

        t1.Start();
        t2.Start();

        t1.Join();
        t2.Join();

        PrintResults();
    }

    static void ListenOnPipe(string pipeName)
    {
        try
        {
            using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In);
            Console.WriteLine($"[Master] Waiting for connection on pipe '{pipeName}'...");
            pipeServer.WaitForConnection();
            Console.WriteLine($"[Master] Connected to pipe '{pipeName}'.");

            using var reader = new StreamReader(pipeServer);
            string? line;
            //Master listens on pipe. Waits until a scanner connects.
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine($"[Master] Received: {line}");
                var parts = line.Split(':');
                if (parts.Length != 3) continue;

                //reads each line from scanner
                string file = parts[0];
                string word = parts[1];
                int count = int.TryParse(parts[2], out int c) ? c : 0;

                lock (aggregatedData)
                {
                    if (!aggregatedData.ContainsKey(file))
                        aggregatedData[file] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    if (!aggregatedData[file].ContainsKey(word))
                        aggregatedData[file][word] = 0;

                    aggregatedData[file][word] += count;
                }
            }

            Console.WriteLine($"[Master] Pipe '{pipeName}' closed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Master] Error on pipe '{pipeName}': {ex.Message}");
        }
    }

    static void PrintResults()
    {
        Console.WriteLine("\nFinal Aggregated Index:");
        foreach (var fileEntry in aggregatedData)
        {
            foreach (var wordEntry in fileEntry.Value)
            {
                Console.WriteLine($"{fileEntry.Key}:{wordEntry.Key}:{wordEntry.Value}");
            }
        }
    }
}
