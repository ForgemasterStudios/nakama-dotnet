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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nakama.Tests.Socket
{
    // NOTE Test name patterns are: MethodName_StateUnderTest_ExpectedBehavior
    public class WebSocketUserStatusTest : IAsyncLifetime
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        private IClient _client;
        private readonly ISocket _socket;

        public WebSocketUserStatusTest()
        {
            _client = ClientUtil.FromSettingsFile();
            _socket = Nakama.Socket.From(_client);
        }

        [Fact]
        public async void FollowUsers_NoUsers_AnotherUser()
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);
            _socket.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            _socket.ReceivedError += e => completer.TrySetException(e);
            await _socket.ConnectAsync(session1);
            await _socket.FollowUsersAsync(new[] {session2.UserId});

            var socket = Nakama.Socket.From(_client);
            await socket.ConnectAsync(session2);
            await socket.UpdateStatusAsync("new status change");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session2.UserId));
        }

        [Fact]
        public async void FollowUsers_NoUsers_AnotherUserByUsername()
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);
            _socket.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            _socket.ReceivedError += e => completer.TrySetException(e);
            await _socket.ConnectAsync(session1);
            await _socket.FollowUsersAsync(new string[] { }, new[] {session2.Username});

            var socket = Nakama.Socket.From(_client);
            await socket.ConnectAsync(session2);
            await socket.UpdateStatusAsync("new status change");

            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session2.UserId));
        }

        [Fact]
        public async void FollowUsers_NoUsers_FollowedSelf()
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);
            await _socket.ConnectAsync(session);

            var statuses = await _socket.FollowUsersAsync(new[] {session.UserId});
            Assert.NotNull(statuses);
            Assert.Empty(statuses.Presences);
        }

        [Fact]
        public async void FollowUsers_NoUsers_UserJoinsAndLeaves()
        {
            var id = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id);
            var session2 = await _client.AuthenticateCustomAsync(id + "a");

            var completer1 = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer1.TrySetCanceled());
            canceller.CancelAfter(Timeout);
            _socket.ReceivedStatusPresence += statuses => completer1.TrySetResult(statuses);
            _socket.ReceivedError += e => completer1.TrySetException(e);
            await _socket.ConnectAsync(session1);
            await _socket.FollowUsersAsync(new[] {session2.UserId});

            // Second user comes online and sets status.
            var socket = Nakama.Socket.From(_client);
            await socket.ConnectAsync(session2);
            await socket.UpdateStatusAsync("new status change");

            var result1 = await completer1.Task;
            Assert.NotNull(result1);
            Assert.Empty(result1.Leaves);
            Assert.Contains(result1.Joins, joined => joined.UserId.Equals(session2.UserId));

            var completer2 = new TaskCompletionSource<IStatusPresenceEvent>();
            _socket.ReceivedStatusPresence += statuses => completer2.SetResult(statuses);

            // Second user drops offline.
            await socket.CloseAsync();
            var result2 = await completer2.Task;
            Assert.NotNull(result2);
            Assert.Empty(result2.Joins);
            Assert.Contains(result2.Leaves, left => left.UserId.Equals(session2.UserId));
        }

        [Fact]
        public async void FollowUsers_TwoSessions_HasTwoStatuses()
        {
            var id1 = Guid.NewGuid().ToString();
            var session1 = await _client.AuthenticateCustomAsync(id1);
            await _socket.ConnectAsync(session1);

            var id2 = Guid.NewGuid().ToString();
            var session2 = await _client.AuthenticateCustomAsync(id2);
            var socket1 = Nakama.Socket.From(_client);
            //socket1.ReceivedError
            await socket1.ConnectAsync(session2);
            var socket2 = Nakama.Socket.From(_client);
            //socket2.ReceivedError
            await socket2.ConnectAsync(session2);

            // Both sockets for single user set statuses.
            const string status1 = "user 2 socket 1 status.";
            await socket1.UpdateStatusAsync(status1);
            const string status2 = "user 2 socket 2 status.";
            await socket2.UpdateStatusAsync(status2);

            var statuses = await _socket.FollowUsersAsync(new[] {session2.UserId});
            Assert.NotNull(statuses);
            Assert.Contains(statuses.Presences,
                presence => presence.Status.Equals(status1) || presence.Status.Equals(status2));

            await socket2.CloseAsync();
        }

        [Fact]
        public async void FollowUsers_TwoUsers_ThirdUserFollowsBoth()
        {
            var id1 = Guid.NewGuid().ToString();
            var socket1 = Nakama.Socket.From(_client);
            //socket1.ReceivedError
            var session1 = await _client.AuthenticateCustomAsync(id1);

            var id2 = Guid.NewGuid().ToString();
            var socket2 = Nakama.Socket.From(_client);
            //socket2.ReceivedError
            var session2 = await _client.AuthenticateCustomAsync(id2);

            var id3 = Guid.NewGuid().ToString();
            var socket3 = Nakama.Socket.From(_client);
            //socket3.ReceivedError
            var session3 = await _client.AuthenticateCustomAsync(id3);

            // Two users come online. Each publishes a status.
            await socket1.ConnectAsync(session1);
            await socket1.UpdateStatusAsync("user 1 status.");
            await socket2.ConnectAsync(session2);
            await socket2.UpdateStatusAsync("user 2 status.");

            // Third user comes online and follows both users.
            await socket3.ConnectAsync(session3);
            var statuses = await socket3.FollowUsersAsync(new[] {session1.UserId, session2.UserId});
            Assert.NotNull(statuses);
            Assert.NotEmpty(statuses.Presences);
            Assert.Contains(statuses.Presences,
                presence => presence.UserId.Equals(session1.UserId) || presence.UserId.Equals(session2.UserId));

            // Dispose
            await socket1.CloseAsync();
            await socket2.CloseAsync();
            await socket3.CloseAsync();
        }

        [Fact]
        public async void UpdateStatus_NoStatus_HasStatus()
        {
            var id = Guid.NewGuid().ToString();
            var session = await _client.AuthenticateCustomAsync(id);

            var completer = new TaskCompletionSource<IStatusPresenceEvent>();
            var canceller = new CancellationTokenSource();
            canceller.Token.Register(() => completer.TrySetCanceled());
            canceller.CancelAfter(Timeout);
            _socket.ReceivedStatusPresence += statuses => completer.SetResult(statuses);
            _socket.ReceivedError += e => completer.TrySetException(e);
            await _socket.ConnectAsync(session);

            await _socket.UpdateStatusAsync("super status change!");
            var result = await completer.Task;
            Assert.NotNull(result);
            Assert.Contains(result.Joins, joined => joined.UserId.Equals(session.UserId));
        }

        [Fact]
        public async void TestFollowMassiveNumberOfUsers()
        {
            const int numFollowees = 2000;

            var followerId = Guid.NewGuid().ToString();

            System.Console.WriteLine("Authenticating user...");
            var followerSession = await _client.AuthenticateCustomAsync(followerId);

            await _socket.ConnectAsync(followerSession);

            var authTasks = new List<Task<ISession>>();

            System.Console.WriteLine("Authenticating followees...");

            for (int i = 0; i < numFollowees; i++)
            {
                var followeeId = Guid.NewGuid().ToString();
                authTasks.Add(_client.AuthenticateCustomAsync(followeeId));
            }

            Task.WaitAll(authTasks.ToArray());

            System.Console.WriteLine("Done authenticating followees...");
            System.Console.WriteLine("Getting IApiUsers...");

            IEnumerable<IApiUser> allFollowees = authTasks.Select(task =>
                new ApiUser{Id = task.Result.UserId}
            );

            System.Console.WriteLine("Done getting IApiUsers...");

            var connectTasks = new List<Task>();

            IStatus statuses = null;

            try
            {
                statuses = await _socket.FollowUsersAsync(allFollowees);
            }
            catch (ApiResponseException e)
            {
                throw e;
            }

            System.Console.WriteLine("Done following users...");

            Assert.Equal(numFollowees, statuses.Presences.Count());
        }

        Task IAsyncLifetime.InitializeAsync()
        {
            return Task.CompletedTask;
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return _socket.CloseAsync();
        }
    }
}
