namespace FakeMailWatcher
{
    public class Mail
    {

        // Properties
        public string Id { get; }
        public string From { get; }
        public string Subject { get; }
        public string Body { get; }


        // Constructor
        public Mail(string id, string from, string subject, string body)
        {
            Id = id;
            From = from;
            Subject = subject;
            Body = body;
        }

    }
}
