using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageHelper.Utility
{
    public class Debouncer : IDisposable
    {
        private readonly TimeSpan _time;
        private CancellationTokenSource _cts;
        private readonly object _lock = new object();   

        public Debouncer(TimeSpan time)
        {
            _time = time;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts?.Dispose();
            }
        }

        public void Run(Func<CancellationToken, Task> action, Action<Exception> onError = null)
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts?.Dispose();

                _cts = new();
            }

            var token = _cts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_time, token);

                    await action(token);
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    onError(ex);
                }
            }, token);
        }
    }
}
