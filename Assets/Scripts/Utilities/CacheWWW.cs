using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CacheWWW {
    private static Dictionary<string, WWWrapper> Cache = new Dictionary<string, WWWrapper>();

    public static WWW Get(string url, long cacheTimeMs = 0) {
        WWW www;
        if (cacheTimeMs > 0 && CacheWWW.HasValidCache(url)) {
            UnityEngine.Debug.Log("CACHED");
            www = Cache[url].www;
        } else {
            UnityEngine.Debug.Log("NOT CACHED");
            WWWrapper wwwrapper = new WWWrapper(url, cacheTimeMs);
            Cache.Add(url, wwwrapper);
            www = wwwrapper.www;
        }
        return www;
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
        public WWW www;

        public WWWrapper(string url, long cacheTimeMs) {
            expire = Stopwatch.GetTimestamp() + cacheTimeMs;
            www = new WWW(url);
        }

        public bool isValid() {
            return Stopwatch.GetTimestamp() >= expire;
        }
    }
}
