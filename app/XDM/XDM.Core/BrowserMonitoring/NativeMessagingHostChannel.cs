using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XDM.Core.BrowserMonitoring
{
    public class NativeMessagingHostChannel : IDisposable
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;

        public event EventHandler<string> MessageReceived;
        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler Disconnected;

        public NativeMessagingHostChannel(NamedPipeServerStream pipeServer)
        {
            _pipeServer = pipeServer ?? throw new ArgumentNullException(nameof(pipeServer));
            _reader = new StreamReader(pipeServer, Encoding.UTF8);
            _writer = new StreamWriter(pipeServer, Encoding.UTF8) { AutoFlush = true };
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var message = await _reader.ReadLineAsync();
                    if (message == null)
                    {
                        OnDisconnected();
                        break;
                    }

                    OnMessageReceived(message);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(NativeMessagingHostChannel));
            }

            try
            {
                await _writer.WriteLineAsync(message);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }

        protected virtual void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        protected virtual void OnErrorOccurred(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _cancellationTokenSource.Cancel();
            _reader.Dispose();
            _writer.Dispose();
            _pipeServer.Dispose();
        }
    }
}
