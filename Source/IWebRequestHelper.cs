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
using System.Net;

namespace GitHubHelper.Libs {
    public interface IWebRequestHelper {
        void DownloadFile(string url, string targetFilename, out HttpStatusCode statusCode, Action<WebRequest> onWebRequestCreated = null, int timeoutInSeconds = 300, int bufferSizeInBytes = 5120);

        T GetData<T>(string url, out HttpStatusCode statusCode, byte[] requestData, Action<WebRequest> onWebRequestCreated = null, int timeoutInSeconds = 30, string methodType = "GET", string contentType = "application/json");
    }
}