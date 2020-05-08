using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Tls;

namespace SkunkLab.Channels.Tcp
{
    public class TcpServerChannel2 : TcpChannel
    {
        #region private methods

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslpolicyerrors)
        {
            if (sslpolicyerrors != SslPolicyErrors.None)
            {
                return false;
            }

            if (certificate == null)
            {
                return false;
            }

            X509Certificate2 cert = new X509Certificate2(certificate);
            return cert.NotBefore < DateTime.Now && cert.NotAfter > DateTime.Now;
        }

        #endregion private methods

        #region ctor

        public TcpServerChannel2(TcpClient client, int blockSize = 0x4000, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            this.client = client;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.token.Register(async () => await CloseAsync());
            Id = "tcp2-" + Guid.NewGuid();
            Port = ((IPEndPoint)client.Client.LocalEndPoint).Port;
            queue = new Queue<byte[]>();
        }

        public TcpServerChannel2(TcpClient client, X509Certificate2 certificate, bool clientAuth,
            int blockSize = 0x4000, int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            this.client = client;
            this.certificate = certificate;
            this.clientAuth = clientAuth;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.token.Register(async () => await CloseAsync());
            Id = "tcp2-" + Guid.NewGuid();
            Port = ((IPEndPoint)client.Client.LocalEndPoint).Port;
            queue = new Queue<byte[]>();
        }

        public TcpServerChannel2(TcpClient client, TlsPskIdentityManager pskManager, int blockSize = 0x4000,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            this.client = client;
            this.pskManager = pskManager;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.token.Register(async () => await CloseAsync());
            Id = "tcp2-" + Guid.NewGuid();
            Port = ((IPEndPoint)client.Client.LocalEndPoint).Port;
            queue = new Queue<byte[]>();
        }

        #endregion ctor

        #region private member variables

        private readonly int blockSize;

        private readonly X509Certificate2 certificate;

        private readonly bool clientAuth;

        private readonly int maxBufferSize;

        private readonly TlsPskIdentityManager pskManager;

        private readonly Queue<byte[]> queue;

        private readonly CancellationToken token;

        private TcpClient client;

        private bool disposed;

        private NetworkStream localStream;

        private TlsServerProtocol protocol;

        private SemaphoreSlim readConnection;

        private ChannelState state;

        private Stream stream;

        private SemaphoreSlim writeConnection;

        #endregion private member variables

        #region events

        public override event EventHandler<ChannelCloseEventArgs> OnClose;

        public override event EventHandler<ChannelErrorEventArgs> OnError;

        public override event EventHandler<ChannelOpenEventArgs> OnOpen;

        public override event EventHandler<ChannelReceivedEventArgs> OnReceive;

        public override event EventHandler<ChannelStateEventArgs> OnStateChange;

        #endregion events

        #region Properties

        public override string Id
        {
            get; internal set;
        }

        public override bool IsAuthenticated
        {
            get; internal set;
        }

        public override bool IsConnected => State == ChannelState.Open;

        public override bool IsEncrypted
        {
            get; internal set;
        }

        public override int Port
        {
            get; internal set;
        }

        public override bool RequireBlocking => pskManager != null;

