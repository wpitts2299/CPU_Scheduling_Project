# CS 3502 Project 2: CPU Scheduling Simulator

## Overview
This project implements a CPU Scheduling Simulator for the CS 3502 course. The simulator compares the performance of six scheduling algorithms: First Come, First Served (FCFS), Shortest Job First (SJF), Round Robin (RR), Priority Scheduling, Shortest Remaining Time First (SRTF), and Multi-Level Feedback Queue (MLFQ). The simulator evaluates these algorithms using metrics such as Average Waiting Time (AWT), Average Turnaround Time (ATT), CPU Utilization, Throughput, and Response Time (RT).

The project includes:
- A C# console application to simulate the scheduling algorithms.
- Tests for various scenarios: an initial test with 4 processes, a larger scale test with 50 processes, and three edge cases.
- A LaTeX report (`report.pdf`) summarizing the implementation, test results, and analysis.
- Charts generated directly in LaTeX using `pgfplots` to visualize AWT and ATT for the large scale test.

## Requirements
### For Running the Simulator
- **.NET Core SDK** (version 6.0 or later recommended)
  - Install from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

### For Compiling the LaTeX Report
- **LaTeX Distribution** (e.g., MiKTeX, TeX Live, or use Overleaf)
  - Overleaf: [https://www.overleaf.com/](https://www.overleaf.com/)
  - MiKTeX: [https://miktex.org/](https://miktex.org/)
  - TeX Live: [https://www.tug.org/texlive/](https://www.tug.org/texlive/)
- Ensure the `pgfplots` package is available (included by default in most LaTeX distributions)

## Setup and Running the Simulator
