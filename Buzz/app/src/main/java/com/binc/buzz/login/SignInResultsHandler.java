package com.binc.buzz.login;

/**
 * Created by Brijesh on 13/05/2016.
 */
public interface SignInResultsHandler {
    public void onSuccess(IdentityProvider ip);
    public void onCancel(IdentityProvider ip);
    public void onError(IdentityProvider ip, Exception ex);
}