        public override ChannelState State
        {
            get => state;

            internal set
            {
                if (value != state)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }

                state = value;
            }
        }

        public override string TypeId => "TCP2";

        #endregion Properties

        #region methods

        public override async Task AddMessageAsync(byte[] message)
        {
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, message));
            await Task.CompletedTask;
        }

        public override async Task CloseAsync()
        {
            if (State == ChannelState.Closed || State == ChannelState.ClosedReceived)
            {
                Console.WriteLine("TCP channel 2 is already closed...returning");
                return;
            }

            State = ChannelState.ClosedReceived;

            try
            {
                if (protocol != null)
                {
                    protocol.Close();
                }
            }
            catch { }

            protocol = null;

            if (client != null && client.Client != null && client.Client.Connected &&
                client.Client.Poll(10, SelectMode.SelectRead))
            {
                if (client.Client.UseOnlyOverlappedIO)
                {
                    client.Client.DuplicateAndClose(Process.GetCurrentProcess().Id);
                }
                else
                {
                    client.Close();
                }
            }

            client = null;

            if (readConnection != null)
            {
                readConnection.Dispose();
            }

            if (writeConnection != null)
            {
                writeConnection.Dispose();
            }

            State = ChannelState.Closed;
            OnClose?.Invoke(this, new ChannelCloseEventArgs(Id));

            await Task.CompletedTask;
        }

        public override async Task OpenAsync()
        {
            State = ChannelState.Connecting;

            readConnection = new SemaphoreSlim(1);
            writeConnection = new SemaphoreSlim(1);

            try
            {
                localStream = client.GetStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fault opening TCP Channel 2  - {0}", ex.Message);
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }

            if (pskManager != null)
            {
                try
                {
                    protocol = TlsClientUtil.ConnectPskTlsServer(pskManager, localStream);
                    stream = protocol.Stream;
                    IsEncrypted = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fault opening TLS connection {0}", ex.Message);
                    State = ChannelState.Aborted;
                    Trace.TraceError(ex.Message);
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                    return;
                }
            }
            else if (certificate != null)
            {
                try
                {
                    stream = new SslStream(localStream, true, ValidateCertificate);
                    IsEncrypted = true;
                    await ((SslStream)stream).AuthenticateAsServerAsync(certificate, clientAuth, SslProtocols.Tls12,
                        true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fault opening TCP Channel 2  via Cert - {0}", ex.Message);
                    State = ChannelState.Aborted;
                    Trace.TraceError(ex.Message);
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                    return;
                }
            }
            else
            {
                stream = localStream;
            }

            State = ChannelState.Open;
            OnOpen?.Invoke(this, new ChannelOpenEventArgs(Id, null));
        }

        public override async Task ReceiveAsync()
        {
            Exception error = null;
            byte[] buffer = null;
            int bytesRead = 0;
            byte[] msgBuffer = null;

            try
            {
                while (client != null && client.Connected && !token.IsCancellationRequested)
                {
                    await readConnection.WaitAsync();
                    using (MemoryStream bufferStream = new MemoryStream())
                    {
                        do
                        {
                            buffer = new byte[blockSize];

                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                            if (bytesRead == 0 && bufferStream.Length == 0)
                            {
                                return;
                            }

                            if (bytesRead + bufferStream.Length > maxBufferSize)
                            {
                                OnError?.Invoke(this,
                                    new ChannelErrorEventArgs(Id,
                                        new InvalidDataException("Message exceeds max buffer size to read.")));
                                return;
                            }

                            await bufferStream.WriteAsync(buffer, 0, bytesRead);
                        } while (localStream.DataAvailable && bytesRead == blockSize);

                        await bufferStream.FlushAsync();
                        bufferStream.Position = 0;

                        msgBuffer = new byte[bufferStream.Length];
                        await bufferStream.ReadAsync(msgBuffer, 0, msgBuffer.Length);

                        readConnection.Release();
                    }

                    if (msgBuffer != null && msgBuffer.Length > 0)
                    {
                        OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, msgBuffer));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fault receiving TCP Channel 2  - {0}", ex.Message);
                error = ex;
            }
            finally
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, error ?? new TimeoutException("Receiver closing")));
            }
        }

        public override async Task SendAsync(byte[] msg)
        {
            if (msg == null || msg.Length == 0)
            {
                OnError?.Invoke(this,
                    new ChannelErrorEventArgs(Id,
                        new IndexOutOfRangeException(
                            "TCP server channel cannot send null or 0-length message for sendasync-2")));
            }

            if (msg.Length > maxBufferSize)
            {
                OnError?.Invoke(this,
                    new ChannelErrorEventArgs(Id,
                        new IndexOutOfRangeException(
                            "TCP server channel message exceeds max buffer size for sendasync-2")));
            }

            await writeConnection.WaitAsync();
            queue.Enqueue(msg);

            while (queue.Count > 0)
            {
                byte[] message = queue.Dequeue();

                try
                {
                    if (protocol != null)
                    {
                        stream.Write(msg, 0, msg.Length);
                        stream.Flush();
                    }
                    else
                    {
                        await stream.WriteAsync(msg, 0, msg.Length);
                        await stream.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fault sending TCP Channel 2  - {0}", ex.Message);
                    Trace.TraceError(ex.Message);
                    State = ChannelState.Aborted;
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                }
            }

            writeConnection.Release();
        }

        #endregion methods

        #region dispose

        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;
                if (!(State == ChannelState.Closed || State == ChannelState.ClosedReceived))
                {
                    try
                    {
                        CloseAsync().GetAwaiter();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception Dispose/Closing TCP Server 2 {0}", ex.Message);
                        Console.WriteLine("***** Inner Exception {0} *****", ex.InnerException);
                        Console.WriteLine("***** Stack Trace {0} *****", ex.InnerException.StackTrace);
                    }
                }

                protocol = null;
                client = null;
                readConnection = null;
                writeConnection = null;
            }
        }

        #endregion dispose
    }
}