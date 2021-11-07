# .NET Core port of chia plotter

## What is this?

chiapos-dotnet is custom port of official chia plotter (chiapos) to .NET Core. This is cross-platform solution which supports Mac/x64, Linux/x64 and Windows/x64.

## Why?

There are several ideas lays behind why I started this project. The first is reseach in performance view: it's very interesting how .NET5 and .NET6 work in comparison with C++ on real project which is widely used but not on artificially created performace tests. The second: I'd like to perform code/algorithms optimizations and see their improvements. Final stage (if get success in the first two phases) will be to increase plot build speed compared to other market solutions.  

## Is this pure C#?

No, it's not. The project is not tight to C# only. If I find that some piece of code can't be executed on .NET as fast as on other platforms I will possible change this piece of code back to more effective C/C++ or whatever.
Currently only Blake3 algorithm is implemented in Rust/C/Asm and used as a library, all other code is C#.

## What is current state?

Current state: porting is completed. Plot generation for k=25 and k=32 plot generation is verified. At now: time of plot generation optimization.

## Performance metrics

Ubuntu Linux 20.04, Intel Xeon Gold 6208U, 384GB RAM,
RAM disk is used for tmp1/tmp2 and final dirs.

Time in seconds.

#### k=25, r=32 Number of Threads: 32

| Phase   |    master   | original  | ProofOfSpace  |
|---------|-------------|-----------|---------------|
| F1      |    0.497    |    0.546  |    2.508      |
| Phase1  |  119        |  226      |   56.7        |
| Phase2  |   20        |  205      |   10.5        |
| Phase3  |   84        |  107      |   35.4        |
| Phase4  |    8.84     |    8.66   |    2.1        |
| Total   |  234        |  548      |  104.7        |

#### k=32, r=32 Number of Threads: 32

| Phase   |    master   | commit 48960 | ProofOfSpace  | MadMaxPlotter |
|---------|-------------|--------------|---------------|---------------|
| F1      |       78    |       78     |      312      |      11       |
| Phase1  |     28891   |    28891     |     5233      |     634       |
| Phase2  |      5617   |     5617     |     3975      |     333       | 
| Phase3  |     22108   |    22108     |     7419      |     319       |
| Phase4  |      1812   |     1812     |      524      |      49       |
| Total   |     58430   |    58430     |    17135      |    1345       |

Legend:
   - master   - current development in `master` branch
   - original - original port from C++ ProofOfSpace to .NET (`original` branch)
   - ProofOfSpace - official chiapos plot generator

## How to Build

    git clone https://github.com/xplicit/chiapos-dotnet
    cd chiapos-dotnet
    git submodule update --init --recursive
    
Windows:

    dotnet build -c Release /p:Platform=x64 chiapos-dotnet.sln

Linux:
    
    dotnet build -c ReleaseLinux /p:Platform=x64 chiapos-dotnet.sln

Mac:

    dotnet build -c ReleaseMac /p:Platform=x64 chiapos-dotnet.sln

## Roadmap

   * [x] Translate C++ code to C#
   * [x] Debug code and verify correctness of generated plots via ProofOfSpace
   * [x] Make performance metrics
   * [ ] Write as much tests as possible before start next step
   * [ ] Make code/algorithm optimizations and compare results with previous versions
 
