# .NET Core port of chia plotter

## What is this?

chiapos-dotnet is custom port of official chia plotter (chiapos) to .NET Core. This is cross-platform solution which supports Mac/x64, Linux/x64 and Windows/x64.

## Why?

There are several ideas lays behind why I started this project. The first is reseach in performance view: it's very interesting how .NET5 and .NET6 work in comparison with C++ on real project which is widely used and not on artificially created performace tests. The second: I'd like to perform code/algorithms optimizations and see their improvements. Final stage (if get success in the first two phases) will be to increase plot build speed compared to other market solutions.  

## Is this pure C#?

No, it's not. The project is not tight to C# only. If I find that some piece of code can't be executed on .NET as fast as on other platforms I will possible change this piece of code back to more effective C/C++ or whatever.
Currently only Blake3 algorithm is implemented in Rust and used as a library, all other code is C#.

## What is current state?

Current state: porting in progress. Phase1 of plotter is ported, but not fully verified, other phases are ported but not verified at all.

## Performance metrics

Currently the project does not have any performance metrics, because it's not ported completely.

## How to Build

TBD.

## Roadmap

   * [x] Translate C++ code to C#
   * [ ] Debug code and verify correctness of generated plots via ProofOfSpace
   * [ ] Make performance metrics
   * [ ] Write as much tests as possible before start next step
   * [ ] Make code/algorithm optimizations and compare results with previous versions
 