package com.binc.buzz.login;

import android.app.Activity;
import android.content.Intent;
import android.view.View;

/**
 * Created by Brijesh on 11/05/2016.
 */
public interface SignInProvider  extends IdentityProvider{
    void handleActivityResult(int requestCode, int resultCode, Intent data);
    View.OnClickListener initializeSignInButton(Activity signInActivity, View buttonView, SignInResultsHandler resultsHandler);

    boolean isRequestCodeOurs(int requestCode);
}
