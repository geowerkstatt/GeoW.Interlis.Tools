namespace Geowerkstatt.Interlis.Compiler.CreateAST;

internal sealed class Scope<T> where T : class
{
    public T? Value { get; private set; }

    public Scope(T? initial = null)
    {
        Value = initial;
    }

    public Frame NewFrame(T? newValue)
    {
        return new Frame(this, newValue);
    }

    /// <summary>
    /// Sets the current value of the scope of the specified new value
    /// and resets the value when disposed.
    /// </summary>
    internal sealed class Frame : IDisposable
    {
        public T? previousValue;
        private Scope<T> scope;
        private bool isDisposed;

        public Frame(Scope<T> scope, T? newValue)
        {
            this.scope = scope;

            previousValue = scope.Value;
            scope.Value = newValue;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                scope.Value = previousValue;
                isDisposed = true;
            }
        }
    }
}
