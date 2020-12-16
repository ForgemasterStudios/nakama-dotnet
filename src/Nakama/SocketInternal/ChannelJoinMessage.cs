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
    /// Send a channel join message to the server.
    /// </summary>
    [DataContract]
    public class ChannelJoinMessage
    {
        [DataMember(Name="hidden", Order = 4), Preserve]
        public bool Hidden { get; set; }

        [DataMember(Name="persistence", Order = 3), Preserve]
        public bool Persistence { get; set; }

        [DataMember(Name="target", Order = 1), Preserve]
        public string Target { get; set; }

        [DataMember(Name="type", Order = 2), Preserve]
        public int Type { get; set; }

        public override string ToString()
        {
            return $"ChannelJoinMessage(Hidden={Hidden}, Persistence={Persistence}, Target='{Target}', Type={Type})";
        }
    }
}
