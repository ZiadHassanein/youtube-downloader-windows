namespace YouTubeDownloader;
public enum OperationState
{
    Idle,
    Preparing,
    Inspecting,
    Downloading,
    Updating,
    Cancelling
}

public sealed class OperationCoordinator
{
    private readonly object gate = new();
    private OperationLease current;
    private long sequence;
    public event Action Changed;
    public OperationState State { get; private set; }

    public bool IsBusy
    {
        get
        {
            lock (gate)
                return current != null;
        }
    }

    public long CurrentId
    {
        get
        {
            lock (gate)
                return current?.Id ?? 0;
        }
    }

    public OperationLease Begin(OperationState state, CancellationToken token)
    {
        OperationLease lease;
        lock (gate)
        {
            if (current != null)
                throw new InvalidOperationException("Another operation is already running.");
            current = lease = new OperationLease(this, ++sequence, token);
            State = state;
        }

        Changed?.Invoke();
        return lease;
    }

    public void SetState(long id, OperationState state)
    {
        lock (gate)
        {
            if (current?.Id != id || State == OperationState.Cancelling)
                return;
            State = state;
        }

        Changed?.Invoke();
    }

    public void Cancel()
    {
        OperationLease lease;
        lock (gate)
        {
            lease = current;
            if (lease == null)
                return;
            State = OperationState.Cancelling;
        }

        Changed?.Invoke();
        lease.Cancel();
    }

    public Task WaitForIdleAsync()
    {
        lock (gate)
            return current?.Completion ?? Task.CompletedTask;
    }

    private void End(OperationLease lease)
    {
        lock (gate)
        {
            if (current != lease)
                return;
            current = null;
            State = OperationState.Idle;
        }

        lease.Finish();
        Changed?.Invoke();
    }

    public sealed class OperationLease : IDisposable
    {
        private readonly OperationCoordinator owner;
        private readonly CancellationTokenSource cancellation;
        private readonly TaskCompletionSource done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public long Id { get; }
        public CancellationToken Token => cancellation.Token;
        public Task Completion => done.Task;

        internal OperationLease(OperationCoordinator owner, long id, CancellationToken token)
        {
            this.owner = owner;
            Id = id;
            cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        }

        internal void Cancel()
        {
            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        internal void Finish()
        {
            cancellation.Dispose();
            done.TrySetResult();
        }

        public void Dispose() => owner.End(this);
    }
}
