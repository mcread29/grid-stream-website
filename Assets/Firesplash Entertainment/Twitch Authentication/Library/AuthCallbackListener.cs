using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Firesplash.UnityAssets.TwitchAuthentication.Internal
{
    internal class AuthCallbackListener
    {
        public HttpListener listener;
        public string url;

        Thread listenerThread;
        Action<string> callback;
        CancellationTokenSource cancelTokenSource;

        string successMessage = "<!DOCTYPE HTML><html><body><h2>Twitch Authentication</h2><p>You can now close this window and return to the application.</p></body></html>";

        public AuthCallbackListener(int port, Action<string> callback, string successHTMLPage)
        {
            this.successMessage = successHTMLPage;

            this.url = "http://localhost:" + port + "/";

            this.callback = callback;
            cancelTokenSource = new CancellationTokenSource();

            listenerThread = new Thread(HTTPServerThread);
            listenerThread.Start();
        }

        public void Cancel()
        {
            cancelTokenSource.Cancel();
            listenerThread.Abort();
        }

        ~AuthCallbackListener()
        {
            Cancel();
        }


        async Task HandleIncomingConnections()
        {
            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (!cancelTokenSource.IsCancellationRequested)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Write the response info
                byte[] data;
                if (req.Url.Query?.Length > 3) data = Encoding.UTF8.GetBytes(successMessage);
                else data = Encoding.UTF8.GetBytes("<!DOCTYPE HTML><html><body><h2>Twitch Authentication</h2><p>Please wait a few seconds while we're redirecting you...<br/><b>If this is not working, please allow javascript for this site or replace the # in the URL bar with a ?</b></p><script>window.onload=function(){document.location.href=document.location.href.replace('#','?');}</script></body></html>");

                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                if ((req.Url.AbsolutePath == "/" || req.Url.AbsolutePath == "") && (req.Url.Query?.Length > 3 || req.Url.Fragment?.Length > 3))
                {
                    callback.Invoke(req.Url.Query?.Length > 3 ? req.Url.Query : req.Url.Fragment);
                    cancelTokenSource.Cancel();
                }

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        void HTTPServerThread()
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}
