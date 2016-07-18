using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sure_copy
{
    class HttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;
        public bool Started = false;

        public HttpServer(int maxThreads)
        {
            _workers = new Thread[maxThreads];
            _queue = new Queue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
        }

        public void Start(int port)
        {
            //This command must be run from a command prompt for this app to run...
            // netsh http add urlacl url=http://+:8181/ user=DOMAIN\mwalters
            
            //remove reservation
            // netsh http  delete urlacl url=http://+:8181/
            
            //To show all 
            // netsh http  show urlacl

            _listener.Prefixes.Add(String.Format(@"http://+:{0}/", port));
            _listener.Start();
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
            Started = true;
        }

        public void Dispose()
        { 
            Stop();
        }

        public void Stop()
        {
            _stop.Set();
            _listenerThread.Join();
            foreach (Thread worker in _workers)
                worker.Join();
            _listener.Stop();
            Started = false;
        }

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                var context = _listener.BeginGetContext(ContextReady, null);

                if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                    return;
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                lock (_queue)
                {
                    _queue.Enqueue(_listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch { return; }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        context = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }

                try 
                { 
                    ProcessRequest(context); 
                }
                catch (Exception Ex) 
                {
                    if(m_eventWriteToLog != null)
                        m_eventWriteToLog("Exception Fired in Worker " + Ex.ToString(), LogMessageType.ErrorAlwaysDisplay);                    
                }
            }
        }

        public event Action<HttpListenerContext> ProcessRequest;
        public event DelegateWriteToLog m_eventWriteToLog;
    }
}
