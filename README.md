# 🔍 Distributed File Indexer using Named Pipes and CPU Core Affinity

This project implements a distributed file indexing system in C# using:
- 🔄 Named pipes for inter-process communication
- 🧵 Multithreading for concurrent scanning
- 🧠 CPU core affinity to assign processes to specific CPU cores

## 📌 Features
- 🧠 CPU core pinning to distribute load effectively
- 🔄 Asynchronous communication using named pipes
- 🧵 Multi-threaded file scanning and word indexing
- 📄 Real-time aggregation and summary by the master node
- 🗂️ Modular project structure for scalability
  
## 📁 Project Structure
```
FileIndexer/
├── Master/ # Central aggregator process
├── ScannerA/ # Agent 1: Scans directory A
├── ScannerB/ # Agent 2: Scans directory B
└── README.md
```

## 🧠 How It Works
- `ScannerA` and `ScannerB` each scan a directory for `.txt` files and index word frequencies.
- Both scanners connect to the `Master` via **named pipes** and send their word counts.
- The `Master` listens to two named pipes (`agent1_pipe`, `agent2_pipe`) and aggregates all word data.
- Each process is pinned to a **specific CPU core** using `Process.ProcessorAffinity`.

---

## 📦 Requirements
- .NET SDK 8.0+
- Windows OS (due to `ProcessorAffinity` and Named Pipes support)
- Basic understanding of terminal/PowerShell usage

## 🛠️ Installation & Usage

1. **Clone the repository**
   ```bash
   git clone https://github.com/joelchirayath/FileIndexer.git
   cd FileIndexer

## 🚀 Running the Project

1. **Build the solution** in Visual Studio or with the command:
   ```bash
   dotnet build
2. Run Master(wait for agents)
   ```bash
   Master.exe agent1_pipe agent2_pipe
4. Run ScannerA in a new terminal:
   ```bash
     ScannerA.exe "Your/TextFile/path/folder"
6. Run ScannerB in a new terminal:
   ```bash
   ScannerB.exe "Your/TextFiles/path/folder"
   
🧬 Sample Output (From Master)
```
Master is pinned to CPU core: 2
[Master] Waiting for connection on pipe 'agent1_pipe'...
[Master] Waiting for connection on pipe 'agent2_pipe'...

[Master] Connected to pipe 'agent1_pipe'.
[Master] Received: ............
[Master] Pipe 'agent1_pipe' closed.

[Master] Connected to pipe 'agent2_pipe'.
[Master] Received:........
[Master] Pipe 'agent2_pipe' closed.
```
```
Final Aggregated Index:
file1.txt:..........
file2.txt:..........
```
---

## 🧠 Key Concepts

### 🪝 Named Pipes
Allows communication between Master and Scanners.
Each scanner sends strings like filename:word:count through its pipe.

### 🧵 Multithreading
Each scanner scans files and counts words concurrently.

### 🧠 CPU Core Affinity
 Each process is pinned to a separate core for true multicore parallelism


## 👨‍🏫 Authors
Developed by Joel Chirayath as part of Object-Oriented Programming (Vilnius University, Semester 2).

## 📬 Contact
For questions, feedback, or collaboration opportunities, feel free to reach out via:
- 📧 joelchirayath@gmail.com
- 🌐 [LinkedIn Profile](https://www.linkedin.com/in/joel-chirayath-5650432b8/)
