using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using _3DCytoFlow.EFChangeNotify;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.Hubs
{
    public class ProcessHub : Hub
    {
        private EntityChangeNotifier<Analysis, ApplicationDbContext> _notifier;
        private int _currentPayloadsCount;
        private int _updatedPayloadsCount;
        private int _currentDifference;

        public override Task OnConnected()
        {
//            SetCurrentPayloadsCount();

            _notifier = new EntityChangeNotifier<Analysis, ApplicationDbContext>(i => true);

//            _notifier.Changed += (sender, e) => OnBatchArrivesOrChangesHandler(e);

            return base.OnConnected();
        }
//
//        private void OnBatchArrivesOrChangesHandler(EntityChangeEventArgs<Analysis> changedRecords)
//        {
//            using (var db = new ApplicationDbContext())
//            {
//                _updatedPayloadsCount = db.PayloadControls.Count();
//            }
//
//            var difference = _updatedPayloadsCount - _currentPayloadsCount;
//
//            if (difference == 0)
//            {
//                var revertedList = changedRecords.Results.Reverse();
//                var json = revertedList.Any() ? JsonConvert.SerializeObject(revertedList) : "";
//
//                if (string.IsNullOrWhiteSpace(json)) return;
//
//                BroadcastMessage(Convert.ToString(0), json);
//                OnReconnected();
//            }
//
//            _currentDifference = difference;
//
//            if (_currentDifference > 0)
//                BroadcastMessage(_currentDifference.ToString(CultureInfo.InvariantCulture), "");
//
//            OnReconnected();
//        }

//        private void SetCurrentPayloadsCount()
//        {
//            using (var db = new ApplicationDbContext())
//            {
//                _currentPayloadsCount = db.PayloadControls.Count();
//            }
//        }

//        public override Task OnReconnected()
//        {
////            SetCurrentPayloadsCount();
//            return base.OnReconnected();
//        }

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
