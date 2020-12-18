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
    /// <summary>
    /// Update the status of the current user.
    /// </summary>
    [DataContract]
    public class StatusUpdateMessage
    {
        [IgnoreDataMember]
        public string Status
        {
            get => _statusValue.Value ?? _status;
            set
            {
                _status = value;
                _statusValue.Value = value;
            }
        }

        [DataMember(Name="status"), Preserve]
        public string _status;

        [Exclude, DataMember(Order = 1), Preserve]
        public StringValue _statusValue;

        public override string ToString()
        {
            return $"StatusUpdateMessage(Status='{Status}')";
        }
    }
}
