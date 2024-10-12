How to use dotnet-trace?
------------------------

## Install

You are able to install dotnet-trace via the dotnet commandline tool, as its provided by NuGet:
```
dotnet tool install --global dotnet-trace
```

## Usage
First collect a trace with `dotnet-trace collect -- <program>` where program in our case is
somewhat like `./bin/{Debug,Release}/net8.0/fraction`. It'll tell you where the trace will be
collected in _(something along the lines of `fraction_date_time.nettrace`)_. To get a nice report
execute `dotnet-trace report <tracefile> topN`.

## Example Session
```
$ dotnet build -c Release
MSBuild version 17.8.5+b5265ef37 for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  fraction -> /home/m349pmma/source/repos/chessbot_mk2/bin/Release/net8.0/fraction.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.85
$ dotnet-trace collect -- ./bin/Release/net8.0/fraction
No profile or providers specified, defaulting to trace profile 'cpu-sampling'

Provider Name                           Keywords            Level               Enabled By
Microsoft-DotNETCore-SampleProfiler     0x0000F00000000000  Informational(4)    --profile
Microsoft-Windows-DotNETRuntime         0x00000014C14FCCBD  Informational(4)    --profile

Launching: ./bin/Release/net8.0/fraction
Process        : /home/m349pmma/source/repos/chessbot_mk2/bin/Release/net8.0/fraction
Output File    : /home/m349pmma/source/repos/chessbot_mk2/fraction_20241012_181527.nettrace

[00:00:00:10]   Recording trace 4.8282   (MB)
Press <Enter> or <Ctrl+C> to exit...

Trace completed.
Process exited with code '0'.
$ dotnet-trace report fraction_20241012_181527.nettrace topN -n 10
Top 10 Functions (Exclusive)                                                  Inclusive           Exclusive
1.  MoveGen.GenerateBoards(class fraction.Chessboard,bool)                    69.12%              48.06%
2.  Minimax.MiniMax(class fraction.Chessboard,int32,float32,float32,bool)     99.71%              30.1%
3.  MoveSets.getPseudoLegalMoves_bb(class fraction.Chessboard,int32,value     7.02%               6.94%
4.  MoveGen.GenerateMoves(class fraction.Chessboard,bool)                     15.23%              6.54%
5.  Chessboard.GenerateBoardWithMove(int32,int32,value class fraction.Piec    5.68%               5.67%
6.  Utility.FindSetBitsMax(unsigned int64,int32)                              1.01%               1.01%
7.  CastHelpers.StelemRef(class System.Array,int,class System.Object)         0.4%                0.4%
8.  MoveGen.GetVisionForPieceAt(class fraction.Chessboard,int32)              0.98%               0.4%
9.  Eval.BasicStaticEval(class fraction.Chessboard)                           0.5%                0.33%
10. Eval.RelativeValue(unsigned int64,value class fraction.Piece)             0.17%               0.17%
```

## Additional Resources
- [Official Documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
