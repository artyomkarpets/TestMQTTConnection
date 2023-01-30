using System;
using System.Net.Security;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {


            RunInitAsync("192.168.20.151", "MLCL_USER_FQDN");


            RunInitAsync("192.168.20.154", "MLCL_USER_FQDN");

            Console.ReadLine();


        }

        private static void RunInitAsync(string host, string certificateName)
        {
            Console.WriteLine($"Press enter to start connection to {host}");
            Console.ReadLine();
            Task.Run(() =>
            {
                try
                {
                    InitMQTTClient(host, certificateName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed {host}");
                    showAllException(ex);

                }
            });
        }

        public static void showAllException(Exception ex)
        {
            Console.WriteLine(ex.Message);

            if (ex.InnerException == null)
            {
                return;
            }
            showAllException(ex.InnerException);
        }

        private static void InitMQTTClient(string host, string certificateName)
        {
            var certificateCA = GetCACertificate("MLCL_ROOT_CA").FirstOrDefault();



            var certificateClient = GetClientCertificate(certificateName).First();

            if (certificateClient != null)
            {
                //  Console.WriteLine("SubjectName" + certificateClient.SubjectName);
                //  Console.WriteLine("" + certificateClient.IssuerName);
                //  Console.WriteLine("" + certificateClient.IssuerName);
                ;

                foreach (X509Extension extension in certificateClient.Extensions)
                {
                    // Create an AsnEncodedData object using the extensions information.
                    AsnEncodedData asndata = new AsnEncodedData(extension.Oid, extension.RawData);
                    Console.WriteLine("Extension type: {0}", extension.Oid.FriendlyName);
                    Console.WriteLine("Oid value: {0}", asndata.Oid.Value);
                    Console.WriteLine("Raw data length: {0} {1}", asndata.RawData.Length, Environment.NewLine);
                    Console.WriteLine(asndata.Format(true));
                }
            }


            MqttClient client = new MqttClient(host,
                MqttSettings.MQTT_BROKER_DEFAULT_SSL_PORT,
                true, certificateCA
                , certificateClient,
                MqttSslProtocols.TLSv1_2, RemoteCertificateValidationCallback);


            client.MqttMsgPublishReceived += MessageReceived;
            client.Connect("test_client");

            Console.WriteLine("Connected to " + host);


            client.Subscribe(new string[] { "test/test" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });


            int counter = 0;
            while (true)
            {
                var msg = $"host {host} test" + counter;
                byte[] bytes = Encoding.ASCII.GetBytes(msg);
                client.Publish("test/test", bytes);
                counter++;

                Console.WriteLine("Publish: " + msg);
                Thread.Sleep(2000);
            }
        }

        public static X509Certificate2Collection GetClientCertificate(string name)
        {

            // Use the X509Store class to get a handle to the local certificate stores. "My" is the "Personal" store.
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            // Open the store to be able to read from it.
            store.Open(OpenFlags.ReadOnly);

            // Use the X509Certificate2Collection class to get a list of certificates that match our criteria (in this case, we should only pull back one).
            X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindBySubjectName, name, true);

            // Associate the certificates with the request
            return collection;
        }


        public static X509Certificate2Collection GetCACertificate(string name)
        {


            // Use the X509Store class to get a handle to the local certificate stores. "My" is the "Personal" store.
            X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);

            // Open the store to be able to read from it.
            store.Open(OpenFlags.ReadOnly);

            // Use the X509Certificate2Collection class to get a list of certificates that match our criteria (in this case, we should only pull back one).
            X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindBySubjectName, name, true);

            // Associate the certificates with the request
            return collection;
        }
        public static void MessageReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string msg = Encoding.ASCII.GetString(e.Message);

            Console.WriteLine("Recive: " + msg);


        }

        public static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // logic for validation here
            return true;
        }
    }
}