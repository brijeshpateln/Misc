package com.binc.buzz.login;
import android.accounts.Account;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.IntentSender;
import android.content.pm.ApplicationInfo;
import android.util.Log;
import android.view.View;

import com.binc.buzz.utils.ThreadUtils;
import com.google.android.gms.auth.GoogleAuthException;
import com.google.android.gms.auth.GoogleAuthUtil;
import com.google.android.gms.common.ConnectionResult;
import com.google.android.gms.common.GooglePlayServicesUtil;
import com.google.android.gms.common.Scopes;
import com.google.android.gms.common.api.GoogleApiClient;
import com.google.android.gms.common.api.Scope;
import com.google.android.gms.plus.Plus;

import java.io.IOException;
/**
 * Created by Brijesh on 11/05/2016.
 *
*/

/* implement GoogleApiClient.ConnectionCallbacks if not using blocking connect ( we use thread and then make call to blockingConnect() */
public class GoogleSignInProvider implements SignInProvider {

    /* Refer : https://developers.google.com/identity/sign-in/android/sign-in?configured=true#configure_google_sign-in_and_the_googleapiclient_object */
    public static final String GOOGLE_CLIENT_ID = "187527357537-fen8emfilh3vj1n9223e1e7u6o06dh8b.apps.googleusercontent.com";
    public static final String LOG_TAG = GoogleSignInProvider.class.getSimpleName();
    private static final int RC_SIGN_IN = 909090;
    private final Context context;
    private final GoogleApiClient mGoogleApiClient;
    private boolean mIsResolving = false;
    private SignInResultsHandler mResultHandler;
    private Activity mLoginActivity;
    private UserInfo mUserInfo;
    private String mAuthToken;

    public GoogleSignInProvider(final Context context) {
        this.context = context;

        mUserInfo = null;

        mGoogleApiClient = new GoogleApiClient.Builder(context)
                .addApi(Plus.API)
                .addScope(new Scope(Scopes.PLUS_LOGIN))
                .addScope(new Scope(Scopes.PROFILE))
                .addScope(new Scope(Scopes.PLUS_ME))
                .build();
        mGoogleApiClient.connect();
    }


    @Override
    public void handleActivityResult(int requestCode, int resultCode, Intent data) {
        Log.d(LOG_TAG, "handleActivityResult:" + requestCode + ":" + resultCode + ":" + data);

        if (requestCode == RC_SIGN_IN) {
            mIsResolving = false;
            if(resultCode == 0) {
                mResultHandler.onCancel(GoogleSignInProvider.this);
                mUserInfo = null;
                return;
            }
            signIn();
        }
    }
    @Override
    public boolean isRequestCodeOurs(int requestCode) {
        return requestCode == RC_SIGN_IN;
    }
    private void signIn() {
        new Thread(new Runnable() {
            @Override
            public void run() {
                final ConnectionResult result = mGoogleApiClient.blockingConnect();
                if (!result.isSuccess()) {
                    ThreadUtils.runOnUiThread(new Runnable() {
                        @Override
                        public void run() {
                            onConnectionFailed(result);
                        }
                    });
                    return;
                }

                try {
                    mAuthToken = getGoogleAuthToken();
                    Log.d(LOG_TAG, "Google provider sign-in succeeded!");
                    reloadUserInfo();
                    ThreadUtils.runOnUiThread(new Runnable() {
                        @Override
                        public void run() {
                            mResultHandler.onSuccess(GoogleSignInProvider.this);
                        }
                    });
                } catch (final Exception e) {
                    Log.e(LOG_TAG, "Error retrieving ID token.", e);
                    ThreadUtils.runOnUiThread(new Runnable() {
                        @Override
                        public void run() {
                            mResultHandler.onError(GoogleSignInProvider.this, e);
                        }
                    });
                }
            }
        }).start();
    }
    private String getGoogleAuthToken() throws GoogleAuthException, IOException {
        final String accountName = Plus.AccountApi.getAccountName(mGoogleApiClient);
        final Account googleAccount = new Account(accountName, GoogleAuthUtil.GOOGLE_ACCOUNT_TYPE);
        final String scopes = "audience:server:client_id:" + GOOGLE_CLIENT_ID;
        return GoogleAuthUtil.getToken(context, googleAccount, scopes);
    }
    private void onConnectionFailed(final ConnectionResult result){
        if (!mIsResolving) {
            if (result.hasResolution()) {
                try {
                    mIsResolving = true;
                    result.startResolutionForResult(mLoginActivity, RC_SIGN_IN);
                } catch (IntentSender.SendIntentException ex) {
                    Log.e(LOG_TAG, "Could not resolve ConnectionResult." + ex);
                    mIsResolving = false;
                    mGoogleApiClient.connect();
                }
            } else {
                mResultHandler.onError(GoogleSignInProvider.this,
                      new IllegalStateException(result.toString()));
            }
        } else {
            Log.w(LOG_TAG, "onConnectionFailed while Google sign-in intent is already in progress.");
        }
    }
    @Override
    public View.OnClickListener initializeSignInButton(Activity signInActivity, View buttonView, SignInResultsHandler resultsHandler) {
        mLoginActivity = signInActivity;
        mResultHandler = resultsHandler;

        if (GooglePlayServicesUtil.isGooglePlayServicesAvailable(context.getApplicationContext()) != 0) {
            final boolean isDebugBuild =
                    (0 != (signInActivity
                            .getApplicationContext()
                            .getApplicationInfo()
                            .flags & ApplicationInfo.FLAG_DEBUGGABLE));

            if (!isDebugBuild) {
                buttonView.setVisibility(View.GONE);
            } else {
                Log.w(LOG_TAG, "Google Play Services are not available, but we are showing the Google Sign-in Button, anyway, because this is a debug build.");
            }
            return null;
        }

        final View.OnClickListener listener = new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                signIn();
            }
        };
        buttonView.setOnClickListener(listener);
        return listener;
    }

    @Override
    public String getToken() {
        return mAuthToken;
    }

    @Override
    public String refreshToken() {
        try {
            mAuthToken = getGoogleAuthToken();
        } catch (Exception e) {
            Log.w(LOG_TAG, "Failed to update Google token", e);
            mAuthToken = null;
        }
        return mAuthToken;
    }

    @Override
    public void signOut() {
        mUserInfo = null;
        mAuthToken = null;
        if (mGoogleApiClient.isConnected()) {
            Plus.AccountApi.clearDefaultAccount(mGoogleApiClient);
            mGoogleApiClient.disconnect();
        }
    }
    @Override
    public UserInfo getUserInfo() {
        return mUserInfo;
    }

    @Override
    public void reloadUserInfo() {
        mUserInfo = new UserInfo();
        mUserInfo.userName = Plus.PeopleApi.getCurrentPerson(mGoogleApiClient).getDisplayName();
        mUserInfo.userEmail = Plus.AccountApi.getAccountName(mGoogleApiClient);
        mUserInfo.userImageUrl = Plus.PeopleApi.getCurrentPerson(mGoogleApiClient).getImage().getUrl();
        Log.d(LOG_TAG,mUserInfo.userName + " " + mUserInfo.userEmail + " " + mUserInfo.userImageUrl);
    }
}
