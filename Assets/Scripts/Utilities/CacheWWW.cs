using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CacheWWW {
    private static Dictionary<string, WWWrapper> Cache = new Dictionary<string, WWWrapper>();

    // cacheTimeMs = 0 means no cache time (expire directly) - default (-1) means to read the response and get specific header with timeout
    public static WWW Get(string url, long cacheTimeMs = -1) {
        WWW www;
        if (cacheTimeMs != 0 && CacheWWW.HasValidCache(url)) {
//            UnityEngine.Debug.Log("CACHED");
            www = Cache[url].www;
        } else {
//            UnityEngine.Debug.Log("NOT CACHED");
            WWWrapper wwwrapper = new WWWrapper(url, cacheTimeMs);
            Cache.Add(url, wwwrapper);
            www = wwwrapper.www;

            if (cacheTimeMs == -1) {
                // Read header in response and use as cachetime
                Singleton<SingletonInstance>.Instance.StartCoroutine(updateWWWrapperWithCacheOnResponse(wwwrapper));
            }
        }
        return www;
    }

    private static IEnumerator updateWWWrapperWithCacheOnResponse(WWWrapper wwwrapper) {
        WWW www = wwwrapper.www;
        yield return www;

        if (www != null && www.responseHeaders != null && www.responseHeaders.ContainsKey("WWW-Cache")) {
            long cacheTimeMs = Convert.ToInt64 (www.responseHeaders ["WWW-Cache"]);
            if (cacheTimeMs > 0L) {
//                UnityEngine.Debug.Log("Updated to " + cacheTimeMs);
                wwwrapper.updateCacheTime(cacheTimeMs);
            } else if (cacheTimeMs == 0L) {
//                UnityEngine.Debug.Log("Removed cache!");
                Cache.Remove(wwwrapper.url);
            }
        }
    }

    private static bool HasValidCache(string url) {
        if (Cache.ContainsKey(url)) {
            // It has cache, either it's expired and should be removed, or it's valid and we should return true
            if (Cache[url].isValid()) {
                return true;
            } else {
                Cache.Remove(url);
            }
        }
        return false;
    }

    private class WWWrapper {
        public long expire;
        public string url;
        public WWW www;

        public WWWrapper(string url, long cacheTimeMs) {
            this.url = url;
            expire = Stopwatch.GetTimestamp() + cacheTimeMs;
            www = new WWW (url);
        }

        public bool isValid() {
            return Stopwatch.GetTimestamp() >= expire;
        }

        public void updateCacheTime(long cacheTimeMs) {
            expire = Stopwatch.GetTimestamp() + cacheTimeMs;
        }
    }
}
