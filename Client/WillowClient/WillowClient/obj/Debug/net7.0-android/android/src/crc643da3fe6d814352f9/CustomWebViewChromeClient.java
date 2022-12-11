package crc643da3fe6d814352f9;


public class CustomWebViewChromeClient
	extends android.webkit.WebChromeClient
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onPermissionRequest:(Landroid/webkit/PermissionRequest;)V:GetOnPermissionRequest_Landroid_webkit_PermissionRequest_Handler\n" +
			"";
		mono.android.Runtime.register ("WillowClient.Platforms.Android.CustomWebViewChromeClient, WillowClient", CustomWebViewChromeClient.class, __md_methods);
	}


	public CustomWebViewChromeClient ()
	{
		super ();
		if (getClass () == CustomWebViewChromeClient.class) {
			mono.android.TypeManager.Activate ("WillowClient.Platforms.Android.CustomWebViewChromeClient, WillowClient", "", this, new java.lang.Object[] {  });
		}
	}


	public void onPermissionRequest (android.webkit.PermissionRequest p0)
	{
		n_onPermissionRequest (p0);
	}

	private native void n_onPermissionRequest (android.webkit.PermissionRequest p0);

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
