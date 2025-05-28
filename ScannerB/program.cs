// -------------------------
// ScannerB.cs
// -------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;


class Program
{
    static ConcurrentQueue<string> fileResults = new ConcurrentQueue<string>();
    static AutoResetEvent dataReady = new AutoResetEvent(false);

    static void Main(string[] args) // starts the program
    {

        // Set this process to use only CPU core 1
        Process currentProcess = Process.GetCurrentProcess();
        currentProcess.ProcessorAffinity = (IntPtr)0x2; // Core 1
        Console.WriteLine("ScannerB is pinned to CPU core 1.");

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: ScannerB <directory path>");
            return;
        }

        SetProcessorAffinity(1); // Core 1

        string directoryPath = args[0];
        Thread readerThread = new Thread(() => ReadAndIndexFiles(directoryPath));
        Thread senderThread = new Thread(() => SendToMaster("agent2_pipe"));

        readerThread.Start();
        senderThread.Start();

        readerThread.Join();
        dataReady.Set(); // Notify sender when reading is done
        senderThread.Join();
    }

    static void SetProcessorAffinity(int core)
    {
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << core);
    }

    static void ReadAndIndexFiles(string dir)
    {
        foreach (var file in Directory.GetFiles(dir, "*.txt")) //reading & sending data
        {
            var wordCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadLines(file))
            {
                var words = line.Split(new[] { ' ', '.', ',', ';', ':', '-', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (wordCount.ContainsKey(word)) //Counts how many times each word appears.
                        wordCount[word]++;
                    else
                        wordCount[word] = 1;
                }
            }

            foreach (var kv in wordCount)
            {
                string line = $"{Path.GetFileName(file)}:{kv.Key}:{kv.Value}";
                fileResults.Enqueue(line);
            }
        }

        dataReady.Set();
    }

    static void SendToMaster(string pipeName)
    {    //Connects to the master through the named pipe.
        using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
        {
            Console.WriteLine("[ScannerB] Connecting to Master...");
            pipe.Connect();
            Console.WriteLine("[ScannerB] Connected.");

            using (StreamWriter writer = new StreamWriter(pipe, Encoding.UTF8))
            {
                writer.AutoFlush = true;
                while (true)
                {
                    dataReady.WaitOne(); // Wait for signal that data is ready

                    while (fileResults.TryDequeue(out string line))
                    {
                        writer.WriteLine(line);
                    }

                    if (fileResults.IsEmpty)
                        break;
                }
            }
        }

        Console.WriteLine("[ScannerB] Done sending data.");
    }
}
