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
    static ManualResetEventSlim dataReady = new ManualResetEventSlim(false);
    static volatile bool readingDone = false; // signals reading finished

    static void Main(string[] args)
    {
        Process currentProcess = Process.GetCurrentProcess();
        currentProcess.ProcessorAffinity = (IntPtr)0x2; // Core 1
        Console.WriteLine("ScannerB is pinned to CPU core 1.");

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: ScannerB <directory path>");
            return;
        }

        string directoryPath = args[0];
        Thread readerThread = new Thread(() => ReadAndIndexFiles(directoryPath));
        Thread senderThread = new Thread(() => SendToMaster("agent2_pipe"));

        readerThread.Start();
        senderThread.Start();

        readerThread.Join();

        // Signal reading is done
        readingDone = true;
        dataReady.Set(); // wake up sender if waiting

        senderThread.Join();
    }

    static void ReadAndIndexFiles(string dir)
    {
        foreach (var file in Directory.GetFiles(dir, "*.txt"))
        {
            Console.WriteLine($"[ScannerA] Found file: {file}");
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

            // Signal sender thread new data is ready
            dataReady.Set();
        }
    }

    static void SendToMaster(string pipeName)
    {
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
                    dataReady.Wait(); // wait for data ready signal
                    dataReady.Reset(); // reset signal

                    while (fileResults.TryDequeue(out var line))
                    {
                        writer.WriteLine(line);
                    }

                    // Exit if reading is done and queue is empty
                    if (readingDone && fileResults.IsEmpty)
                        break;
                }
            }
        }

        Console.WriteLine("[ScannerB] Done sending data.");
    }
}
