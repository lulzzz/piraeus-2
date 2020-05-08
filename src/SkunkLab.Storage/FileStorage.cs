using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace SkunkLab.Storage
{
    public class FileStorage
    {
        private readonly CloudFileClient client;

        public FileStorage(string connectionString)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            StorageCredentials credentials = new StorageCredentials(account.Credentials.AccountName,
                Convert.ToBase64String(account.Credentials.ExportKey()));
            client = new CloudFileClient(account.FileStorageUri, credentials);
            client.DefaultRequestOptions.ParallelOperationThreadCount = 64;
            client.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(TimeSpan.FromMilliseconds(10000), 8);
            client.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromMinutes(10.0);
        }

        public event EventHandler<BytesTransferredEventArgs> OnDownloadBytesTransferred;

        public event EventHandler<BlobCompleteEventArgs> OnDownloadCompleted;

        public event EventHandler<BytesTransferredEventArgs> OnUploadBytesTransferred;

        public event EventHandler<BlobCompleteEventArgs> OnUploadCompleted;

        public async Task<byte[]> DownloadFileAsync(string path, string share, string filename,
            CancellationToken token = default)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            _ = share ?? throw new ArgumentNullException(nameof(share));
            _ = filename ?? throw new ArgumentNullException(nameof(filename));

            Exception error = null;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            double time = watch.Elapsed.TotalMilliseconds;
            long bytesTransferred = 0;

            byte[] buffer = null;

            try
            {
                CloudFileShare choudShare = client.GetShareReference(share);
                CloudFileDirectory dir = choudShare.GetRootDirectoryReference();
                CloudFile file = dir.GetFileReference(filename);

                IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                    progress =>
                    {
                        bytesTransferred = bytesTransferred < progress.BytesTransferred
                            ? progress.BytesTransferred
                            : bytesTransferred;
                        if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                            bytesTransferred <= progress.BytesTransferred)
                        {
                            OnDownloadBytesTransferred?.Invoke(this,
                                new BytesTransferredEventArgs(share, filename, bytesTransferred,
                                    file.Properties.Length));
                        }
                    });

                buffer = new byte[file.Properties.Length];
                await file.DownloadRangeToByteArrayAsync(buffer, 0, 0, buffer.Length, default, default, default,
                    progressHandler, token);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    buffer = null;
                }
                else
                {
                    error = ex;
                    throw ex;
                }
            }
            finally
            {
                watch.Stop();
                OnDownloadCompleted?.Invoke(this,
                    new BlobCompleteEventArgs(share, filename, token.IsCancellationRequested, error));
            }

            return buffer;
        }

        public async Task<byte[]> ReadFileAsync(string share, string filename, CancellationToken token = default)
        {
            _ = share ?? throw new ArgumentNullException(nameof(share));
            _ = filename ?? throw new ArgumentNullException(nameof(filename));

            Exception error = null;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            double time = watch.Elapsed.TotalMilliseconds;
            long bytesTransferred = 0;

            byte[] buffer = null;

            try
            {
                CloudFileShare choudShare = client.GetShareReference(share);
                CloudFileDirectory dir = choudShare.GetRootDirectoryReference();
                CloudFile file = dir.GetFileReference(filename);
                long length = file.Properties.Length;
                buffer = new byte[length];
                IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                    progress =>
                    {
                        bytesTransferred = bytesTransferred < progress.BytesTransferred
                            ? progress.BytesTransferred
                            : bytesTransferred;
                        if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                            bytesTransferred <= progress.BytesTransferred)
                        {
                            OnDownloadBytesTransferred?.Invoke(this,
                                new BytesTransferredEventArgs(share, filename, bytesTransferred, buffer.Length));
                        }
                    });

                await file.DownloadRangeToByteArrayAsync(buffer, 0, 0, buffer.Length, default, default, default,
                    progressHandler, token);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    buffer = null;
                }
                else
                {
                    error = ex;
                    throw ex;
                }
            }
            finally
            {
                watch.Stop();
                OnDownloadCompleted?.Invoke(this,
                    new BlobCompleteEventArgs(share, filename, token.IsCancellationRequested, error));
            }

            return buffer;
        }

        public async Task<Stream> ReadFileAsync(string share, string filename, Stream stream,
            CancellationToken token = default)
        {
            _ = share ?? throw new ArgumentNullException(nameof(share));
            _ = filename ?? throw new ArgumentNullException(nameof(filename));
            _ = stream ?? throw new ArgumentNullException(nameof(stream));

            Exception error = null;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            double time = watch.Elapsed.TotalMilliseconds;
            long bytesTransferred = 0;

            try
            {
                CloudFileShare choudShare = client.GetShareReference(share);
                CloudFileDirectory dir = choudShare.GetRootDirectoryReference();
                CloudFile file = dir.GetFileReference(filename);

                IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                    progress =>
                    {
                        bytesTransferred = bytesTransferred < progress.BytesTransferred
                            ? progress.BytesTransferred
                            : bytesTransferred;
                        if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                            bytesTransferred <= progress.BytesTransferred)
                        {
                            OnDownloadBytesTransferred?.Invoke(this,
                                new BytesTransferredEventArgs(share, filename, bytesTransferred,
                                    file.Properties.Length));
                        }
                    });

                await file.DownloadToStreamAsync(stream, default, default, default, progressHandler, token);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    stream = null;
                }
                else
                {
                    error = ex;
                    throw ex;
                }
            }
            finally
            {
                watch.Stop();
                OnDownloadCompleted?.Invoke(this,
                    new BlobCompleteEventArgs(share, filename, token.IsCancellationRequested, error));
            }

            return stream;
        }

        public async Task UploadFileAsync(string path, string share, string filename,
            string contentType = "application/octet-stream", CancellationToken token = default)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            _ = share ?? throw new ArgumentNullException(nameof(share));
            _ = filename ?? throw new ArgumentNullException(nameof(filename));

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("path");
            }

            Exception error = null;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            double time = watch.Elapsed.TotalMilliseconds;
            long bytesTransferred = 0;

            IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                progress =>
                {
                    bytesTransferred = bytesTransferred < progress.BytesTransferred
                        ? progress.BytesTransferred
                        : bytesTransferred;
                    FileInfo info = new FileInfo(path);
                    if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                        bytesTransferred <= progress.BytesTransferred)
                    {
                        OnUploadBytesTransferred?.Invoke(this,
                            new BytesTransferredEventArgs(share, filename, bytesTransferred, info.Length));
                    }
                });

            try
            {
                CloudFileShare choudShare = client.GetShareReference(share);
                CloudFileDirectory dir = choudShare.GetRootDirectoryReference();
                CloudFile file = dir.GetFileReference(filename);
                file.Properties.ContentType = contentType;
                await file.UploadFromFileAsync(path, default, default, default, progressHandler, token);
            }
            catch (Exception ex)
            {
                if (!(ex.InnerException is TaskCanceledException))
                {
                    error = ex;
                    throw ex;
                }
            }
            finally
            {
                watch.Stop();
                OnUploadCompleted?.Invoke(this,
                    new BlobCompleteEventArgs(share, filename, token.IsCancellationRequested, error));
            }
        }

        public async Task WriteFileAsync(string share, string filename, byte[] source,
            string contentType = "application/octet-stream", CancellationToken token = default)
        {
            _ = share ?? throw new ArgumentNullException(nameof(share));
            _ = filename ?? throw new ArgumentNullException(nameof(filename));
            _ = source ?? throw new ArgumentNullException(nameof(source));

            Exception error = null;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            double time = watch.Elapsed.TotalMilliseconds;
            long bytesTransferred = 0;

            IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                progress =>
                {
                    bytesTransferred = bytesTransferred < progress.BytesTransferred
                        ? progress.BytesTransferred
                        : bytesTransferred;
                    if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                        bytesTransferred <= progress.BytesTransferred)
                    {
                        OnUploadBytesTransferred?.Invoke(this,
                            new BytesTransferredEventArgs(share, filename, bytesTransferred, source.Length));
                    }
                });

            try
            {
                CloudFileShare choudShare = client.GetShareReference(share);
                CloudFileDirectory dir = choudShare.GetRootDirectoryReference();
                CloudFile file = dir.GetFileReference(filename);
                file.Properties.ContentType = contentType;
                await file.UploadFromByteArrayAsync(source, 0, source.Length, default, default, default, null, token);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    source = null;
                }
                else
                {
                    error = ex;
                    throw ex;
                }
            }
            finally
            {
                watch.Stop();
                OnUploadCompleted?.Invoke(this,
                    new BlobCompleteEventArgs(share, filename, token.IsCancellationRequested, error));
            }
        }

        public async Task WriteFileAsync(string share, string filename, Stream source,
            string contentType = "application/octet-stream", CancellationToken token = default)
        {
            _ = share ?? throw new ArgumentNullException(nameof(share));
            _ = filename ?? throw new ArgumentNullException(nameof(filename));
            _ = source ?? throw new ArgumentNullException(nameof(source));

            Exception error = null;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            double time = watch.Elapsed.TotalMilliseconds;
            long bytesTransferred = 0;

            IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                progress =>
                {
                    bytesTransferred = bytesTransferred < progress.BytesTransferred
                        ? progress.BytesTransferred
                        : bytesTransferred;
                    if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                        bytesTransferred <= progress.BytesTransferred)
                    {
                        OnUploadBytesTransferred?.Invoke(this,
                            new BytesTransferredEventArgs(share, filename, bytesTransferred, source.Length));
                    }
                });

            try
            {
                CloudFileShare choudShare = client.GetShareReference(share);
                CloudFileDirectory dir = choudShare.GetRootDirectoryReference();
                CloudFile file = dir.GetFileReference(filename);
                file.Properties.ContentType = contentType;
                await file.UploadFromStreamAsync(source, source.Length, default, default, default, progressHandler,
                    token);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    source = null;
                }
                else
                {
                    error = ex;
                    throw ex;
                }
            }
            finally
            {
                watch.Stop();
                OnUploadCompleted?.Invoke(this,
                    new BlobCompleteEventArgs(share, filename, token.IsCancellationRequested, error));
            }
        }
    }
}