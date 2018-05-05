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
        private static string no_B_first_exchange = "noB1";
        private static string B_first_exchange = "B1";
        private static string no_B_second_exchange = "noB2";
        private static string B_second_exchange = "B2";
        private static string endPlay = "played";
        private object lockobj = new object(); //synchronizacja playedCount
        private static Random r = new Random(997); //musi byc statyczne

        public bool playedAlready = false;

        public int neighborCount;

        public JankielMonitor firstExchangeMonitor = new JankielMonitor();
        public JankielMonitor secondExchangeMonitor = new JankielMonitor();

        public int X { get; private set; }
        public int Y { get; private set; }
        public int D { get; set; }//max stopien w grafie
        public int N { get; set; } //liczba wierzcholkow
        private static int M = 10;
        public string QueueName { get; private set; }

        private readonly int jankielPlayTime = 3000;

        public List<Jankiel> Neighbors = new List<Jankiel>();

        private int playedCount;

        public Jankiel(int _x, int _y, string _queueName, int _n)
        {
            X = _x;
            Y = _y;
            QueueName = _queueName;
            N = _n;
        }

        public void AddNeighbor(Jankiel neighbor)
        {
            this.Neighbors.Add(neighbor);
        }

        public void Run()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                //uruchom consumera kolejki
                startConsuming(connection, channel);
                //
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
                    if (message == no_B_first_exchange)
                    {
                        firstExchangeMonitor.DecreaseCounter(false);
                    }
                    else if (message == B_first_exchange)
                    {
                        firstExchangeMonitor.DecreaseCounter(true);
                    }
                    else if (message == B_second_exchange)
                    {
                        secondExchangeMonitor.DecreaseCounter(true);
                    }
                    else if (message == no_B_second_exchange)
                    {
                        secondExchangeMonitor.DecreaseCounter(false);
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
            if (runMIS())
            {
                logEvent($"{QueueName} gra.");
                sendToAll(endPlay);
                playedAlready = true;
                return;
            }
            else
            {
                logEvent($"{QueueName} czeka.");
                Thread.Sleep(jankielPlayTime);
                lock (lockobj)
                {
                    neighborCount -= playedCount;
                    playedCount = 0;
                }
                //ustawienie liczby sąsiadów którzy nie grali na kolejną rundę
                firstExchangeMonitor.SetNeighborCount(neighborCount);
                secondExchangeMonitor.SetNeighborCount(neighborCount);
                logEvent($"{QueueName} skończył czekać.");
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
            //odpalane w jednej rundzie, procedura ustalania statusu przez jankiela
            //zwraca true == moze grac, false == nie moze
            int v; bool wasB;
            bool isSet = false, canPlay = false;
            if (neighborCount == 0) //nie ma sąsiadów == można grać
                return true;
            for (int i = 0; i < intLog(D); i++)
            {
                for (int j = 0; j < M * intLog(N); j++)
                {         
                    if (isSet)
                    {
                        //biernie przesyłaj swój ustalony status do końca pętli
                        sendToAll(no_B_first_exchange);
                        wasB = firstExchangeMonitor.WaitIfNecessary();
                        sendToAll(no_B_second_exchange);
                        wasB = secondExchangeMonitor.WaitIfNecessary();
                    }
                    else
                    {
                        //ustalanie statusu, logika zbieżna z algorytmem MIS z wykładu
                        v = 0;
                        bool success = RandomProvider.Binom(1d / (Math.Pow(2, (double)(intLog(D) - i))));
                        sendToAll(success ? B_first_exchange : no_B_first_exchange);
                        //odbierz wszystkie wiadomości od sąsiadów
                        wasB = firstExchangeMonitor.WaitIfNecessary();
                        if (success) v = 1;
                        if (wasB) v = 0;
                        sendToAll(v == 1 ? B_second_exchange : no_B_second_exchange);
                        //odbierz wszystkie wiadomości od sąsiadów
                        wasB = secondExchangeMonitor.WaitIfNecessary();

                        if (v == 1)
                        {//mamy gwarancję że nikt nie gra, ustalamy status jako canPlay
                            isSet = true;
                            canPlay = true;
                        }
                        else if (wasB)
                        {//ktoś inny gra, ustalamy status jako !canPlay
                            isSet = true;
                            canPlay = false;
                        }
                    }
                }
            }
            return canPlay;
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
            //wyczyść kolejkę
            model.QueuePurge(QueueName);

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