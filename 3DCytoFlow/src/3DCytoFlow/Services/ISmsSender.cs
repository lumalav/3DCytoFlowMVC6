using System.Threading.Tasks;

namespace _3DCytoFlow.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
        void SendSms(AuthMessageSender.Message message, string accountSid, string authToken, string number, string to);
    }
}
