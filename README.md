# FakeMailWatcher
A helper class that watchs a FakeMailGenerator email for new mails.
This library is made with .NET Standard 2.0.

## How to use

Watch mails for *testemail457@dayrep.com*.
```
var watcher = new MailWatcher("dayrep.com", "testemail457").OnMailReceived((watcher, mail) => 
{
  Console.WriteLine("Mail #{0} received from {1}.", mail.Id, mail.From);
}).Start();
```

Watch mails coming from *xer-noreply@gmail.com* for *testemail457@dayrep.com*
```
var watcher = new MailWatcher("dayrep.com", "testemail457", "xer-noreply@gmail.com").OnMailReceived((watcher, mail) => 
{
  Console.WriteLine("Mail #{0} received from {1}.", mail.Id, mail.From);
}).Start();
```
