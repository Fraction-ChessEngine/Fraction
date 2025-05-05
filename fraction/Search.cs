using System;
using System.Threading;

namespace fraction;

public abstract class Search {
    private CancellationTokenSource cts = null!;

    protected SearchResult? SearchResult { get; set; }
    protected CancellationToken CancellationToken => cts.Token;

    public Thread? MainThread { get; private set; }
    public bool IsRunning => this.MainThread is not null;

    public event EventHandler<SearchResult>? Finished;

    public abstract SearchHeuristics SearchHeuristics { get;}

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
        this.cts = null!;
        this.Reset();
    }
}
