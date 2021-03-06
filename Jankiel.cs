using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JankielsProj
{
    public class Jankiel
    {
        private static string noB1 = "no B exchange 1";
        private static string B1 = "B exchange 1";
        private static string noB2 = "no B exchange 2";
        private static string B2 = "B exchange 2";
        private static string endPlay = "me play";
        private static int M = 10;
        private int n;
        private object lockobj = new object();
        private static Random r = new Random(997);

        public bool playedAlready = false;

        public int neighborCount;

        public JankielMonitor monitor = new JankielMonitor();
        public JankielMonitor monitor2 = new JankielMonitor();
        private JankielPlayMonitor playMonitor = new JankielPlayMonitor();

        public int X { get; private set; }
        public int Y { get; private set; }
        public int D { get; set; }
        public string QueueName { get; private set; }

        private readonly int jankielPlayTime = 3000;

        public List<Jankiel> Neighbors = new List<Jankiel>();
        private int playedCount;

        public Jankiel(int _x, int _y, string _queueName, int _n)
        {
            X = _x;
            Y = _y;
            QueueName = _queueName;
            n = _n;
        }

        public void AddNeighbor(Jankiel neighbor)
        {
            this.Neighbors.Add(neighbor);
        }

        public void Run()
        {

            System.Console.WriteLine($"{QueueName} started, neighbor count: {this.neighborCount}.");
            //uruchom consumera
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                startConsuming(connection, channel);
                //logika algorytmu
                runAlgorithm();
                channel.QueuePurge(QueueName);
            }
        }

        /*///// logika algorytmu /////*/

        //co robić przy odbiorze wiadomości?
        private EventHandler<BasicDeliverEventArgs> messageHandler()
        {
            return (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    if (message == noB1)
                    {
                        monitor.DecreaseCounter(false, QueueName);
                    }
                    else if (message == B1)
                    {
                        monitor.DecreaseCounter(true, QueueName);
                    }
                    else if (message == B2)
                    {
                        monitor2.DecreaseCounter(true, QueueName);
                    }
                    else if (message == noB2)
                    {
                        monitor2.DecreaseCounter(false, QueueName);
                    }
                    else if (message == endPlay)
                    {
                        lock (lockobj)
                        {
                            playedCount++;

                        }
                    }
                };
        }

        //logika algorytmu
        public void runAlgorithm()
        {

            bool anyonePlaying = runMIS();
            if (!anyonePlaying)
            {
                logEvent($"{QueueName} gra.");
                sendToAll(endPlay);
                playedAlready = true;
                return;
            }
            else
            {
                //logEvent($"{QueueName} czeka na koniec grania {anyonePlaying}.");
                Thread.Sleep(3000);
                lock (lockobj)
                {
                    neighborCount -= playedCount;
                    playedCount = 0;
                }
                //logEvent($"{QueueName}: set neighbor count 2 to {neighborCount}");
                monitor.SetNeighborCount(neighborCount);
                monitor2.SetNeighborCount(neighborCount);

                //logEvent($"{QueueName} skonczyl czekac na koniec grania {anyonePlaying}.");
            }


        }

        private static int intLog(int k)
        {
            return (int)Math.Log(k, 2);
        }

        private static bool binom(double probability)
        {
            double val = r.NextDouble();
            return val < probability;
        }

        private bool runMIS()
        {
            int v; bool wasB;
            bool isSet = false, ret = false;
            if (neighborCount == 0)
                return false;
            for (int i = 0; i < intLog(D); i++)
            {
                for (int j = 0; j < M * intLog(n); j++)
                {
                    if (isSet)
                    {
                        sendToAll(noB1);
                        wasB = monitor.WaitIfNecessary(QueueName);
                        sendToAll(noB2);
                        wasB = monitor2.WaitIfNecessary(QueueName);
                    }
                    else
                    {
                        // (1st exchange)
                        v = 0;
                        bool success = binom(1d / (Math.Pow(2, (double)(intLog(D) - i))));
                        // broadcast B to all neighbors
                        sendToAll(success ? B1 : noB1);
                        // logEvent($"{QueueName} waitIfNecessary exchange 1");
                        wasB = monitor.WaitIfNecessary(QueueName);
                        // v ← 1
                        if (success) v = 1;
                        // if received a message B then v ← 0;
                        if (wasB) v = 0;
                        // (2nd exchange)
                        // if v = 1 then
                        sendToAll(v == 1 ? B2 : noB2);
                        //logEvent($"{QueueName} waitIfNecessary exchange 2");
                        wasB = monitor2.WaitIfNecessary(QueueName);
                        if (v == 1)
                        {//mamy gwarancje ze nikt nie gra
                            isSet = true;
                        }
                        else if (wasB)
                        {//ktos inny gra
                            isSet = true;
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }

        private void play()
        {
            Thread.Sleep((int)this.jankielPlayTime * 1000);
        }

        /*///// opakowywacze komunikacji /////*/
        private void sendToAll(string message)
        {
            foreach (var neighbor in Neighbors)
            {
                send(neighbor.QueueName, message);
            }
        }

        private void send(string queueName, string message)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queueName,
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "",
                                    routingKey: queueName,
                                    basicProperties: null,
                                    body: body);
                //logEvent($"[{this.QueueName}] wysyla do {queueName} : {message}");
            }
        }

        private void startConsuming(IConnection connection, IModel model)
        {
            model.QueueDeclare(queue: QueueName,
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

            var consumer = new EventingBasicConsumer(model);
            consumer.Received += messageHandler();

            model.BasicConsume(queue: QueueName,
                                autoAck: true,
                                consumer: consumer);
        }

        public static void logEvent(String eventText)
        {
            System.Console.WriteLine(System.DateTime.Now + " " + eventText);
        }
    }
}