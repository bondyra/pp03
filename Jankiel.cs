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
        private static string noB1 = "|no B exchange 1";
        private static string B1 = "|B exchange 1";
        private static string noB2 = "|no B exchange 2";
        private static string B2 = "|B exchange 2";
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
            var factory = new ConnectionFactory() {HostName = "localhost"};
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                startConsuming(connection, channel);
                //logika algorytmu
                runAlgorithm();
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
                if (message.Contains(noB1))
                {
                    var roundNumber = getRoundNumberFromMessage(message);
                    monitor.DecreaseCounter(false, QueueName, roundNumber);
                }
                else if (message.Contains(B1))
                {
                    var roundNumber = getRoundNumberFromMessage(message);
                    monitor.DecreaseCounter(true, QueueName, roundNumber);
                }
                else if (message.Contains(B2))
                {
                    var roundNumber = getRoundNumberFromMessage(message);
                    monitor2.DecreaseCounter(true, QueueName, roundNumber);
                }
                else if (message.Contains(noB2))
                {
                    var roundNumber = getRoundNumberFromMessage(message);
                    monitor2.DecreaseCounter(false, QueueName, roundNumber);
                }
                else if (message == endPlay)
                {
                    monitor.SetNeighborCount(--neighborCount);
                    monitor2.SetNeighborCount(--neighborCount);
                    playMonitor.NotifyEndPlay();
                }
            };
        }

        //logika algorytmu
        public void runAlgorithm()
        {
            while (!playedAlready)
            {
                bool anyonePlaying = runMIS();
                if (!anyonePlaying)
                {
                    logEvent($"{QueueName} gra.");
                    Thread.Sleep(jankielPlayTime);
                    playedAlready = true;
                    logEvent($"{QueueName} skonczyl grac.");
                    sendToAll(endPlay);
                    return;
                }
                else
                {
                    logEvent($"{QueueName} czeka na koniec grania.");
                    playMonitor.WaitForEndPlaying();
                    logEvent($"{QueueName} skonczyl czekac na koniec grania.");
                }
            }
        }

        private static int intLog(int k)
        {
            return (int) Math.Log(k, 2);
        }

        private static bool binom(double probability)
        {
            double val = r.NextDouble();
            return val < probability;
        }

        private bool runMIS()
        {
            logEvent($"{QueueName} Zaczyna ustalac {neighborCount}");
            int v;
            bool wasB;
            bool isSet = false, ret = false;
            int roundNumber = 0;
            for (int i = 0; i < intLog(D); i++)
            {
                for (int j = 0; j < M * intLog(n); j++)
                {
                    monitor.setRoundNumber(QueueName, roundNumber);
                    logEvent($"{QueueName} zaczyna runde {roundNumber}");
                    if (isSet)
                    {
                        sendToAll(roundNumber + noB1);
                        monitor.WaitIfNecessary(QueueName, roundNumber);
                        sendToAll(roundNumber + noB2);
                        monitor2.WaitIfNecessary(QueueName, roundNumber);
                    }
                    else
                    {
                        // (1st exchange)
                        v = 0;
                        bool success = binom(1d / (Math.Pow(2, (double) (intLog(D) - i))));
                        // broadcast B to all neighbors
                        sendToAll(success ? roundNumber + B1 : roundNumber + noB1);
                        // logEvent($"{QueueName} waitIfNecessary exchange 1");
                        wasB = monitor.WaitIfNecessary(QueueName, roundNumber);
                        // v ← 1
                        if (success) v = 1;
                        // if received a message B then v ← 0;
                        if (wasB) v = 0;
                        // (2nd exchange)
                        // if v = 1 then
                        sendToAll(v == 1 ? roundNumber + B2 : roundNumber + noB2);
                        //logEvent($"{QueueName} waitIfNecessary exchange 2");
                        wasB = monitor2.WaitIfNecessary(QueueName, roundNumber);
                        if (v == 1)
                        {
                            logEvent($"{QueueName} Wygral!");
                            //mamy gwarancje ze nikt nie gra
                            ret = false;
                            isSet = true;
                        }
                        else if (wasB)
                        {
                            logEvent($"{QueueName} Przegral!");
                            //ktos inny gra
                            ret = true;
                            isSet = true;
                        }
                    }

                    roundNumber++;
                }
            }

            logEvent($"{QueueName} Skonczyl ustalac");
            return ret;
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
            var factory = new ConnectionFactory() {HostName = "localhost"};
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

        private int getRoundNumberFromMessage(string message)
        {
            var index = message.IndexOf("|");
            var roundNumber = message.Substring(0, index);
            return Int32.Parse(roundNumber);
        }

        public static void logEvent(String eventText)
        {
            System.Console.WriteLine(System.DateTime.Now + " " + eventText);
        }
    }
}