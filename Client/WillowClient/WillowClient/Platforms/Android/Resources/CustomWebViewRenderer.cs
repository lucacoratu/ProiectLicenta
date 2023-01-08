using Android.App;
using Android.Content;
using Android.Net.Http;
using Android.Webkit;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Platforms.Android;

[assembly: ExportRenderer(typeof(WillowClient.CustomWebView), typeof(CustomWebViewRenderer))]
namespace WillowClient.Platforms.Android
{
    public class CustomWebViewRenderer : WebViewRenderer
    {
        Activity mContext;
        public CustomWebViewRenderer(Context context) : base(context)
        {
            this.mContext = context as Activity;
        }
        protected override void OnElementChanged(ElementChangedEventArgs<Microsoft.Maui.Controls.WebView> e)
        {
            base.OnElementChanged(e);
            Control.Settings.JavaScriptEnabled = true;
            Control.Settings.DomStorageEnabled = true;
            Control.Settings.MediaPlaybackRequiresUserGesture = false;
            Control.Settings.AllowContentAccess = true;
            Control.SetWebViewClient(new CustomWebViewClient());
            Control.SetWebChromeClient(new CustomWebViewChromeClient());
            //base.OnElementChanged(e);
            //Control.ClearSslPreferences();
            //Control.ClearCache(true);
            //Control.SetWebChromeClient(new CustomWebViewClient(mContext));
            global::Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
        }
    }

    public class CustomWebViewChromeClient : global::Android.Webkit.WebChromeClient
    {
        public CustomWebViewChromeClient()
        {
            
        }

        public override void OnPermissionRequest(PermissionRequest request)
        {
            request.Grant(request.GetResources());
        }
    }

    public class CustomWebViewClient : global::Android.Webkit.WebViewClient
    {
        Activity mContext;
        public CustomWebViewClient(Activity context)
        {
            this.mContext = context as Activity;
        }

        public CustomWebViewClient()
        {

        }

        public override void OnReceivedSslError(global::Android.Webkit.WebView view, SslErrorHandler handler, SslError error)
        {
            handler.Proceed();//this line make ssl error handle so the webview show the page even with certificate errors
        }
    }
}
