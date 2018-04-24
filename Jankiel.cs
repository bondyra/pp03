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
        public int X {get; private set;}
        public int Y {get; private set;}
        public string QueueName {get; private set;}

        private readonly double jankielPlayTime = 3.0d;

        private List<Jankiel> neighbors = new List<Jankiel>();

        public Jankiel(int _x, int _y, string _queueName)
        {
            X = _x;
            Y = _y;
            QueueName = _queueName;
        }

        public void AddNeighbor (Jankiel neighbor)
        {
            this.neighbors.Add(neighbor);
        }

        public void Run()
        {
            System.Console.WriteLine($"{QueueName} started, neighbor count: {this.neighbors.Count}.");
            //uruchom consumera
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                startConsuming(connection, channel);
                //logika algorytmu
                runAlgorithm();
            }
        }

        /*///// logika algorytmu /////*/

        //co robić przy odbiorze wiadomości?
        private EventHandler<BasicDeliverEventArgs> messageHandler ()
        {
            return (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"[x] received in {QueueName} : {message}");
                };
        }

        //logika algorytmu
        private void runAlgorithm()
        {
            sendToAll($"To ja {QueueName}.");

            //by nie zabijac consumera:
            while(true);
        }


        private void runMIS()
        {

        }

        private void play()
        {
            Thread.Sleep((int)this.jankielPlayTime*1000);
        }

        /*///// opakowywacze komunikacji /////*/
        private void sendToAll (string message)
        {
            foreach (var neighbor in neighbors)
            {
                send(neighbor.QueueName, message);
            }
        }

        private void send(string queueName, string message)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
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
                Console.WriteLine($"[x] Sent to {queueName} : {message}");
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
    }
}