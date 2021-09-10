# .NET Core port of chia plotter

## What is this?

chiapos-dotnet is custom port of official chia plotter (chiapos) to .NET Core. This is cross-platform solution which supports Mac/x64, Linux/x64 and Windows/x64.

## Why?

There are several ideas lays behind why I started this project. The first is reseach in performance view: it's very interesting how .NET5 and .NET6 work in comparison with C++ on real project which is widely used but not on artificially created performace tests. The second: I'd like to perform code/algorithms optimizations and see their improvements. Final stage (if get success in the first two phases) will be to increase plot build speed compared to other market solutions.  

## Is this pure C#?

No, it's not. The project is not tight to C# only. If I find that some piece of code can't be executed on .NET as fast as on other platforms I will possible change this piece of code back to more effective C/C++ or whatever.
Currently only Blake3 algorithm is implemented in Rust/C/Asm and used as a library, all other code is C#.

## What is current state?

Current state: porting is completed, currently verifying the correctness. k=25 plot generation is verified, next step to verify k=32 plot generation.

## Performance metrics

Ubuntu Linux 20.04, Intel Xeon Gold 6208U, 384GB RAM,
RAM disk is used for tmp1/tmp2 and final dirs.

#### k=25, r=32 Number of Threads: 32

Time in seconds

| Phase   |    master   | original  | ProofOfSpace  |
|---------|-------------|-----------|---------------|
| F1      |    0.515    |    0.546  |    2.508      |
| Phase1  |  146        |  226      |   56.7        |
| Phase2  |   20        |  205      |   10.5        |
| Phase3  |  106        |  107      |   35.4        |
| Phase4  |    8.53     |    8.66   |    2.1        |
| Total   |  280        |  548      |  104.7        |

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
 
