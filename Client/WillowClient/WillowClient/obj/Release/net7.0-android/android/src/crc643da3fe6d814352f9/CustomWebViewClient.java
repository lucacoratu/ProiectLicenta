package crc643da3fe6d814352f9;


public class CustomWebViewClient
	extends android.webkit.WebViewClient
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onReceivedSslError:(Landroid/webkit/WebView;Landroid/webkit/SslErrorHandler;Landroid/net/http/SslError;)V:GetOnReceivedSslError_Landroid_webkit_WebView_Landroid_webkit_SslErrorHandler_Landroid_net_http_SslError_Handler\n" +
			"";
		mono.android.Runtime.register ("WillowClient.Platforms.Android.CustomWebViewClient, WillowClient", CustomWebViewClient.class, __md_methods);
	}


	public CustomWebViewClient ()
	{
		super ();
		if (getClass () == CustomWebViewClient.class) {
			mono.android.TypeManager.Activate ("WillowClient.Platforms.Android.CustomWebViewClient, WillowClient", "", this, new java.lang.Object[] {  });
		}
	}

	public CustomWebViewClient (android.app.Activity p0)
	{
		super ();
		if (getClass () == CustomWebViewClient.class) {
			mono.android.TypeManager.Activate ("WillowClient.Platforms.Android.CustomWebViewClient, WillowClient", "Android.App.Activity, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}


	public void onReceivedSslError (android.webkit.WebView p0, android.webkit.SslErrorHandler p1, android.net.http.SslError p2)
	{
		n_onReceivedSslError (p0, p1, p2);
	}

	private native void n_onReceivedSslError (android.webkit.WebView p0, android.webkit.SslErrorHandler p1, android.net.http.SslError p2);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
