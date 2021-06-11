namespace Rev1.API.Security.Business.Contract
{
    public interface IEmailService
    {
        void Send(string to, string subject, string html, string from = null);
    }
}
