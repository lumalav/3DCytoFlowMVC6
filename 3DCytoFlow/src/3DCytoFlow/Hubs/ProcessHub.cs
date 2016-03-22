using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using _3DCytoFlow.EFChangeNotify;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Hubs
{
    public class ProcessHub : Hub
    {
        private EntityChangeNotifier<Analysis, ApplicationDbContext> _notifier;

        public override Task OnConnected()
        {
            var username = Context.User.Identity.Name;

            _notifier = new EntityChangeNotifier<Analysis, ApplicationDbContext>(i => i.User.FirstName.Equals(username) && i.ResultFilePath != null);

            _notifier.Changed += (sender, e) => OnProcessFinishesHandler(e);

            return base.OnConnected();
        }

        private void OnProcessFinishesHandler(EntityChangeEventArgs<Analysis> changedRecords)
        {
            BroadcastMessage("", "");

            OnReconnected();
        }


        public Task OnDisconnected()
        {
            return base.OnDisconnected(true);
        }

        public void BroadcastMessage(string text, string obj)
        {
            Clients.All.displayBatchInfo(text, obj);
        }
    }
}
