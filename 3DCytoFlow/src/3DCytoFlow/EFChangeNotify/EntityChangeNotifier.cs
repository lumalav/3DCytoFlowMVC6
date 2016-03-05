using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using _3DCytoFlow.Models;

namespace _3DCytoFlow.EFChangeNotify
{
    public class EntityChangeNotifier<TEntity, IdentityDbContext> : IDisposable where IdentityDbContext : new() where TEntity : class
    {
        private ApplicationDbContext _context;
        private readonly Expression<Func<TEntity, bool>> _query;
        private readonly string _connectionString;

        public event EventHandler<EntityChangeEventArgs<TEntity>> Changed;
        public event EventHandler<NotifierErrorEventArgs> Error;

        public EntityChangeNotifier(Expression<Func<TEntity, bool>> query)
        {
            _context = new ApplicationDbContext();
            _query = query;
            _connectionString = Startup.GetDefaultConnectionString();

            SafeCountDictionary.Increment(_connectionString, x => SqlDependency.Start(x));

            RegisterNotification();
        }

        private void RegisterNotification()
        {
            _context = new ApplicationDbContext();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = GetCommand())
                {
                    command.Connection = connection;
                    connection.Open();

                    var sqlDependency = new SqlDependency(command);
                    sqlDependency.OnChange += _sqlDependency_OnChange;

                    command.ExecuteReader();
                }
            }
        }

        private string GetSql()
        {
            var q = GetCurrent();

            return q.ToTraceString();
        }

        private SqlCommand GetCommand()
        {
            var q = GetCurrent();

            return q.ToSqlCommand();
        }

        private DbQuery<TEntity> GetCurrent()
        {
            var query = _context.Set<TEntity>().Where(_query) as DbQuery<TEntity>;

            return query;
        }

        private void _sqlDependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (_context == null)
                return;

            if (e.Type == SqlNotificationType.Subscribe || e.Info == SqlNotificationInfo.Error)
            {
                var args = new NotifierErrorEventArgs
                {
                    Reason = e,
                    Sql = GetCurrent().ToString()
                };

                OnError(args);
            }
            else
            {
                var args = new EntityChangeEventArgs<TEntity>
                {
                    Results = GetCurrent(),
                    ContinueListening = true
                };

                OnChanged(args);

                if (args.ContinueListening)
                {
                    RegisterNotification();
                }
            }
        }

        protected virtual void OnChanged(EntityChangeEventArgs<TEntity> e)
        {
            if (Changed != null)
            {
                Changed(this, e);
            }
        }

        protected virtual void OnError(NotifierErrorEventArgs e)
        {
            if (Error != null)
            {
                Error(this, e);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            SafeCountDictionary.Decrement(_connectionString, x => SqlDependency.Stop(x));

            if (_context == null) return;
            _context.Dispose();
            _context = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
