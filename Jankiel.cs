using System;
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

        private List<Jankiel> neighbors = new List<Jankiel>();

        public Jankiel(int _x, int _y, string _queueName)
        {
            X = _x;
            Y = _y;
            QueueName = _queueName;
        }

        public void addNeighbor (Jankiel neighbor)
        {
            this.neighbors.Add(neighbor);
        }

        //logika algorytmu
        private void logika ()
        {
            sendToAll($"To ja {QueueName}.");

            //by nie zabijac consumera:
            while(true);
        }


        public void run()
        {
            System.Console.WriteLine($"{QueueName} started, neighbor count: {this.neighbors.Count}.");
            //uruchom consumera
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                    channel.QueueDeclare(queue: QueueName,
                                        durable: false,
                                        exclusive: false,
                                        autoDelete: false,
                                        arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += messageHandler();

                    channel.BasicConsume(queue: QueueName,
                                        autoAck: true,
                                        consumer: consumer);

            //logika algorytmu (?)
            logika();
            }
        }

        //wysylka wiadomosci
        private void sendToAll (string message)
        {
            foreach (var neighbor in neighbors)
            {
                send(neighbor.QueueName, message);
            }
        }

        public void send(string queueName, string message)
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

        //co robic przy odbiorze wiadomosci?
        private EventHandler<BasicDeliverEventArgs> messageHandler ()
        {
            return (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"[x] received in {QueueName} : {message}");
                };
        }
    }
}