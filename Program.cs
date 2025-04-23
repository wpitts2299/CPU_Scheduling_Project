using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CpuSchedulingSimulator
{
    public class Process
    {
        public int Id { get; set; }
        public double ArrivalTime { get; set; }
        public double BurstTime { get; set; }
        public int Priority { get; set; }
        public double RemainingTime { get; set; }
        public double CompletionTime { get; set; }
        public double WaitingTime { get; set; }
        public double TurnaroundTime { get; set; }
        public double ResponseTime { get; set; } = -1;
        public bool IsScheduled { get; set; }

        public Process(int id, double arrivalTime, double burstTime, int priority = 0)
        {
            Id = id;
            ArrivalTime = arrivalTime;
            BurstTime = burstTime;
            RemainingTime = burstTime;
            Priority = priority;
        }

        public Process Clone()
        {
            return new Process(Id, ArrivalTime, BurstTime, Priority)
            {
                RemainingTime = RemainingTime,
                CompletionTime = CompletionTime,
                WaitingTime = WaitingTime,
                TurnaroundTime = TurnaroundTime,
                ResponseTime = ResponseTime,
                IsScheduled = IsScheduled
            };
        }
    }

    public class Metrics
    {
        public string Name { get; set; }
        public double AverageWaitingTime { get; set; }
        public double AverageTurnaroundTime { get; set; }
        public double CpuUtilization { get; set; }
        public double Throughput { get; set; }
        public double AverageResponseTime { get; set; }

        public Metrics(List<Process> processes, double totalTime, string name)
        {
            Name = name;
            AverageWaitingTime = processes.Average(p => p.WaitingTime);
            AverageTurnaroundTime = processes.Average(p => p.TurnaroundTime);
            AverageResponseTime = processes.Average(p => p.ResponseTime);
            CpuUtilization = processes.Sum(p => p.BurstTime) / totalTime * 100;
            Throughput = processes.Count / totalTime;
        }

        public void Print()
        {
            Console.WriteLine($"{Name}:");
            Console.WriteLine($"  AWT: {AverageWaitingTime:F2}, ATT: {AverageTurnaroundTime:F2}, " +
                              $"CPU Util: {CpuUtilization:F2}%, Throughput: {Throughput:F4}, RT: {AverageResponseTime:F2}");
        }
    }

    public interface ISchedulingAlgorithm
    {
        string Name { get; }
        List<Process> Schedule(List<Process> processes, double quantum = 0);
        Metrics CalculateMetrics(List<Process> processes, double totalTime);
    }

    public class FCFSScheduler : ISchedulingAlgorithm
    {
        public string Name => "First Come, First Served";

        public List<Process> Schedule(List<Process> processes, double quantum = 0)
        {
            var completed = new List<Process>();
            double currentTime = 0;
            var sortedProcesses = processes.Select(p => p.Clone()).OrderBy(p => p.ArrivalTime).ToList();

            foreach (var process in sortedProcesses)
            {
                if (currentTime < process.ArrivalTime)
                    currentTime = process.ArrivalTime;

                process.ResponseTime = currentTime - process.ArrivalTime;
                currentTime += process.BurstTime;
                process.CompletionTime = currentTime;
                process.TurnaroundTime = currentTime - process.ArrivalTime;
                process.WaitingTime = process.TurnaroundTime - process.BurstTime;
                process.IsScheduled = true;
                completed.Add(process);
            }
            return completed;
        }

        public Metrics CalculateMetrics(List<Process> processes, double totalTime)
        {
            return new Metrics(processes, totalTime, Name);
        }
    }

    public class SJFScheduler : ISchedulingAlgorithm
    {
        public string Name => "Shortest Job First";

        public List<Process> Schedule(List<Process> processes, double quantum = 0)
        {
            var readyQueue = new List<Process>();
            var completed = new List<Process>();
            double currentTime = 0;
            var processList = processes.Select(p => p.Clone()).ToList();

            while (processList.Any() || readyQueue.Any())
            {
                readyQueue.AddRange(processList.Where(p => p.ArrivalTime <= currentTime && !p.IsScheduled));
                processList.RemoveAll(p => readyQueue.Contains(p));

                if (!readyQueue.Any())
                {
                    currentTime = processList.Any() ? processList.Min(p => p.ArrivalTime) : currentTime + 1;
                    continue;
                }

                var currentProcess = readyQueue.OrderBy(p => p.BurstTime).ThenBy(p => p.ArrivalTime).First();
                readyQueue.Remove(currentProcess);

                if (currentProcess.ResponseTime == -1)
                    currentProcess.ResponseTime = currentTime - currentProcess.ArrivalTime;

                currentTime += currentProcess.BurstTime;
                currentProcess.CompletionTime = currentTime;
                currentProcess.TurnaroundTime = currentTime - currentProcess.ArrivalTime;
                currentProcess.WaitingTime = currentProcess.TurnaroundTime - currentProcess.BurstTime;
                process.IsScheduled = true;
                completed.Add(currentProcess);
            }
            return completed;
        }

        public Metrics CalculateMetrics(List<Process> processes, double totalTime)
        {
            return new Metrics(processes, totalTime, Name);
        }
    }

    public class RRScheduler : ISchedulingAlgorithm
    {
        public string Name => "Round Robin";

        public List<Process> Schedule(List<Process> processes, double quantum)
        {
            var readyQueue = new Queue<Process>();
            var completed = new List<Process>();
            double currentTime = 0;
            var processList = processes.Select(p => p.Clone()).ToList();

            while (processList.Any() || readyQueue.Any())
            {
                readyQueue.EnqueueRange(processList.Where(p => p.ArrivalTime <= currentTime && !p.IsScheduled));
                processList.RemoveAll(p => readyQueue.Contains(p));

                if (!readyQueue.Any())
                {
                    currentTime = processList.Any() ? processList.Min(p => p.ArrivalTime) : currentTime + 1;
                    continue;
                }

                var currentProcess = readyQueue.Dequeue();
                if (currentProcess.ResponseTime == -1)
                    currentProcess.ResponseTime = currentTime - currentProcess.ArrivalTime;

                double timeSlice = Math.Min(quantum, currentProcess.RemainingTime);
                currentProcess.RemainingTime -= timeSlice;
                currentTime += timeSlice;

                if (currentProcess.RemainingTime <= 0)
                {
                    currentProcess.CompletionTime = currentTime;
                    currentProcess.TurnaroundTime = currentTime - currentProcess.ArrivalTime;
                    currentProcess.WaitingTime = currentProcess.TurnaroundTime - currentProcess.BurstTime;
                    currentProcess.IsScheduled = true;
                    completed.Add(currentProcess);
                }
                else
                {
                    readyQueue.Enqueue(currentProcess);
                }
            }
            return completed;
        }

        public Metrics CalculateMetrics(List<Process> processes, double totalTime)
        {
            return new Metrics(processes, totalTime, Name);
        }
    }

    public class PriorityScheduler : ISchedulingAlgorithm
    {
        public string Name => "Priority Scheduling";

        public List<Process> Schedule(List<Process> processes, double quantum = 0)
        {
            var readyQueue = new List<Process>();
            var completed = new List<Process>();
            double currentTime = 0;
            var processList = processes.Select(p => p.Clone()).ToList();

            while (processList.Any() || readyQueue.Any())
            {
                readyQueue.AddRange(processList.Where(p => p.ArrivalTime <= currentTime && !p.IsScheduled));
                processList.RemoveAll(p => readyQueue.Contains(p));

                if (!readyQueue.Any())
                {
                    currentTime = processList.Any() ? processList.Min(p => p.ArrivalTime) : currentTime + 1;
                    continue;
                }

                var currentProcess = readyQueue.OrderBy(p => p.Priority).ThenBy(p => p.ArrivalTime).First();
                readyQueue.Remove(currentProcess);

                if (currentProcess.ResponseTime == -1)
                    currentProcess.ResponseTime = currentTime - currentProcess.ArrivalTime;

                currentTime += currentProcess.BurstTime;
                currentProcess.CompletionTime = currentTime;
                currentProcess.TurnaroundTime = currentTime - currentProcess.ArrivalTime;
                currentProcess.WaitingTime = currentProcess.TurnaroundTime - currentProcess.BurstTime;
                currentProcess.IsScheduled = true;
                completed.Add(currentProcess);
            }
            return completed;
        }

        public Metrics CalculateMetrics(List<Process> processes, double totalTime)
        {
            return new Metrics(processes, totalTime, Name);
        }
    }

    public class SRTFScheduler : ISchedulingAlgorithm
    {
        public string Name => "Shortest Remaining Time First";

        public List<Process> Schedule(List<Process> processes, double quantum = 0)
        {
            var readyQueue = new List<Process>();
            var completed = new List<Process>();
            double currentTime = 0;
            var processList = processes.Select(p => p.Clone()).ToList();

            while (processList.Any() || readyQueue.Any())
            {
                readyQueue.AddRange(processList.Where(p => p.ArrivalTime <= currentTime && !p.IsScheduled));
                processList.RemoveAll(p => readyQueue.Contains(p));

                if (!readyQueue.Any())
                {
                    currentTime = processList.Any() ? processList.Min(p => p.ArrivalTime) : currentTime + 1;
                    continue;
                }

                var currentProcess = readyQueue.OrderBy(p => p.RemainingTime).First();
                readyQueue.Remove(currentProcess);

                if (currentProcess.ResponseTime == -1)
                    currentProcess.ResponseTime = currentTime - currentProcess.ArrivalTime;

                double timeSlice = 1;
                currentProcess.RemainingTime -= timeSlice;
                currentTime += timeSlice;

                if (currentProcess.RemainingTime <= 0)
                {
                    currentProcess.CompletionTime = currentTime;
                    currentProcess.TurnaroundTime = currentTime - currentProcess.ArrivalTime;
                    currentProcess.WaitingTime = currentProcess.TurnaroundTime - currentProcess.BurstTime;
                    currentProcess.IsScheduled = true;
                    completed.Add(currentProcess);
                }
                else
                {
                    readyQueue.Add(currentProcess);
                }
            }
            return completed;
        }

        public Metrics CalculateMetrics(List<Process> processes, double totalTime)
        {
            return new Metrics(processes, totalTime, Name);
        }
    }

    public class MLFQScheduler : ISchedulingAlgorithm
    {
        public string Name => "Multi-Level Feedback Queue";

        public List<Process> Schedule(List<Process> processes, double quantum = 8)
        {
            var queues = new List<Queue<Process>> { new Queue<Process>(), new Queue<Process>(), new Queue<Process>() };
            var quanta = new double[] { quantum, quantum * 2, double.MaxValue };
            var completed = new List<Process>();
            double currentTime = 0;
            var processList = processes.Select(p => p.Clone()).ToList();

            while (processList.Any() || queues.Any(q => q.Any()))
            {
                var arrived = processList.Where(p => p.ArrivalTime <= currentTime && !p.IsScheduled).ToList();
                foreach (var p in arrived) queues[0].Enqueue(p);
                processList.RemoveAll(p => arrived.Contains(p));

                int currentQueue = -1;
                for (int i = 0; i < queues.Count; i++)
                    if (queues[i].Any()) { currentQueue = i; break; }

                if (currentQueue == -1)
                {
                    currentTime = processList.Any() ? processList.Min(p => p.ArrivalTime) : currentTime + 1;
                    continue;
                }

                var currentProcess = queues[currentQueue].Dequeue();
                if (currentProcess.ResponseTime == -1)
                    currentProcess.ResponseTime = currentTime - currentProcess.ArrivalTime;

                double timeSlice = Math.Min(quanta[currentQueue], currentProcess.RemainingTime);
                currentProcess.RemainingTime -= timeSlice;
                currentTime += timeSlice;

                if (currentProcess.RemainingTime <= 0)
                {
                    currentProcess.CompletionTime = currentTime;
                    currentProcess.TurnaroundTime = currentTime - currentProcess.ArrivalTime;
                    currentProcess.WaitingTime = currentProcess.TurnaroundTime - currentProcess.BurstTime;
                    currentProcess.IsScheduled = true;
                    completed.Add(currentProcess);
                }
                else
                {
                    int nextQueue = Math.Min(currentQueue + 1, queues.Count - 1);
                    queues[nextQueue].Enqueue(currentProcess);
                }
            }
            return completed;
        }

        public Metrics CalculateMetrics(List<Process> processes, double totalTime)
        {
            return new Metrics(processes, totalTime, Name);
        }
    }

    public static class QueueExtensions
    {
        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> items)
        {
            foreach (var item in items) queue.Enqueue(item);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var schedulers = new List<ISchedulingAlgorithm>
                {
                    new FCFSScheduler(),
                    new SJFScheduler(),
                    new RRScheduler(),
                    new PriorityScheduler(),
                    new SRTFScheduler(),
                    new MLFQScheduler()
                };
                double quantum = 4;
                var random = new Random(42);

                // Larger Scale Test
                Console.WriteLine("=== Larger Scale Test (50 Processes) ===\n");
                var largeProcesses = GenerateRandomProcesses(50, random);
                var largeScaleResults = new List<(string Name, Metrics Metrics)>();
                foreach (var scheduler in schedulers)
                {
                    var scheduledProcesses = scheduler.Schedule(largeProcesses, quantum);
                    double totalTime = scheduledProcesses.Max(p => p.CompletionTime);
                    var metrics = scheduler.CalculateMetrics(scheduledProcesses, totalTime);
                    metrics.Print();
                    largeScaleResults.Add((scheduler.Name, metrics));
                    Console.WriteLine();
                }
                SaveMetricsToCsv(largeScaleResults, "large_scale_metrics.csv");
                AnalyzeMetrics(largeScaleResults, "Large Scale Test");

                // Edge Case 1
                Console.WriteLine("\n=== Edge Case 1: All Arrive at Time 0, Identical Burst Times (10 Processes) ===\n");
                var edgeCase1Processes = GenerateEdgeCase1(10);
                var edgeCase1Results = new List<(string Name, Metrics Metrics)>();
                foreach (var scheduler in schedulers)
                {
                    var scheduledProcesses = scheduler.Schedule(edgeCase1Processes, quantum);
                    double totalTime = scheduledProcesses.Max(p => p.CompletionTime);
                    var metrics = scheduler.CalculateMetrics(scheduledProcesses, totalTime);
                    metrics.Print();
                    edgeCase1Results.Add((scheduler.Name, metrics));
                    Console.WriteLine();
                }
                SaveMetricsToCsv(edgeCase1Results, "edge_case1_metrics.csv");

                // Edge Case 2
                Console.WriteLine("\n=== Edge Case 2: Long and Short Burst Times (20 Processes) ===\n");
                var edgeCase2Processes = GenerateEdgeCase2(20, random);
                var edgeCase2Results = new List<(string Name, Metrics Metrics)>();
                foreach (var scheduler in schedulers)
                {
                    var scheduledProcesses = scheduler.Schedule(edgeCase2Processes, quantum);
                    double totalTime = scheduledProcesses.Max(p => p.CompletionTime);
                    var metrics = scheduler.CalculateMetrics(scheduledProcesses, totalTime);
                    metrics.Print();
                    edgeCase2Results.Add((scheduler.Name, metrics));
                    Console.WriteLine();
                }
                SaveMetricsToCsv(edgeCase2Results, "edge_case2_metrics.csv");

                // Edge Case 3
                Console.WriteLine("\n=== Edge Case 3: Wide Range of Priorities (20 Processes) ===\n");
                var edgeCase3Processes = GenerateEdgeCase3(20, random);
                var edgeCase3Results = new List<(string Name, Metrics Metrics)>();
                foreach (var scheduler in schedulers)
                {
                    var scheduledProcesses = scheduler.Schedule(edgeCase3Processes, quantum);
                    double totalTime = scheduledProcesses.Max(p => p.CompletionTime);
                    var metrics = scheduler.CalculateMetrics(scheduledProcesses, totalTime);
                    metrics.Print();
                    edgeCase3Results.Add((scheduler.Name, metrics));
                    Console.WriteLine();
                }
                SaveMetricsToCsv(edgeCase3Results, "edge_case3_metrics.csv");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static List<Process> GenerateRandomProcesses(int count, Random random)
        {
            var processes = new List<Process>();
            for (int i = 1; i <= count; i++)
            {
                double arrivalTime = random.Next(0, 101);
                double burstTime = random.Next(1, 21);
                int priority = random.Next(1, 11);
                processes.Add(new Process(i, arrivalTime, burstTime, priority));
            }
            return processes.OrderBy(p => p.ArrivalTime).ToList();
        }

        static List<Process> GenerateEdgeCase1(int count)
        {
            var processes = new List<Process>();
            for (int i = 1; i <= count; i++)
            {
                processes.Add(new Process(i, 0, 10, new Random().Next(1, 11)));
            }
            return processes;
        }

        static List<Process> GenerateEdgeCase2(int count, Random random)
        {
            var processes = new List<Process>();
            for (int i = 1; i <= count; i++)
            {
                double arrivalTime = random.Next(0, 51);
                double burstTime = random.Next(0, 2) == 0 ? random.Next(1, 6) : random.Next(50, 101);
                int priority = random.Next(1, 11);
                processes.Add(new Process(i, arrivalTime, burstTime, priority));
            }
            return processes.OrderBy(p => p.ArrivalTime).ToList();
        }

        static List<Process> GenerateEdgeCase3(int count, Random random)
        {
            var processes = new List<Process>();
            for (int i = 1; i <= count; i++)
            {
                double arrivalTime = random.Next(0, 51);
                double burstTime = random.Next(1, 21);
                int priority = random.Next(1, 101);
                processes.Add(new Process(i, arrivalTime, burstTime, priority));
            }
            return processes.OrderBy(p => p.ArrivalTime).ToList();
        }

        static void AnalyzeMetrics(List<(string Name, Metrics Metrics)> results, string testName)
        {
            Console.WriteLine($"\n=== Analysis for {testName} ===\n");

            var awtValues = results.Select(r => r.Metrics.AverageWaitingTime).ToList();
            var attValues = results.Select(r => r.Metrics.AverageTurnaroundTime).ToList();

            double awtMean = awtValues.Average();
            double awtVariance = awtValues.Sum(v => Math.Pow(v - awtMean, 2)) / awtValues.Count;
            double attMean = attValues.Average();
            double attVariance = attValues.Sum(v => Math.Pow(v - attMean, 2)) / attValues.Count;

            Console.WriteLine($"AWT Variance: {awtVariance:F2}");
            Console.WriteLine($"ATT Variance: {attVariance:F2}");

            double awtStdDev = Math.Sqrt(awtVariance);
            double attStdDev = Math.Sqrt(attVariance);

            foreach (var (name, metrics) in results)
            {
                if (Math.Abs(metrics.AverageWaitingTime - awtMean) > 2 * awtStdDev)
                    Console.WriteLine($"Anomaly: {name} has unusually high/low AWT ({metrics.AverageWaitingTime:F2})");
                if (Math.Abs(metrics.AverageTurnaroundTime - attMean) > 2 * attStdDev)
                    Console.WriteLine($"Anomaly: {name} has unusually high/low ATT ({metrics.AverageTurnaroundTime:F2})");
            }
        }

        static void SaveMetricsToCsv(List<(string Name, Metrics Metrics)> results, string filePath)
        {
            try
            {
                var lines = new List<string> { "Algorithm,AWT,ATT,CpuUtil,Throughput,ResponseTime" };
                foreach (var (name, metrics) in results)
                {
                    lines.Add($"{name},{metrics.AverageWaitingTime:F2},{metrics.AverageTurnaroundTime:F2}," +
                              $"{metrics.CpuUtilization:F2},{metrics.Throughput:F4},{metrics.AverageResponseTime:F2}");
                }
                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving to CSV: {ex.Message}");
            }
        }
    }
}