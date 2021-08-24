using DevFreela.Payments.API.Model;
using DevFreela.Payments.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DevFreela.Payments.API.Consumers
{

    /// <summary>
    /// Classe que ficara rodando (processo interno do aspnet.core) esperando a mensagem e disparando evento interno para processa-la.
    /// Herda da classe abstrata BackgroundService
    /// </summary>
    public class ProcessPaymentConsumer : BackgroundService
    {
        // cria conexao
        private readonly IConnection _connection; // connection com RabbitMQ, vai ficar escutando a mensagem
        private readonly IModel _channel; // responsável por armazenar o canal, a sessão
        private readonly IServiceProvider _serviceProvider; // necessário quando for realizar acesso a um serviço que esta injetado na aplicação (tipo ciclo vida scoped)
        private const string QUEUE = "Payments";
        private const string PAYMENT_APPROVED_QUEUE = "PaymentsApproved";

        public ProcessPaymentConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory
            {
                HostName = "localhost"                

            };

            _connection = factory.CreateConnection(); // atribuindo o connection
            _channel = _connection.CreateModel(); // atribuindo o channel

            // declarando a fila, para caso mensagem não exista ela será criada.
            _channel.QueueDeclare(
                queue: QUEUE,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
                );

            // define que a mensagem foi processada
            _channel.QueueDeclare(
               queue: PAYMENT_APPROVED_QUEUE,
               durable: false,
               exclusive: false,
               autoDelete: false,
               arguments: null
               );

        }
        /// <summary>
        /// Processando a mensagem
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // permite definir evento  em caso  em que eu receba uma mensagem
            var consumer = new EventingBasicConsumer(_channel);

            //toda vez que receber a mensagem vai ser disparado evento received.
            //acessa novo evento, eventArgs para ver o conteudo da mensagem
            consumer.Received += (sender, eventArgs) =>
             {
                 //obter o array de bytes
                 var byteArray = eventArgs.Body.ToArray();
                 var paymentInfoJson = Encoding.UTF8.GetString(byteArray);
                 //deserializando paymentInfoJson .
                 var paymentInfo = JsonSerializer.Deserialize<PaymentInfoInputModel>(paymentInfoJson);

                 // Chama o metódo para processar o pagamento
                 ProcessPayment(paymentInfo);                 

                 // Apos processar pagamento, mandar mensagem, republicar para uma outra fila, se um p'rocesso tiver interessado vai la e cata a mensagem
                 var paymentApproved = new PaymentApprovedIntegrationEvent(paymentInfo.IdProject);
                 //cria o json
                 var paymentApprovedJson = JsonSerializer.Serialize(paymentApproved);
                 // passa para os bytes pq o broker aceita bytes como mensagem
                 var paymentApprovedBytes = Encoding.UTF8.GetBytes(paymentInfoJson);

                 // utilizando metodo basicpublish, recebendo a fila no routingKey
                 _channel.BasicPublish(
                     exchange: "",
                     routingKey: PAYMENT_APPROVED_QUEUE,
                     basicProperties: null,
                     body: paymentApprovedBytes

                     );

                 // informa ao Broker (Messagem Broker que a mensagem foi recebida, passando informação de entrega da mensagem.)
                 _channel.BasicAck(eventArgs.DeliveryTag, false);

             };

            // utilizando o consumo de mensagem, escutando a fila QUEUE e passa qual o consumo que ela ta configurada.
            _channel.BasicConsume(QUEUE, false, consumer);

            return Task.CompletedTask;

        }

        /// <summary>
        /// Processando o pagamento
        /// </summary>
        /// <param name="paymentInfo"></param>
        public void ProcessPayment(PaymentInfoInputModel paymentInfo)
        {
            // utilizando o serviceProvider, criando uma instância

            using (var scope = _serviceProvider.CreateScope())
            {
                // acessa o IPaymentService pois será executado de maneira indefinida.
                var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                //processa o pagamento
                paymentService.Process(paymentInfo);
            }
        }
    }
}
