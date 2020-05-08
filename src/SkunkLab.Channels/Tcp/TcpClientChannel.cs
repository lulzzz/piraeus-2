using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    public class TcpClientChannel : TcpChannel
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

        public TcpClientChannel(string hostname, int port, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            this.hostname = hostname;
            this.port = port;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp-" + Guid.NewGuid();
            this.token.Register(async () => await CloseAsync());
            queue = new Queue<byte[]>();
        }

        public TcpClientChannel(string hostname, int port, IPEndPoint localEP, int maxBufferSize = 0x400000,
            CancellationToken token = default)
            : this(hostname, port, localEP, null, maxBufferSize, token)
        {
        }

        public TcpClientChannel(IPEndPoint remoteEndpoint, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            remoteEP = remoteEndpoint;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp-" + Guid.NewGuid();
            this.token.Register(async () => await CloseAsync());
            queue = new Queue<byte[]>();
        }

        public TcpClientChannel(IPEndPoint remoteEndpoint, IPEndPoint localEP, int maxBufferSize = 0x400000,
            CancellationToken token = default)
            : this(remoteEndpoint, localEP, null, maxBufferSize, token)
        {
        }

        public TcpClientChannel(IPAddress address, int port, int maxBufferSize = 0x400000,
            CancellationToken token = default)
        {
            this.address = address;
            this.port = port;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp-" + Guid.NewGuid();
            this.token.Register(async () => await CloseAsync());
            queue = new Queue<byte[]>();
        }

        public TcpClientChannel(IPAddress address, int port, IPEndPoint localEP, int maxBufferSize = 0x400000,
            CancellationToken token = default)
            : this(address, port, localEP, null, maxBufferSize, token)
        {
        }

        public TcpClientChannel(string hostname, int port, X509Certificate2 certificate, int maxBufferSize = 0x400000,
            CancellationToken token = default)
            : this(hostname, port, null, certificate, maxBufferSize, token)
        {
        }

        public TcpClientChannel(string hostname, int port, IPEndPoint localEP, X509Certificate2 certificate,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            this.hostname = hostname;
            this.port = port;
            this.localEP = localEP;
            this.certificate = certificate;
            this.token = token;
            this.maxBufferSize = maxBufferSize;
            Id = "tcp-" + Guid.NewGuid();
            this.token.Register(async () => await CloseAsync());

            Port = port;
            queue = new Queue<byte[]>();
        }

        public TcpClientChannel(IPEndPoint remoteEndpoint, X509Certificate2 certificate, int maxBufferSize = 0x400000,
            CancellationToken token = default)
            : this(remoteEndpoint, null, certificate, maxBufferSize, token)
        {
        }

        public TcpClientChannel(IPEndPoint remoteEndpoint, IPEndPoint localEP, X509Certificate2 certificate,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            _ = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));

            remoteEP = remoteEndpoint;
            this.localEP = localEP;
            this.certificate = certificate;
            this.token = token;
            this.maxBufferSize = maxBufferSize;
            Id = "tcp-" + Guid.NewGuid();
            this.token.Register(async () => await CloseAsync());

            if (certificate != null)
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(remoteEndpoint.Address);
                hostname = ipHostInfo.HostName;
            }

            Port = remoteEndpoint.Port;
            queue = new Queue<byte[]>();
        }

        public TcpClientChannel(IPAddress address, int port, X509Certificate2 certificate, int maxBufferSize = 0x400000,
            CancellationToken token = default)
            : this(address, port, null, certificate, maxBufferSize, token)
        {
        }

        public TcpClientChannel(IPAddress address, int port, IPEndPoint localEP, X509Certificate2 certificate,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            _ = address ?? throw new ArgumentNullException(nameof(address));

            this.address = address;
            this.port = port;
            this.localEP = localEP;
            this.certificate = certificate;
            this.token = token;
            this.maxBufferSize = maxBufferSize;
            Id = "tcp-" + Guid.NewGuid();
            this.token.Register(async () => await CloseAsync());

            if (certificate != null)
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(address);
                hostname = ipHostInfo.HostName;
            }

            Port = port;
            queue = new Queue<byte[]>();
        }

        public TcpClientChannel(IPAddress address, int port, IPEndPoint localEP, string pskIdentity, byte[] psk,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            this.address = address;
            this.port = port;
            this.localEP = localEP;
            this.pskIdentity = pskIdentity;
            this.psk = psk;
            this.token = token;
            this.token.Register(async () => await CloseAsync());
            this.maxBufferSize = maxBufferSize;
            Id = "tcp-" + Guid.NewGuid();
            queue = new Queue<byte[]>();
        }

        public TcpClientChannel(IPAddress address, int port, string pskIdentity, byte[] psk,
            int maxBufferSize = 0x400000, CancellationToken token = default)
            : this(address, port, null, pskIdentity, psk, maxBufferSize, token)
        {
        }

        public TcpClientChannel(string hostname, int port, string pskIdentity, byte[] psk, int maxBufferSize = 0x400000,
            CancellationToken token = default)
            : this(hostname, port, null, pskIdentity, psk, maxBufferSize, token)
        {
        }

        public TcpClientChannel(string hostname, int port, IPEndPoint localEP, string pskIdentity, byte[] psk,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            this.hostname = hostname;
            this.port = port;
            this.pskIdentity = pskIdentity;
            this.localEP = localEP;
            this.psk = psk;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.token.Register(async () => await CloseAsync());
            Id = "tcp-" + Guid.NewGuid();
            queue = new Queue<byte[]>();
        }

        public TcpClientChannel(IPEndPoint remoteEP, string pskIdentity, byte[] psk, int maxBufferSize = 0x400000,
            CancellationToken token = default)
            : this(remoteEP, null, pskIdentity, psk, maxBufferSize, token)
        {
        }

        public TcpClientChannel(IPEndPoint remoteEP, IPEndPoint localEP, string pskIdentity, byte[] psk,
            int maxBufferSize = 0x400000, CancellationToken token = default)
        {
            this.remoteEP = remoteEP;
            this.localEP = localEP;
            this.pskIdentity = pskIdentity;
            this.psk = psk;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.token.Register(async () => await CloseAsync());
            Id = "tcp-" + Guid.NewGuid();
            queue = new Queue<byte[]>();
        }

        #endregion ctor

        #region private member variables

        private readonly IPAddress address;

        private readonly X509Certificate2 certificate;

        private readonly string hostname;

        private readonly IPEndPoint localEP;

        private readonly int maxBufferSize;

        private readonly int port;

        private readonly byte[] psk;

        private readonly string pskIdentity;

        private readonly Queue<byte[]> queue;

        private readonly IPEndPoint remoteEP;

        private readonly CancellationToken token;

        private TcpClient client;

        private bool disposed;

        private NetworkStream localStream;

        private TlsClientProtocol protocol;

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

        public override bool RequireBlocking => psk != null;

        public override ChannelState State
        {
            get => state;
            internal set
            {
                if (state != value)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }

                state = value;
            }
        }

        public override string TypeId => "TCP";

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

        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        public override async Task OpenAsync()
        {
            State = ChannelState.Connecting;
            readConnection = new SemaphoreSlim(1);
            writeConnection = new SemaphoreSlim(1);

            if (localEP != null)
            {
                client = new TcpClient(localEP);
            }
            else
            {
                client = new TcpClient();
            }

            if (remoteEP != null)
            {
                await client.ConnectAsync(remoteEP.Address, remoteEP.Port);
            }
            else if (address != null)
            {
                await client.ConnectAsync(address, port);
            }
            else if (!string.IsNullOrEmpty(hostname))
            {
                await client.ConnectAsync(hostname, port);
            }
            else
            {
                State = ChannelState.Aborted;
                InvalidOperationException ioe =
                    new InvalidOperationException("Tcp client connection parameters not sufficient.");
                Trace.TraceError(ioe.Message);
                throw ioe;
            }

            localStream = client.GetStream();

            if (psk != null)
            {
                try
                {
                    protocol = TlsClientUtil.ConnectPskTlsClient(pskIdentity, psk, localStream);
                    stream = protocol.Stream;
                    IsEncrypted = true;
                }
                catch (Exception ex)
                {
                    State = ChannelState.Aborted;
                    Console.WriteLine("Fault opening TLS connection {0}", ex.Message);
                    Trace.TraceError(ex.Message);
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                    return;
                }
            }
            else if (certificate != null)
            {
                stream = new SslStream(localStream, true, ValidateCertificate);
                IsEncrypted = true;

                try
                {
                    X509CertificateCollection certificates = new X509CertificateCollection();
                    X509Certificate cert = new X509Certificate(certificate.RawData);
                    certificates.Add(cert);
                    SslStream sslStream = (SslStream)stream;
                    await sslStream.AuthenticateAsClientAsync(hostname, certificates, SslProtocols.Tls12, true);

                    if (!sslStream.IsEncrypted || !sslStream.IsSigned)
                    {
                        throw new AuthenticationException("SSL stream is not both encrypted and signed.");
                    }
                }
                catch (Exception ex)
                {
                    State = ChannelState.Aborted;
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                    throw;
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
            byte[] prefix = null;
            int remainingLength = 0;
            int offset = 0;
            int bytesRead = 0;

            try
            {
                while (client.Connected && !token.IsCancellationRequested)
                {
                    await readConnection.WaitAsync();

                    while (offset < 4)
                    {
                        if (offset == 0)
                        {
                            prefix = new byte[4];
                        }

                        bytesRead = await stream.ReadAsync(prefix, offset, prefix.Length - offset);

                        if (bytesRead == 0)
                        {
                            return;
                        }

                        offset += bytesRead;
                    }

                    prefix = BitConverter.IsLittleEndian ? prefix.Reverse().ToArray() : prefix;
                    remainingLength = BitConverter.ToInt32(prefix, 0);

                    if (remainingLength >= maxBufferSize)
                    {
                        throw new IndexOutOfRangeException(
                            "TCP client channel receive message exceeds max buffer size for receiveasync");
                    }

                    offset = 0;

                    byte[] message = new byte[remainingLength];

                    while (remainingLength > 0)
                    {
                        buffer = new byte[remainingLength];
                        bytesRead = await stream.ReadAsync(buffer, 0, remainingLength);
                        remainingLength -= bytesRead;
                        Buffer.BlockCopy(buffer, 0, message, offset, bytesRead);
                        offset += bytesRead;
                    }

                    OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, message));

                    offset = 0;
                    readConnection.Release();
                }
            }
            catch (Exception ex)
            {
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
                            "TCP client channel cannot send null or 0-length message for sendasync-1")));
            }

            if (msg.Length > maxBufferSize)
            {
                OnError?.Invoke(this,
                    new ChannelErrorEventArgs(Id,
                        new IndexOutOfRangeException(
                            "TCP client channel message exceeds max buffer size for sendasync-1")));
            }

            queue.Enqueue(msg);

            while (queue.Count > 0)
            {
                byte[] message = queue.Dequeue();

                try
                {
                    await writeConnection.WaitAsync();
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
                    Trace.TraceError(ex.Message);
                    State = ChannelState.Aborted;
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                }
                finally
                {
                    writeConnection.Release();
                }
            }
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
                        Console.WriteLine("Exception Dispose/Closing TCP Client {0}", ex.Message);
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

        #endregion methods
    }
}