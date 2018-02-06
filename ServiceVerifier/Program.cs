using Newtonsoft.Json;
using SharpRaven;
using SharpRaven.Data;
using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.ServiceProcess;
using System.Threading;
using System.Timers;

namespace ServiceVerifier
{
    public class Program
    {
        private static System.Timers.Timer aTimer;
        public static ConfigProperty config;
        public static RavenClient ravenClient;
        public static bool firstTimeFails = false;
        public static Log log = new Log(); 

        static void Main(string[] args)
        {
            try{
                config = JsonConvert.DeserializeObject<ConfigProperty>(File.ReadAllText("config.json"));
                ravenClient = new RavenClient(config.SentryDns);
                MessageBox.Show("The Recovery program have started successfully", "Recovery "+config.ServiceName, MessageBoxButtons.OK);
                aTimer = new System.Timers.Timer(5000);
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
                new ManualResetEvent(false).WaitOne();
            }
            catch(Exception ex)
            {
                log.LogServiceWrite("Exception throw in main(): " + ex.Message,"Error");
            }
            
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            ServiceController service = new ServiceController(config.ServiceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
                
                if (service.Status.Equals(ServiceControllerStatus.Stopped))
                {
                    bool isRestarted = false;
                    log.LogServiceWrite("The service has stopped", "Info");
                    
                    try
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                    }
                    catch (Exception ex)
                    {
                        log.LogServiceWrite("Error in service.Start() " + ex.Message, "Error");
                    }
                    
                    if (service.Status.Equals(ServiceControllerStatus.Running))
                    {
                        log.LogServiceWrite("The service have been restarted ", "Info");
                        isRestarted = true;
                    }
                    if (!firstTimeFails)
                    {
                        ravenClient.Capture(new SentryEvent("The service has stopped at " + e.SignalTime));
                        firstTimeFails = true;
                        SendEmail(isRestarted);
                    }
                }
                else
                {
                    firstTimeFails = false;
                }
            }
            catch (Exception ex)
            {
                log.LogServiceWrite("Error "+ ex.Message + " "+ ex.Source, "Error" );
            }
        }
        
        public static void SendEmail(bool isRestarted)
        {
            try
            {
                SmtpClient client = new SmtpClient
                {
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(config.EmailFrom,config.EmailPassword),
                    Host = config.EmailHost
                };

                //e-mail sender
                MailAddress from = new MailAddress(config.EmailFrom,config.EmailName, System.Text.Encoding.UTF8);
                //destinations for the e-mail message.
                MailAddress to = new MailAddress(config.EmailTo);

                //message content
                MailMessage message = new MailMessage(from, to)
                {
                    Body = "The service "+ config.ServiceName +" has stopped",
                    BodyEncoding = System.Text.Encoding.UTF8,
                    Subject = config.ServiceName +" Service down",
                    SubjectEncoding = System.Text.Encoding.UTF8
                };
                message.Body += isRestarted ?", but it was recovered by the recovery program.\n\n":" and it couldn't be recovered please restart it.\n\n";
                message.Body += GetLastServiceError();

                // Set the method that is called back when the send operation ends.
                client.SendCompleted += new
                SendCompletedEventHandler(SendCompletedCallback);

                // The userState can be any object that allows your callback 
                string userState = "test message1";
                client.SendAsync(message, userState);
                log.LogServiceWrite("The Email was sended to "+ config.EmailTo, "Info");
            }
            catch (Exception e)
            {
                log.LogServiceWrite("Error sending e-mail " + e.Message + "\n Source: " + e.Source, "Error");
            }
        }

        public static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            //Get the unique identifier for this asynchronous operation.

            if (e.Error != null)
            {
                log.LogServiceWrite(("Error sending e-mail "+ e.Error.ToString()), "Error");
            }
            else
            {
                log.LogServiceWrite("Message sent","Info");
            }
        }

        public static string GetLastServiceError()
        {
            EventLog eventLog = new EventLog
            {
                Log = "Application",
                Source = "Application Error"
            };

            for (int index = eventLog.Entries.Count - 1; index > 0; index--)
            {
                var errLastEntry = eventLog.Entries[index];
                if (errLastEntry.EntryType == EventLogEntryType.Error && errLastEntry.Source.Equals(".NET Runtime"))
                {
                    //This is the last entry with Error
                    return "The last error that was registered from " + config.ServiceName + " was:" +
                        "\nEvent id: " + errLastEntry.InstanceId + "\n" + errLastEntry.Source +
                        "\nMachine: " + errLastEntry.MachineName + "\nTime registered: " + errLastEntry.TimeWritten +
                        "\n" + errLastEntry.Message;
                }
            }

            return "";
        }
    }
}
