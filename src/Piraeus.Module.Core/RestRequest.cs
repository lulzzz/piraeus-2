using System.IO;
using System.Net;

namespace Piraeus.Module
{
    public class RestRequest : RestRequestBase
    {
        private readonly RestRequestBuilder requestBuilder;

        public RestRequest(RestRequestBuilder builder)
        {
            requestBuilder = builder;
        }

        public override void Delete()
        {
            HttpWebRequest request = requestBuilder.BuildRequest();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }
        }

        public override T Get<T>()
        {
            string contentType = requestBuilder.ContentType.ToLowerInvariant();
            HttpWebRequest request = requestBuilder.BuildRequest();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST GET operation return status code {0}",
                    response.StatusCode.ToString()));
            }

            byte[] buffer = new byte[16384];
            byte[] msg = null;
            using (Stream stream = response.GetResponseStream())
            {
                using MemoryStream bufferStream = new MemoryStream();
                int bytesRead;
                do
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        bufferStream.Write(buffer, 0, bytesRead);
                    }
                } while (bytesRead > 0);

                if (bufferStream != null && bufferStream.Length > 0)
                {
                    msg = bufferStream.ToArray();
                }
            }

            if (msg != null)
            {
                return Serializer.Deserialize<T>(contentType, msg);
            }

            return default;
        }

        public override void Post()
        {
            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = 0;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }
        }

        public override T Post<T>()
        {
            string contentType = requestBuilder.ContentType.ToLowerInvariant();

            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = 0;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }

            byte[] buffer = new byte[16384];
            byte[] msg = null;
            using (Stream responseStream = response.GetResponseStream())
            {
                using MemoryStream bufferStream = new MemoryStream();
                int bytesRead;
                do
                {
                    bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        bufferStream.Write(buffer, 0, bytesRead);
                    }
                } while (bytesRead > 0);

                msg = bufferStream.ToArray();
            }

            return Serializer.Deserialize<T>(contentType, msg);
        }

        public override U Post<T, U>(T body)
        {
            string contentType = requestBuilder.ContentType.ToLowerInvariant();
            byte[] payload = Serializer.Serialize(contentType, body);

            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = payload.Length;

            Stream stream = request.GetRequestStream();
            stream.Write(payload, 0, payload.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }

            byte[] buffer = new byte[16384];
            byte[] msg = null;
            using (Stream responseStream = response.GetResponseStream())
            {
                using MemoryStream bufferStream = new MemoryStream();
                int bytesRead;
                do
                {
                    bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        bufferStream.Write(buffer, 0, bytesRead);
                    }
                } while (bytesRead > 0);

                msg = bufferStream.ToArray();
                //buffer = new byte[response.ContentLength];
                //responseStream.Read(buffer, 0, buffer.Length);
            }

            //return Serializer.Deserialize<U>(contentType, buffer);
            return Serializer.Deserialize<U>(contentType, msg);
        }

        public override void Post<T>(T body)
        {
            string contentType = requestBuilder.ContentType.ToLowerInvariant();
            byte[] payload = Serializer.Serialize(contentType, body);
            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = payload.Length;

            Stream stream = request.GetRequestStream();
            stream.Write(payload, 0, payload.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }
        }

        public override void Put<T>(T body)
        {
            string contentType = requestBuilder.ContentType.ToLowerInvariant();
            byte[] payload = Serializer.Serialize(contentType, body);
            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = payload.Length;

            Stream stream = request.GetRequestStream();
            stream.Write(payload, 0, payload.Length);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST PUT operation return status code {0}",
                    response.StatusCode.ToString()));
            }
        }
    }
}