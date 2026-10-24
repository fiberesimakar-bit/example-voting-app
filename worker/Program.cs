
using System;
using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Npgsql;
using StackExchange.Redis;

namespace Worker
{
    public class Program
    {
        public static int Main(string[] args)
        {
            NpgsqlConnection pgsql = null;
            NpgsqlCommand keepAliveCommand = null;
            ConnectionMultiplexer redisConn = null;

            try
            {
                var postgresPassword =
                    Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

                if (string.IsNullOrWhiteSpace(postgresPassword))
                {
                    throw new InvalidOperationException(
                        "POSTGRES_PASSWORD environment variable is missing.");
                }

                var connectionString =
                    new NpgsqlConnectionStringBuilder
                    {
                        Host = "db",
                        Port = 5432,
                        Database = "postgres",
                        Username = "postgres",
                        Password = postgresPassword
                    }.ConnectionString;

                pgsql = OpenDbConnection(connectionString);
                keepAliveCommand = CreateKeepAliveCommand(pgsql);

                redisConn = OpenRedisConnection("redis");
                var redis = redisConn.GetDatabase();

                var definition = new { vote = "", voter_id = "" };

                while (true)
                {
                    Thread.Sleep(100);

                    // Reconnect to Redis when its connection is lost.
                    if (!redisConn.IsConnected)
                    {
                        Console.WriteLine("Reconnecting Redis");

                        redisConn.Dispose();
                        redisConn = OpenRedisConnection("redis");
                        redis = redisConn.GetDatabase();
                    }

                    string json;

                    try
                    {
                        json = redis.ListLeftPopAsync("votes").Result;
                    }
                    catch (RedisException ex)
                    {
                        Console.Error.WriteLine(
                            $"Redis operation failed: {ex.Message}");

                        redisConn.Dispose();
                        redisConn = OpenRedisConnection("redis");
                        redis = redisConn.GetDatabase();
                        continue;
                    }

                    if (json == null)
                    {
                        // Reconnect to PostgreSQL if its connection closed.
                        if (pgsql.State != ConnectionState.Open)
                        {
                            Console.WriteLine("Reconnecting DB");

                            keepAliveCommand.Dispose();
                            pgsql.Dispose();

                            pgsql = OpenDbConnection(connectionString);
                            keepAliveCommand =
                                CreateKeepAliveCommand(pgsql);
                        }
                        else
                        {
                            try
                            {
                                keepAliveCommand.ExecuteNonQuery();
                            }
                            catch (DbException ex)
                            {
                                Console.Error.WriteLine(
                                    $"Database keep-alive failed: {ex.Message}");

                                keepAliveCommand.Dispose();
                                pgsql.Dispose();

                                pgsql = OpenDbConnection(connectionString);
                                keepAliveCommand =
                                    CreateKeepAliveCommand(pgsql);
                            }
                        }

                        continue;
                    }

                    var vote =
                        JsonConvert.DeserializeAnonymousType(json, definition);

                    if (vote == null ||
                        string.IsNullOrWhiteSpace(vote.voter_id) ||
                        string.IsNullOrWhiteSpace(vote.vote))
                    {
                        Console.Error.WriteLine(
                            "Skipping invalid vote message.");
                        continue;
                    }

                    Console.WriteLine(
                        $"Processing vote for '{vote.vote}' by '{vote.voter_id}'");

                    // Keep retrying this vote if PostgreSQL temporarily fails.
                    while (true)
                    {
                        try
                        {
                            if (pgsql.State != ConnectionState.Open)
                            {
                                keepAliveCommand.Dispose();
                                pgsql.Dispose();

                                pgsql = OpenDbConnection(connectionString);
                                keepAliveCommand =
                                    CreateKeepAliveCommand(pgsql);
                            }

                            UpdateVote(
                                pgsql,
                                vote.voter_id,
                                vote.vote);

                            break;
                        }
                        catch (DbException ex)
                        {
                            Console.Error.WriteLine(
                                $"Vote database operation failed: {ex.Message}");

                            keepAliveCommand.Dispose();
                            pgsql.Dispose();

                            pgsql = OpenDbConnection(connectionString);
                            keepAliveCommand =
                                CreateKeepAliveCommand(pgsql);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Do not print the database password.
                Console.Error.WriteLine($"Worker failed: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
            finally
            {
                keepAliveCommand?.Dispose();
                pgsql?.Dispose();
                redisConn?.Dispose();
            }
        }

        private static NpgsqlConnection OpenDbConnection(
            string connectionString)
        {
            while (true)
            {
                var connection = new NpgsqlConnection(connectionString);

                try
                {
                    connection.Open();

                    using var command = connection.CreateCommand();

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS votes (
                            id VARCHAR(255) NOT NULL UNIQUE,
                            vote VARCHAR(255) NOT NULL
                        )";

                    command.ExecuteNonQuery();

                    Console.WriteLine(
                        "Connected to db; votes table is ready.");

                    return connection;
                }
                catch (SocketException ex)
                {
                    connection.Dispose();

                    Console.Error.WriteLine(
                        $"Waiting for db: network error: {ex.Message}");
                }
                catch (DbException ex)
                {
                    connection.Dispose();

                    Console.Error.WriteLine(
                        $"Waiting for db: database error: {ex.Message}");

                    // Invalid credentials will not be fixed by retrying.
                    if (ex is NpgsqlException npgsqlEx &&
                        npgsqlEx.SqlState == "28P01")
                    {
                        throw;
                    }
                }

                Thread.Sleep(3000);
            }
        }

        private static NpgsqlCommand CreateKeepAliveCommand(
            NpgsqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            return command;
        }

        private static ConnectionMultiplexer OpenRedisConnection(
            string hostname)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine($"Connecting to Redis at {hostname}");

                    var connection =
                        ConnectionMultiplexer.Connect(hostname);

                    Console.WriteLine("Connected to Redis");
                    return connection;
                }
                catch (RedisConnectionException ex)
                {
                    Console.Error.WriteLine(
                        $"Waiting for Redis: {ex.Message}");

                    Thread.Sleep(3000);
                }
            }
        }

        private static void UpdateVote(
            NpgsqlConnection connection,
            string voterId,
            string vote)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO votes (id, vote)
                VALUES (@id, @vote)
                ON CONFLICT (id)
                DO UPDATE SET vote = EXCLUDED.vote";

            command.Parameters.AddWithValue("@id", voterId);
            command.Parameters.AddWithValue("@vote", vote);

            command.ExecuteNonQuery();
        }
    }
}
