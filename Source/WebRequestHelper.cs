/*
 * Copyright 2020 Stevanus Ronald Riantono
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace GitHubHelper.Libs {
    public class WebRequestHelper : IWebRequestHelper {
        public virtual void DownloadFile(string url, string targetFilename, out HttpStatusCode statusCode, Action<WebRequest> onWebRequestCreated = null, int timeoutInSeconds = 300, int bufferSizeInBytes = 5120)
        {
            var request = WebRequest.Create(url);
            request.Timeout = (timeoutInSeconds * 1000);

            if (onWebRequestCreated != null) {
                onWebRequestCreated(request);
            }

            using (var response = request.GetResponse()) {
                using (var stream = response.GetResponseStream()) {
                    using (var writer = File.Open(targetFilename, FileMode.Create, FileAccess.ReadWrite)) {
                        int actualReadInBytes;
                        var buffer = new byte[bufferSizeInBytes];
                        while((actualReadInBytes = stream.Read(buffer, 0, bufferSizeInBytes)) > 0) {
                            writer.Write(buffer, 0, actualReadInBytes);
                        }

                        writer.Flush();
                        writer.Close();
                    }
                }

                if (response is HttpWebResponse) {
                    statusCode = ((HttpWebResponse)response).StatusCode;
                }
                else {
                    statusCode = HttpStatusCode.OK;
                }
            }
        }

        public virtual T GetData<T>(string url, out HttpStatusCode statusCode, byte[] requestData, Action<WebRequest> onWebRequestCreated = null, int timeoutInSeconds = 30, string methodType = "GET", string contentType = "application/json")
        {
            var request = WebRequest.Create(url);
            request.Timeout = (timeoutInSeconds * 1000);
            request.ContentType = contentType;
            request.Method = methodType;

            if (onWebRequestCreated != null) {
                onWebRequestCreated(request);
            }

            if (requestData != null) {
                request.ContentLength = requestData.Length;
                using (var stream = request.GetRequestStream()) {
                    stream.Write(requestData, 0, requestData.Length);
                    stream.Flush();
                    stream.Close();
                }
            }

            string responseData;
            using (var response = request.GetResponse()) {
                using (var reader = new StreamReader(response.GetResponseStream())) {
                    responseData = reader.ReadToEnd();
                }

                if (response is HttpWebResponse) {
                    statusCode = ((HttpWebResponse)response).StatusCode;
                }
                else {
                    statusCode = HttpStatusCode.OK;
                }
            }

            try {
                return JsonConvert.DeserializeObject<T>(responseData);
            }
            catch {
                return (T)Convert.ChangeType(responseData, typeof(T));
            }
        }
    }
}