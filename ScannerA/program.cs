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

    static void Main(string[] args)
    {

        // Set this process to use only CPU core 0
        Process currentProcess = Process.GetCurrentProcess();
        currentProcess.ProcessorAffinity = (IntPtr)0x1; // Core 0
        Console.WriteLine("ScannerA is pinned to CPU core 0.");

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: ScannerA <directory path>");
            return;
        }

        SetProcessorAffinity(0); // Core 0

        string directoryPath = args[0];
        Thread readerThread = new Thread(() => ReadAndIndexFiles(directoryPath));
        Thread senderThread = new Thread(() => SendToMaster("agent1_pipe"));

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
        foreach (var file in Directory.GetFiles(dir, "*.txt"))
        {
            Console.WriteLine($"[ScannerA] Found file: {file}");
            var content = File.ReadAllText(file);

            var wordCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadLines(file))
            {
                var words = line.Split(new[] { ' ', '.', ',', ';', ':', '-', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (wordCount.ContainsKey(word))
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
    {
        using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
        {
            Console.WriteLine("[ScannerA] Connecting to Master...");
            pipe.Connect();
            Console.WriteLine("[ScannerA] Connected.");

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

        Console.WriteLine("[ScannerA] Done sending data.");
    }
}
