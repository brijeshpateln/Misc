package com.binc.buzz.login;

/**
 * Created by Brijesh on 11/05/2016.
 */
public interface IdentityProvider {
    /**
     * Call getToken to retrieve the access token from successful sign-in with this provider.
     * @return the access token
     */
    String getToken();

    /**
     * Refreshes the token if it has expired.
     * @return the refreshed access token, or null if the token cannot be refreshed.
     */
    String refreshToken();

    /**
     * Call signOut to sign out of this provider.
     */
    void signOut();

    /**
     * Gets the user's name, assuming user is signed in.
     * @return user name or null if not signed-in.
     */
    UserInfo getUserInfo();

    /**
     * Force the provider to reload user name and image.
     * Note: this is a blocking call.
     */
    void reloadUserInfo();
}
