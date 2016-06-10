package com.binc.buzz;

import android.Manifest;
import android.app.Activity;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.AsyncTask;
import android.os.Bundle;
import android.os.Looper;
import android.support.v4.app.ActivityCompat;
import android.support.v4.content.ContextCompat;
import android.support.v7.app.ActionBar;
import android.support.v7.app.AppCompatActivity;
import android.text.TextUtils;
import android.util.Log;
import android.view.Gravity;
import android.view.KeyEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowManager;
import android.view.inputmethod.EditorInfo;
import android.widget.AutoCompleteTextView;
import android.widget.Button;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import com.binc.buzz.R;
import com.binc.buzz.login.FacebookSignInProvider;
import com.binc.buzz.login.GoogleSignInProvider;
import com.binc.buzz.login.IdentityProvider;
import com.binc.buzz.login.SignInProvider;
import com.binc.buzz.login.SignInResultsHandler;
import com.binc.buzz.utils.ThreadUtils;
import com.facebook.login.widget.LoginButton;
import com.google.android.gms.common.SignInButton;

import java.util.ArrayList;

/**
 * Created by Brijesh on 11/05/2016.
 */
public class LoginActivity extends AppCompatActivity {
    private static final int GET_ACCOUNTS_PERMISSION_REQUEST_CODE = 93;
    AutoCompleteTextView mEmailView;
    EditText mPasswordView;
    AuthTask mAuthTask;
    ArrayList<SignInProvider> providers;
    GoogleSignInProvider mGoogleSignIn;
    FacebookSignInProvider mFBSignIn;
    SignInResultsHandler handler = new SignInResultsHandler() {
        @Override
        public void onSuccess(final IdentityProvider ip) {
            if(!isRunningOnUIThread()) {
                ThreadUtils.runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        onSuccess(ip);
                    }
                });
            } else {
                Log.d("[Handler]","onSuccess");
            }
        }

        @Override
        public void onCancel(final IdentityProvider ip) {
            if(!isRunningOnUIThread()) {
                ThreadUtils.runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        onCancel(ip);
                    }
                });
            } else {
                Log.d("[Handler]","onCancel");
            }
        }

        @Override
        public void onError(final IdentityProvider ip, final Exception ex) {
            if(!isRunningOnUIThread()) {
                ThreadUtils.runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        onError(ip,ex);
                    }
                });
            } else {
                Log.d("[Handler]","onError");
            }
        }
    };
    @Override
    protected void onCreate(Bundle savedInstanceState){
        super.onCreate(savedInstanceState);
        getSupportActionBar().hide();
        providers = new ArrayList<>();
        mFBSignIn = new FacebookSignInProvider(this);
        providers.add(mFBSignIn);
        setContentView(R.layout.activity_login);
        mGoogleSignIn = new GoogleSignInProvider(this);
        providers.add(mGoogleSignIn);
        initFacebookSignIn();
        initGoogleSignIn();
        initEmailSignIn();
    }
    private void initEmailSignIn(){
        //TODO : move login logic to custom provider
        mEmailView = (AutoCompleteTextView) findViewById(R.id.email);
        mPasswordView = (EditText) findViewById(R.id.password);
        /* pressing enter while typing pass should try login */
        mPasswordView.setOnEditorActionListener(new TextView.OnEditorActionListener() {
            @Override
            public boolean onEditorAction(TextView v, int actionId, KeyEvent event) {
                /*ime action id is given for password view in layout*/
                if (actionId == R.id.login || actionId == EditorInfo.IME_NULL) {
                    attemptLogin();
                    return true;
                }
                return false;
            }
        });
        Button mEmailSignInButton = (Button) findViewById(R.id.email_sign_in_button);
        mEmailSignInButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                attemptLogin();
            }
        });
    }
    private void initGoogleSignIn(){
        SignInButton v = (SignInButton)findViewById(R.id.google_sign_in_btn);
        for (int i = 0; i < v.getChildCount(); i++) {
            View z = v.getChildAt(i);

            if (z instanceof TextView) {
                TextView tv = (TextView) z;
                tv.setText("Sign in with Google");
                FrameLayout.LayoutParams params = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT);
                params.gravity = Gravity.CENTER;
                tv.setLayoutParams(params);
                break;            }
        }
        mGoogleSignIn = new GoogleSignInProvider(this);
        final View.OnClickListener vc = mGoogleSignIn.initializeSignInButton(LoginActivity.this, v, handler);

        if (vc != null) {
            // if the onClick listener was null, initializeSignInButton will have removed the view.
            v.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(final View view) {
                    final Activity thisActivity = LoginActivity.this;
                    if (ContextCompat.checkSelfPermission(thisActivity,
                            Manifest.permission.GET_ACCOUNTS) != PackageManager.PERMISSION_GRANTED) {
                        ActivityCompat.requestPermissions(LoginActivity.this,
                                new String[]{Manifest.permission.GET_ACCOUNTS},
                                GET_ACCOUNTS_PERMISSION_REQUEST_CODE);
                        return;
                    }
                    vc.onClick(view);
                }
            });
        }
    }
    private void initFacebookSignIn(){
        LoginButton v = (LoginButton)findViewById(R.id.facebook_sign_in_btn);
        mFBSignIn.initializeSignInButton(LoginActivity.this,v,handler);
    }
    private void attemptLogin() {
        if (mAuthTask != null) {
            return;
        }

        mEmailView.setError(null);
        mPasswordView.setError(null);

        String email = mEmailView.getText().toString();
        String password = mPasswordView.getText().toString();

        View focusInvalidView = null;

        if (!TextUtils.isEmpty(password) && !isPasswordValid(password)) {
            mPasswordView.setError(getString(R.string.error_invalid_password));
            focusInvalidView = mPasswordView;
        }

        if (TextUtils.isEmpty(email)) {
            mEmailView.setError(getString(R.string.error_field_required));
            focusInvalidView = mEmailView;
        } else if (!isEmailValid(email)) {
            mEmailView.setError(getString(R.string.error_invalid_email));
            focusInvalidView = mEmailView;
        }

        if (focusInvalidView != null) {
            focusInvalidView.requestFocus();
        } else {
            mAuthTask = new AuthTask(email, password);
            mAuthTask.execute((Void) null);
        }
    }

    private boolean isEmailValid(String email) {
        //TODO: Replace this with your own logic
        return email.contains("@");
    }

    private boolean isPasswordValid(String password) {
        //TODO: Replace this with your own logic
        return password.length() > 4;
    }
    private boolean isRunningOnUIThread(){
        return Looper.myLooper() == Looper.getMainLooper();
    }
    @Override
    protected void onActivityResult(final int requestCode, final int resultCode, final Intent data) {
        for (final SignInProvider provider : providers) {
            if (provider.isRequestCodeOurs(requestCode)) {
                provider.handleActivityResult(requestCode, resultCode, data);
                break;
            }
        }
        super.onActivityResult(requestCode, resultCode, data);
    }
    public class AuthTask extends AsyncTask<Void, Void, Boolean> {

        private final String mEmail;
        private final String mPassword;

        AuthTask(String email, String password) {
            mEmail = email;
            mPassword = password;
        }

        @Override
        protected Boolean doInBackground(Void... params) {
            // TODO: attempt authentication against a network service.

            try {
                // Simulate network access.
                Thread.sleep(2000);
            } catch (InterruptedException e) {
                return false;
            }

            // TODO: register the new account here.
            return true;
        }

        @Override
        protected void onPostExecute(final Boolean success) {
            mAuthTask = null;

            if (success) {
                finish();
            } else {
                mPasswordView.setError(getString(R.string.error_incorrect_password));
                mPasswordView.requestFocus();
            }
        }

        @Override
        protected void onCancelled() {
            mAuthTask = null;
        }
    }
}

