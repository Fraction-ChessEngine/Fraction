using System;
using System.Threading;

namespace fraction;

public abstract class Search {
    private CancellationTokenSource cts = new();
    protected CancellationToken CancellationToken => cts.Token;

    public abstract long Depth { get; }
    public abstract long Nodes { get; }
    public abstract long Time { get; }

    public Thread? MainThread { get; private set; }

    public bool IsRunning => this.MainThread is not null;

    public event EventHandler<Heuristics>? NewHeuristics;

    protected virtual void OnNewHeuristics(Heuristics e) {
        this.NewHeuristics?.Invoke(this, e);
    }

    public event EventHandler<SearchResult>? Finished;

    protected virtual void OnFinished(SearchResult e) {
        this.Finished?.Invoke(this, e);
    }

    protected virtual void Run(object? args) => Run((SearchArgs) args!);

    protected abstract void Run(SearchArgs args);
    protected abstract void Reset();

    public void Start(SearchArgs args) {
        this.cts = new();
        this.MainThread = new(this.Run);
        this.MainThread.Name = "Main Search Thread";
        this.MainThread.IsBackground = true;
        this.MainThread.Priority = ThreadPriority.AboveNormal;
        this.MainThread.Start(args);
    }

    public void Stop() {
        if (!this.IsRunning) return;
        this.cts.Cancel();
        this.MainThread?.Join();
        this.MainThread = null;
        this.cts.Dispose();
        this.cts = new();
        this.Reset();
    }
}
