﻿using Org.BouncyCastle.Crypto.Tls;
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

namespace SkunkLab.Channels.Tcp
{
    public class TcpClientChannel2 : TcpChannel
    {
        #region ctor

        public TcpClientChannel2(string hostname, int port, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.hostname = hostname;
            this.port = port;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(string hostname, int port, IPEndPoint localEP, int blockSize, int maxBufferSize, CancellationToken token)
            : this(hostname, port, localEP, null, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(IPEndPoint remoteEndpoint, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.remoteEP = remoteEndpoint;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPEndPoint remoteEndpoint, IPEndPoint localEP, int blockSize, int maxBufferSize, CancellationToken token)
            : this(remoteEndpoint, localEP, null, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(IPAddress address, int port, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.address = address;
            this.port = port;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPAddress address, int port, IPEndPoint localEP, int blockSize, int maxBufferSize, CancellationToken token)
            : this(address, port, localEP, null, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(string hostname, int port, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
            : this(hostname, port, null, certificate, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(string hostname, int port, IPEndPoint localEP, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.hostname = hostname;
            this.port = port;
            this.localEP = localEP;
            this.certificate = certificate;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            this.token.Register(async () => await CloseAsync());
            Id = "tcp2-" + Guid.NewGuid().ToString();
            Port = port;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPEndPoint remoteEndpoint, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
            : this(remoteEndpoint, null, certificate, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(IPEndPoint remoteEndpoint, IPEndPoint localEP, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
        {
            _ = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));

            remoteEP = remoteEndpoint;
            this.localEP = localEP;
            this.certificate = certificate;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.token.Register(async () => await CloseAsync());

            if (certificate != null)
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(remoteEndpoint.Address);
                hostname = ipHostInfo.HostName;
            }

            Port = remoteEndpoint.Port;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPAddress address, int port, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
            : this(address, port, null, certificate, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(IPAddress address, int port, IPEndPoint localEP, X509Certificate2 certificate, int blockSize, int maxBufferSize, CancellationToken token)
        {
            _ = address ?? throw new ArgumentNullException(nameof(address));

            this.address = address;
            this.port = port;
            this.localEP = localEP;
            this.certificate = certificate;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.token.Register(async () => await CloseAsync());

            if (certificate != null)
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(address);
                hostname = ipHostInfo.HostName;
            }

            Port = port;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPAddress address, int port, IPEndPoint localEP, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.address = address;
            this.port = port;
            this.localEP = localEP;
            this.pskIdentity = pskIdentity;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.psk = psk;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.token = token;
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPAddress address, int port, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
            : this(address, port, null, pskIdentity, psk, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(string hostname, int port, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
            : this(hostname, port, null, pskIdentity, psk, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(string hostname, int port, IPEndPoint localEP, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.hostname = hostname;
            this.port = port;
            this.pskIdentity = pskIdentity;
            this.localEP = localEP;
            this.psk = psk;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.queue = new Queue<byte[]>();
        }

        public TcpClientChannel2(IPEndPoint remoteEP, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
            : this(remoteEP, null, pskIdentity, psk, blockSize, maxBufferSize, token)
        {
        }

        public TcpClientChannel2(IPEndPoint remoteEP, IPEndPoint localEP, string pskIdentity, byte[] psk, int blockSize, int maxBufferSize, CancellationToken token)
        {
            this.remoteEP = remoteEP;
            this.localEP = localEP;
            this.pskIdentity = pskIdentity;
            this.psk = psk;
            this.blockSize = blockSize;
            this.maxBufferSize = maxBufferSize;
            this.token = token;
            Id = "tcp2-" + Guid.NewGuid().ToString();
            this.queue = new Queue<byte[]>();
        }

        #endregion ctor

        #region private member variables

        private readonly IPAddress address;

        private readonly int blockSize;

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

        private ChannelState _state;

        private TcpClient client;

        private bool disposed;

        private NetworkStream localStream;

        private TlsClientProtocol protocol;

        private SemaphoreSlim readConnection;

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

        public override string Id { get; internal set; }

        public override bool IsAuthenticated { get; internal set; }

        public override bool IsConnected
        {
            get
            {
                if (disposed || client == null || client.Client == null)
                {
                    return false;
                }
                else
                {
                    return client.Client.Connected;
                }
            }
        }

        public override bool IsEncrypted { get; internal set; }

        public override int Port { get; internal set; }

        public override bool RequireBlocking => psk != null;

        public override ChannelState State
        {
            get => _state;
            internal set
            {
                if (_state != value)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }

                _state = value;
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

            if (client != null && client.Client != null && (client.Client.Connected && client.Client.Poll(10, SelectMode.SelectRead)))
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

            try
            {
                if (localEP != null)
                {
                    client = new TcpClient(localEP);
                }
                else
                {
                    client = new TcpClient();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                return;
            }

            client.LingerState = new LingerOption(true, 0);
            client.NoDelay = true;
            client.ExclusiveAddressUse = false;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.UseOnlyOverlappedIO = true;

            try
            {
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
                    throw new InvalidDataException("Tcp client connection parameters not sufficient.");
                }
            }
            catch (AggregateException ae)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ae.Flatten().InnerException));
                return;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                return;
            }

            try
            {
                localStream = client.GetStream();

                if (psk != null)
                {
                    try
                    {
                        protocol = client.ConnectPskTlsClient(pskIdentity, psk, localStream);
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
                    try
                    {
                        stream = new SslStream(localStream, true, new RemoteCertificateValidationCallback(ValidateCertificate));
                        IsEncrypted = true;
                        X509CertificateCollection certificates = new X509CertificateCollection();
                        X509Certificate cert = new X509Certificate(certificate.RawData);
                        certificates.Add(cert);
                        SslStream sslStream = (SslStream)stream;
                        await sslStream.AuthenticateAsClientAsync(hostname, certificates, SslProtocols.Tls12, true);

                        if (!sslStream.IsEncrypted || !sslStream.IsSigned)
                        {
                            stream.Dispose();
                            throw new AuthenticationException("SSL stream is not both encrypted and signed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        protocol = null;

                        if (client != null)
                        {
                            State = ChannelState.ClosedReceived;
                            if (client.Client.UseOnlyOverlappedIO)
                            {
                                client.Client.DuplicateAndClose(Process.GetCurrentProcess().Id);
                            }
                            else
                            {
                                client.Close();
                            }
                            stream.Close();
                            client = null;
                        }

                        State = ChannelState.Aborted;
                        Trace.TraceError(ex.Message);
                        OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                    }
                }
                else
                {
                    stream = localStream;
                }
            }
            catch (AggregateException ae)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ae.Flatten().InnerException));
                return;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                return;
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
                    try
                    {
                        await readConnection.WaitAsync();
                        using (MemoryStream bufferStream = new MemoryStream())
                        {
                            do
                            {
                                buffer = new byte[16384];

                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                                if (bytesRead == 0 && bufferStream.Length == 0)
                                {
                                    Console.WriteLine("Forcing return");
                                    return;
                                }

                                if (bytesRead + bufferStream.Length > maxBufferSize)
                                {
                                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, new InvalidDataException("Message exceeds max buffer size to read.")));
                                    return;
                                }

                                await bufferStream.WriteAsync(buffer, 0, bytesRead);
                            } while (localStream.DataAvailable && bytesRead == 16384);

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
                    finally
                    {
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
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, new IndexOutOfRangeException("TCP client channel cannot send null or 0-length message for sendasync-2")));
            }

            if (msg.Length > maxBufferSize)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, new IndexOutOfRangeException("TCP client channel message exceeds max buffer size for sendasync-2")));
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
                    Trace.TraceError(ex.Message);
                    State = ChannelState.Aborted;
                    OnError?.Invoke(this, new ChannelErrorEventArgs(Id, ex));
                }
            }

            writeConnection.Release();
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
                        Console.WriteLine("Exception Dispose/Closing TCP Client 2 {0}", ex.Message);
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

        #region private methods

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            if (sslpolicyerrors != SslPolicyErrors.None)
            {
                return false;
            }

            if (certificate == null)
            {
                return false;
            }
            else
            {
                X509Certificate2 cert = new X509Certificate2(certificate);
                return (cert.NotBefore < DateTime.Now && cert.NotAfter > DateTime.Now);
            }
        }

        #endregion private methods
    }
}