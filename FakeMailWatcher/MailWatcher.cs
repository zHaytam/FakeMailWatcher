using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using HtmlAgilityPack;

namespace FakeMailWatcher
{
    public class MailWatcher : IDisposable
    {

        // Properties
        public Uri Uri { get; private set; }
        public List<Mail> ReceivedMails { get; private set; }
        public string SpecificFromEmail { get; set; }
        public int Interval
        {
            get => _interval;
            set
            {
                _interval = value;
                _timer.Change(0, _interval);
            }
        }
        public bool IsWatching { get; private set; }


        // Fields
        private const string MailIdPattern = @"\/inbox\/[^\/]+\/[^\/]+\/message-([^ \/]+)\/";
        private const string MailFromPattern = @"[^<]+ &lt;([^>]+)&gt;";
        private HttpClient _httpClient;
        private int _interval;
        private Timer _timer;
        private Action<MailWatcher, Mail> _mailReceivedAction;


        /// <summary>
        /// Initializes a new instance of the MailWatcher class
        /// </summary>
        /// <param name="domain">The email's domain (one of FakeMailGenerator's domains).</param>
        /// <param name="name">The email's name.</param>
        /// <param name="fromEmail">A specific email address to receive from, leave empty or null to receive from any email address.</param>
        /// <param name="interval">The time interval between checkings</param>
        public MailWatcher(string domain, string name, string fromEmail = "", int interval = 5000)
        {
            _httpClient = new HttpClient();
            _interval = interval;
            _timer = new Timer(Time_Callback, null, Timeout.Infinite, Timeout.Infinite);

            Uri = new Uri($"http://www.fakemailgenerator.com/inbox/{domain}/{name}");
            ReceivedMails = new List<Mail>();
            SpecificFromEmail = fromEmail;
        }

        /// <summary>
        /// Sets a <see cref="Action"/> to call everytime a mail is received
        /// </summary>
        /// <param name="mailReceivedAction"></param>
        /// <returns>The MailWatcher instance</returns>
        public MailWatcher OnMailReceived(Action<MailWatcher, Mail> mailReceivedAction)
        {
            _mailReceivedAction = mailReceivedAction;
            return this;
        }
        
        /// <summary>
        /// Starts watching
        /// </summary>
        public void Start()
        {
            if (IsWatching)
                return;

            IsWatching = true;
            _timer.Change(0, _interval);
        }

        /// <summary>
        /// Stops watching
        /// </summary>
        public void Stop()
        {
            if (!IsWatching)
                return;

            IsWatching = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private async void Time_Callback(object state)
        {
            if (!IsWatching || _mailReceivedAction == null)
                return;

            try
            {
                var response = await _httpClient.GetAsync(Uri);
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                var doc = new HtmlDocument();
                doc.Load(stream);

                var emails = doc.DocumentNode.SelectSingleNode("//ul[@id='email-list']");
                if (!emails.HasChildNodes)
                    return;

                foreach (var child in emails.ChildNodes)
                {
                    var a = child.SelectSingleNode("./a");
                    var mailFrom = Regex.Match(a.ChildNodes[0].ChildNodes[0].InnerText, MailFromPattern).Groups[1].Value;

                    // If the user specified a specific email to receive from and this mail isn't from who we want
                    if (!string.IsNullOrEmpty(SpecificFromEmail) && mailFrom != SpecificFromEmail)
                        continue;

                    var mailSubject = a.ChildNodes[0].ChildNodes[1].InnerText;

                    // Extract message id
                    var mailUrl = a.GetAttributeValue("href", null);
                    string mailId = Regex.Match(mailUrl, MailIdPattern).Groups[1].Value;

                    // If this mail was already received
                    if (ReceivedMails.FirstOrDefault(m => m.Id == mailId) != null)
                        continue;

                    // Get the email's body
                    var response2 = await _httpClient.GetAsync($"http://www.fakemailgenerator.com/email/gustr.com/ghactr/message-{mailId}/");
                    var mailBody = await response2.Content.ReadAsStringAsync();

                    // Add the mail to the received mails
                    var mail = new Mail(mailId, mailFrom, mailSubject, mailBody);
                    ReceivedMails.Add(mail);

                    // Invoke the mailReceivedAction
                    _mailReceivedAction.Invoke(this, mail);
                }
            }
            catch
            {
                // Ignored
            }
        }

        /// <summary>
        /// Releases all the resources used by the current instance of <see cref="MailWatcher"/>.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            _timer?.Dispose();
            ReceivedMails?.Clear();

            _httpClient = null;
            _timer = null;
            Uri = null;
            SpecificFromEmail = null;
            ReceivedMails = null;
            IsWatching = false;
            _mailReceivedAction = null;
            _interval = 0;
        }

    }
}
