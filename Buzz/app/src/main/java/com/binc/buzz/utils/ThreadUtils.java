package com.binc.buzz.utils;
import android.os.Handler;
import android.os.Looper;

/**
 * Created by Brijesh on 13/05/2016.
 */
public class ThreadUtils {
    public static void runOnUiThread(final Runnable runnable) {
        if (Looper.myLooper() != Looper.getMainLooper()) {
            new Handler(Looper.getMainLooper()).post(runnable);
        } else {
            runnable.run();
        }
    }
}