/**
* Copyright 2020 The Nakama Authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.Runtime.Serialization;

namespace Nakama.SocketInternal
{
    [DataContract]
    public class ApiNotification : IApiNotification
    {
        /// <inheritdoc />
        [DataMember(Name="code", Order = 1), Preserve]
        public int Code { get; set; }

        /// <inheritdoc />
        [DataMember(Name="content", Order = 2), Preserve]
        public string Content { get; set; }

        /// <inheritdoc />
        [DataMember(Name="create_time", Order = 3), Preserve]
        public string CreateTime { get; set; }

        /// <inheritdoc />
        [DataMember(Name="id", Order = 4), Preserve]
        public string Id { get; set; }

        /// <inheritdoc />
        [DataMember(Name="persistent", Order = 5), Preserve]
        public bool Persistent { get; set; }

        /// <inheritdoc />
        [DataMember(Name="sender_id", Order = 6), Preserve]
        public string SenderId { get; set; }

        /// <inheritdoc />
        [DataMember(Name="subject", Order = 7), Preserve]
        public string Subject { get; set; }

        public override string ToString()
        {
            var output = "";
            output = string.Concat(output, "Code: ", Code, ", ");
            output = string.Concat(output, "Content: ", Content, ", ");
            output = string.Concat(output, "CreateTime: ", CreateTime, ", ");
            output = string.Concat(output, "Id: ", Id, ", ");
            output = string.Concat(output, "Persistent: ", Persistent, ", ");
            output = string.Concat(output, "SenderId: ", SenderId, ", ");
            output = string.Concat(output, "Subject: ", Subject, ", ");
            return output;
        }
    }
}
