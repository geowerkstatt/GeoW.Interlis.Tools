namespace Geowerkstatt.Interlis.Compiler.CreateAST;

/// <summary>
/// Encapsulates a generic value. The value can only be changed by calling <see cref="NewFrame(T?)"/>.
/// When the frame returned by <see cref="NewFrame(T?)"/> gets disposed, the value is reverted back to the value when the frame was created.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
internal sealed class Scope<T>
{
    public T? Value { get; private set; }

    public Scope(T? initial = default)
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
