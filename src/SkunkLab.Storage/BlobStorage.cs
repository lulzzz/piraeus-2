using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace SkunkLab.Storage
{
    public class BlobStorage
    {
        private static SkunkLabBufferManager bufferManager;

        private static BlobStorage instance;

        private readonly CloudBlobClient client;

        protected BlobStorage(string connectionString)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            StorageCredentials credentials = new StorageCredentials(account.Credentials.AccountName,
                Convert.ToBase64String(account.Credentials.ExportKey()));
            client = new CloudBlobClient(account.BlobStorageUri, credentials);
            client.DefaultRequestOptions.ParallelOperationThreadCount = 64;
            client.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = 1048576;
            client.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(TimeSpan.FromMilliseconds(10000), 8);
            client.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromMinutes(5.0);

            if (bufferManager != null)
            {
                client.BufferManager = bufferManager;
            }
        }

        protected BlobStorage(string connectionString, string sasToken)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            StorageCredentials credentials = new StorageCredentials(sasToken);
            client = new CloudBlobClient(account.BlobStorageUri, credentials);
            client.DefaultRequestOptions.ParallelOperationThreadCount = 64;
            client.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = 1048576;
            client.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(TimeSpan.FromMilliseconds(10000), 8);
            client.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromMinutes(3.0);

            if (bufferManager != null)
            {
                client.BufferManager = bufferManager;
            }
        }

        public event EventHandler<BytesTransferredEventArgs> OnDownloadBytesTransferred;

        public event EventHandler<BlobCompleteEventArgs> OnDownloadCompleted;

        public event EventHandler<BytesTransferredEventArgs> OnUploadBytesTransferred;

        public event EventHandler<BlobCompleteEventArgs> OnUploadCompleted;

        public static BlobStorage CreateSingleton(string connectionString)
        {
            if (instance == null)
            {
                instance = new BlobStorage(connectionString);
            }

            return instance;
        }

        public static BlobStorage CreateSingleton(string connectionString, long maxBufferPoolSize,
            int defaultBufferSize)
        {
            BufferManager manager = BufferManager.CreateBufferManager(maxBufferPoolSize, defaultBufferSize);
            bufferManager = new SkunkLabBufferManager(manager, defaultBufferSize);
            return CreateSingleton(connectionString);
        }

        public static BlobStorage New(string connectionString, long maxBufferPoolSize = 0, int defaultBufferSize = 0)
        {
            if (maxBufferPoolSize > 0)
            {
                BufferManager manager = BufferManager.CreateBufferManager(maxBufferPoolSize, defaultBufferSize);
                bufferManager = new SkunkLabBufferManager(manager, defaultBufferSize);
            }

            return new BlobStorage(connectionString);
        }

        public static BlobStorage New(string connectionString, string sasToken, long maxBufferPoolSize = 0,
            int defaultBufferSize = 0)
        {
            if (maxBufferPoolSize > 0)
            {
                BufferManager manager = BufferManager.CreateBufferManager(maxBufferPoolSize, defaultBufferSize);
                bufferManager = new SkunkLabBufferManager(manager, defaultBufferSize);
            }

            return new BlobStorage(connectionString, sasToken);
        }

        #region Blob Writers

        #region Block Blob Writers

        public async Task WriteBlockBlobAsync(string containerName, string filename, Stream source,
            string contentType = "application/octet-stream", CancellationToken token = default)
        {
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
                            new BytesTransferredEventArgs(containerName, filename, bytesTransferred, source.Length));
                    }
                });

            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                CloudBlockBlob blob = container.GetBlockBlobReference(filename);
                blob.Properties.ContentType = contentType;

                await blob.UploadFromStreamAsync(source, default, default, default, progressHandler, token);
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
                    new BlobCompleteEventArgs(containerName, filename, token.IsCancellationRequested, error));
            }
        }

        public async Task WriteBlockBlobAsync(string containerName, string filename, byte[] source,
            string contentType = "application/octet-stream", CancellationToken token = default)
        {
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
                        OnDownloadBytesTransferred?.Invoke(this,
                            new BytesTransferredEventArgs(containerName, filename, bytesTransferred, source.Length));
                    }
                });

            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                CloudBlockBlob blob = container.GetBlockBlobReference(filename);
                blob.Properties.ContentType = contentType;

                await blob.UploadFromByteArrayAsync(source, 0, source.Length, default, default, default,
                    progressHandler, token);
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
                    new BlobCompleteEventArgs(containerName, filename, token.IsCancellationRequested, error));
            }
        }

        #endregion Block Blob Writers

        #region Page Blob Writers

        public async Task WritePageBlobAsync(string containerName, string filename, Stream source,
            string contentType = "application/octet-stream", CancellationToken token = default)
        {
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
                            new BytesTransferredEventArgs(containerName, filename, bytesTransferred, source.Length));
                    }
                });

            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                CloudPageBlob blob = container.GetPageBlobReference(filename);

                blob.Properties.ContentType = contentType;
                await blob.UploadFromStreamAsync(source, null, default, default, default, progressHandler, token);
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
                    new BlobCompleteEventArgs(containerName, filename, token.IsCancellationRequested, error));
            }
        }

        public async Task WritePageBlobAsync(string containerName, string filename, byte[] source,
            string contentType = "application/octet-stream", CancellationToken token = default)
        {
            _ = filename ?? throw new ArgumentNullException(nameof(filename));

            if (source == null)
            {
                throw new ArgumentException("source");
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
                    if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                        bytesTransferred <= progress.BytesTransferred)
                    {
                        OnUploadBytesTransferred?.Invoke(this,
                            new BytesTransferredEventArgs(containerName, filename, bytesTransferred, source.Length));
                    }
                });

            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                CloudPageBlob blob = container.GetPageBlobReference(filename);

                blob.Properties.ContentType = contentType;
                await blob.UploadFromByteArrayAsync(source, 0, source.Length, null, default, default, default,
                    progressHandler, token);
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
                    new BlobCompleteEventArgs(containerName, filename, token.IsCancellationRequested, error));
            }
        }

        #endregion Page Blob Writers

        #region Append Blob Writers

        public async Task WriteAppendBlobAsync(string containerName, string filename, Stream source,
            CancellationToken token = default)
        {
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
                            new BytesTransferredEventArgs(containerName, filename, bytesTransferred, source.Length));
                    }
                });

            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                CloudAppendBlob blob = container.GetAppendBlobReference(filename);
                await blob.UploadFromStreamAsync(source, default, default, default, progressHandler, token);
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
                    new BlobCompleteEventArgs(containerName, filename, token.IsCancellationRequested, error));
            }
        }

        public async Task WriteAppendBlobAsync(string containerName, string filename, byte[] source,
            CancellationToken token = default)
        {
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
                            new BytesTransferredEventArgs(containerName, filename, bytesTransferred, source.Length));
                    }
                });

            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                CloudAppendBlob blob = container.GetAppendBlobReference(filename);
                await blob.UploadFromByteArrayAsync(source, 0, source.Length, default, default, default,
                    progressHandler, token);
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
                    new BlobCompleteEventArgs(containerName, filename, token.IsCancellationRequested, error));
            }
        }

        #endregion Append Blob Writers

        #endregion Blob Writers

        #region Blob Readers

        #region Block Blob Readers

        public async Task<byte[]> ReadBlockBlobAsync(string containerName, string filename)
        {
            _ = filename ?? throw new ArgumentNullException(nameof(filename));

            CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
            CloudBlockBlob blob = container.GetBlockBlobReference(filename);
            return await DownloadAsync(blob);
        }

        #endregion Block Blob Readers

        #region Page Blob Readers

        public async Task<byte[]> ReadPageBlobAsync(string containerName, string filename)
        {
            _ = filename ?? throw new ArgumentNullException(nameof(filename));

            CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
            CloudPageBlob blob = container.GetPageBlobReference(filename);
            return await DownloadAsync(blob);
        }

        #endregion Page Blob Readers

        #region Append Blob Readers

        public async Task<byte[]> ReadAppendBlobAsync(string containerName, string filename)
        {
            _ = filename ?? throw new ArgumentNullException(nameof(filename));

            CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
            CloudAppendBlob blob = container.GetAppendBlobReference(filename);
            return await DownloadAsync(blob);
        }

        #endregion Append Blob Readers

        #endregion Blob Readers

        #region List Blobs

        public async Task<BlobResultSegment> ListBlobsAsync(string containerName, BlobContinuationToken token)
        {
            CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
            return await container.ListBlobsSegmentedAsync(token);
        }

        public async Task<BlobResultSegment> ListBlobsSegmentedAsync(string containerName, BlobContinuationToken token)
        {
            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                return await container.ListBlobsSegmentedAsync(token);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                throw ex;
            }
        }

        #endregion List Blobs

        #region Direct File Upload/Download

        public async Task DownloadBlockBlobToFile(string filePath, string containerName, string blobFilename,
            CancellationToken token = default)
        {
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _ = containerName ?? throw new ArgumentNullException(nameof(containerName));
            _ = blobFilename ?? throw new ArgumentNullException(nameof(blobFilename));

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
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
                    FileInfo info = new FileInfo(filePath);
                    if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                        bytesTransferred <= progress.BytesTransferred)
                    {
                        OnDownloadBytesTransferred?.Invoke(this,
                            new BytesTransferredEventArgs(containerName, blobFilename, bytesTransferred, info.Length));
                    }
                });

            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                CloudBlockBlob blob = container.GetBlockBlobReference(blobFilename);
                await blob.DownloadToFileAsync(filePath, FileMode.Create, default, default, default, progressHandler,
                    token);
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
                OnDownloadCompleted?.Invoke(this,
                    new BlobCompleteEventArgs(containerName, blobFilename, token.IsCancellationRequested, error));
            }
        }

        public async Task UploadFileToBlockBlob(string filePath, string containerName, string blobFilename,
            CancellationToken token = default)
        {
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _ = containerName ?? throw new ArgumentNullException(nameof(containerName));
            _ = blobFilename ?? throw new ArgumentNullException(nameof(blobFilename));

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
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
                    FileInfo info = new FileInfo(filePath);
                    if (watch.Elapsed.TotalMilliseconds > time + 1000.0 &&
                        bytesTransferred <= progress.BytesTransferred)
                    {
                        OnUploadBytesTransferred?.Invoke(this,
                            new BytesTransferredEventArgs(containerName, blobFilename, bytesTransferred, info.Length));
                    }
                });

            try
            {
                CloudBlobContainer container = await GetContainerReferenceAsync(containerName);
                CloudBlockBlob blob = container.GetBlockBlobReference(blobFilename);
                await blob.UploadFromFileAsync(filePath, default, default, default, progressHandler, token);
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
                    new BlobCompleteEventArgs(containerName, blobFilename, token.IsCancellationRequested, error));
            }
        }

        #endregion Direct File Upload/Download

        #region Utilities

        public async Task<byte[]> DownloadAsync(ICloudBlob blob)
        {
            byte[] buffer = null;
            using (MemoryStream stream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(stream);
                buffer = stream.ToArray();
            }

            return buffer;
        }

        public async Task<CloudBlobContainer> GetContainerReferenceAsync(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
            {
                return client.GetContainerReference("$root");
            }

            CloudBlobContainer container = client.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }

        #endregion Utilities
    }
}