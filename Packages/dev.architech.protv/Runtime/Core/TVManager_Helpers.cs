using System;
using ArchiTech.SDK;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech.ProTV
{
    public partial class TVManager
    {
        private readonly char[] SPLIT_QUERY = { '?' };
        private readonly char[] SPLIT_QUERY_PARAM = { '&' };
        private readonly char[] SPLIT_HASH = { '#' };
        private readonly char[] SPLIT_HASH_PARAM = { ';' };
        private readonly char[] SPLIT_VALUE = { '=' };
        private readonly char[] SPLIT_PATH = { '/' };
        private readonly string[] SPLIT_PROTOCOL = { "://" };
        private const StringSplitOptions splitNone = StringSplitOptions.None;
        private const StringSplitOptions splitTrim = StringSplitOptions.RemoveEmptyEntries;

        /// <seealso cref="_RefreshMedia"/>
        private void triggerRefresh(float delay = 0f, string callSource = "(No Source Provided)")
        {
            waitingForMediaRefresh = true;
            var newWait = Time.timeSinceLevelLoad + delay;
            // longest wait time should always take priority
            if (newWait >= waitUntil)
            {
                if (IsTraceEnabled) Trace($"Refresh triggered for {delay} seconds from now, from source: {callSource}");
                waitUntil = newWait;
                // ensure that the global rate limit time is respected
                if (waitUntil < nextUrlAttemptAllowed)
                    waitUntil = nextUrlAttemptAllowed;
            }
        }

        private void triggerSync(float delay)
        {
            enforceSyncTime = true;
            var time = Time.timeSinceLevelLoad;
            var newWait = time + delay;
            // longest wait time should always take priority
            if (newWait > syncEnforceWait)
            {
                syncEnforceWait = newWait;
                autoSyncWait = time + automaticResyncInterval;
                if (IsTraceEnabled) Trace($"Sync Enforcement triggered for {delay} seconds from now");
            }
        }

        private void setLoadingState(bool yes)
        {
            if (yes && maxAllowedLoadingTime > 0) loadingWait = Time.timeSinceLevelLoad + maxAllowedLoadingTime;
            loading = yes;
            SendManagedEvent(loading ? nameof(TVPlugin._TvLoading) : nameof(TVPlugin._TvLoadingEnd));
            if (deserializationDelayedByLoadingState) SendCustomEventDelayedFrames(nameof(_PostDeserialization), 1);
            forceBlitOnce = true;
        }

        /// <summary>
        /// Update and assign the global shader data property. 
        /// Empty spots denote a reserved piece of data for later use.
        /// <br/><br/>
        /// MATRIX DATA STRUCTURE
        /// <code><para>
        /// [ FLAGS   (_11) , STATE        (_12) , ERROR_STATE    (_13) , READY (_14) ]<br/>
        /// [ VOLUME  (_21) , SEEK_PERCENT (_22) , PLAYBACK_SPEED (_23) ,       (_24) ]<br/>
        /// [         (_31) ,              (_32) ,                (_33) ,       (_34) ]<br/>
        /// [ 3D_MODE (_41) ,              (_42) ,                (_43) ,       (_44) ]<br/>
        /// </para></code>
        /// The flags field (_11) is a composition of the following options (and their respective value checks, the int() cast is required in the shader)
        /// <code><para>
        /// LOCKED  (int(_11) &gt;&gt; 0 &amp; 1)<br/>
        /// MUTE    (int(_11) &gt;&gt; 1 &amp; 1)<br/>
        /// LIVE    (int(_11) &gt;&gt; 2 &amp; 1)<br/>
        /// LOADING (int(_11) &gt;&gt; 3 &amp; 1)<br/>
        /// FORCE2D (int(_11) &gt;&gt; 4 &amp; 1)<br/>
        /// </para></code>
        /// </summary>
        /// <seealso cref="shaderVideoData"/>
        private void updateShaderData()
        {
            int flags = 0;
            flags |= locked         ? 1 << 0 : 0;
            flags |= mute           ? 1 << 1 : 0;
            flags |= isLive         ? 1 << 2 : 0;
            flags |= IsLoadingMedia ? 1 << 3 : 0;
            flags |= force2D        ? 1 << 4 : 0;

            // due to funky U# compilation stuff, must cast enum to int var
            // then explicitly cast to a float for the assignment.
            int istate = (int)state;
            int ierror = (int)errorState;

            // _11
            shaderVideoData.m00 = (float)flags;
            // _12
            shaderVideoData.m01 = (float)istate;
            // // _13
            shaderVideoData.m02 = (float)ierror;
            // // _14
            shaderVideoData.m03 = isReady ? 1f : 0;
            // _21
            shaderVideoData.m10 = volume;
            // _22
            shaderVideoData.m11 = SeekPercent;
            // _23
            shaderVideoData.m12 = PlaybackSpeed;
            // _24
            shaderVideoData.m13 = 0;

            // _31
            shaderVideoData.m20 = 0;
            // _32
            shaderVideoData.m21 = 0;
            // _33
            shaderVideoData.m22 = 0;
            // _34
            shaderVideoData.m23 = 0;

            int imode = (int)video3d;
            if (video3dFull) imode *= -1;
            // _41
            shaderVideoData.m30 = (float)imode;
            // _42
            shaderVideoData.m31 = 0;
            // _43
            shaderVideoData.m32 = 0;
            // _44
            shaderVideoData.m33 = 0;
        }

        /// <summary>
        /// Intakes a given url and outputs the important parts that the TV needs to know about. 
        /// It will parse to extract the domain name and all valid parameters and respective values.<br/>
        /// The parameters will be pulled from both the url's query parameters as well as the custom url fragment (aka hash) parameters.
        /// Hash parameters override any of the parameters of the same name found in the query parameters section.<br/>
        /// Query parameters are separated by the ampersand ( &amp; ) symbol with the key and value being separated with an equals ( = ) sign.
        /// Hash parameters are separated by the semicolon ( ; ) symbol with the key and value being separated with an equals ( = ) sign.
        /// </summary>
        /// <example>
        /// <code>https://mydomain.com/?key1=value1&amp;key2=value2#key3;key2=value3</code>
        /// This would split into:
        /// <code>
        /// - domain: "mydomain.com"
        /// - keys: ["key1", "key2", "key3"]
        /// - values: ["value1", "value3", ""]
        /// </code>
        /// You'll notice that key2 from the query params was overwritten by the value from the hash params.<br/>
        /// Additionally, key3 did not have an = sign value, so the value becomes implicitly empty. Besure to handle the empty string appropriately.
        /// </example>
        /// <param name="urlStr">the given url to parse</param>
        /// <param name="protocol">out value for the protocol portion of the given url</param>
        /// <param name="domain">out value for the domain portion of the given url</param>
        /// <param name="keys">out array for all the param keys found</param>
        /// <param name="values">out array for all the param values found</param>
        private void parseUrl(string urlStr, out string protocol, out string domain, out string[] keys, out string[] values)
        {
            protocol = EMPTYSTR;
            domain = EMPTYSTR;
            keys = new string[0];
            values = new string[0];
            if (string.IsNullOrWhiteSpace(urlStr)) return;
            var sarr = urlStr.Split(SPLIT_HASH, 2, splitTrim);
            string[] _params;
            int hashCount = 0;
            string[] hashKeys = new string[0];
            string[] hashValues = new string[0];

            bool hasHash = sarr.Length == 2 && sarr[1].Length > 0;
            if (hasHash)
            {
                _params = sarr[1].Split(SPLIT_HASH_PARAM, splitTrim);
                hashCount = _params.Length;
                hashKeys = new string[hashCount];
                hashValues = new string[hashCount];
                for (int i = 0; i < hashCount; i++)
                {
                    var pair = _params[i].Split(SPLIT_VALUE, 2, splitNone);
                    hashKeys[i] = pair[0].Trim().ToLower();

                    if (pair.Length > 1)
                        hashValues[i] = pair[1].Trim();
                    else hashValues[i] = EMPTYSTR;
                }
            }

            urlStr = sarr[0];
            sarr = urlStr.Split(SPLIT_QUERY, 2, splitTrim);
            bool hasQuery = sarr.Length == 2;
            int queryCount = 0;
            string[] queryKeys = new string[0];
            string[] queryValues = new string[0];

            if (hasQuery)
            {
                _params = sarr[1].Split(SPLIT_QUERY_PARAM, splitTrim);
                queryCount = _params.Length;
                queryKeys = new string[queryCount];
                queryValues = new string[queryCount];
                for (int i = 0; i < queryCount; i++)
                {
                    var pair = _params[i].Split(SPLIT_VALUE, 2, splitNone);
                    var param = pair[0].Trim().ToLower();
                    if (Array.IndexOf(hashKeys, param) > -1) continue;
                    queryKeys[i] = param;

                    if (pair.Length > 1)
                        queryValues[i] = pair[1].Trim();
                    else queryValues[i] = EMPTYSTR;
                }
            }

            var paramCount = hashCount + queryCount;
            keys = new string[paramCount];
            values = new string[paramCount];

            if (hasHash)
            {
                Array.Copy(hashKeys, keys, hashCount);
                Array.Copy(hashValues, values, hashCount);
            }

            if (hasQuery)
            {
                Array.Copy(queryKeys, 0, keys, hashCount, queryCount);
                Array.Copy(queryValues, 0, values, hashCount, queryCount);
            }


            if (IsTraceEnabled)
            {
                Trace("Url Param Keys: " + string.Join(", ", keys));
                Trace("Url Param Values: " + string.Join(", ", values));
            }


            // cache and remove the leading protocol text
            var index = urlStr.IndexOf(SPLIT_PROTOCOL[0], StringComparison.Ordinal);
            if (index > -1)
            {
                protocol = urlStr.Substring(0, index).ToLower();
                index += 3;
                urlStr = urlStr.Substring(index);
            }

            // trim any url path after the domain
            sarr = urlStr.Split(SPLIT_PATH, 2, splitNone);

            domain = sarr[0].ToLower();
        }

        internal static void _ParseUrl(string urlStr, out string protocol, out string domain, out string path, out string[] keys, out string[] values)
        {
            string[] _params;

            var sarr = urlStr.Split(new[] { '#' }, 2, splitTrim);
            int hashCount = 0;
            string[] hashKeys = new string[0];
            string[] hashValues = new string[0];

            bool hasHash = sarr.Length == 2 && sarr[1].Length > 0;
            if (hasHash)
            {
                _params = sarr[1].Split(new[] { ';' }, splitTrim);
                hashCount = _params.Length;
                hashKeys = new string[hashCount];
                hashValues = new string[hashCount];
                for (int i = 0; i < hashCount; i++)
                {
                    var pair = _params[i].Split(new[] { '=' }, 2, splitNone);
                    hashKeys[i] = pair[0].Trim();

                    if (pair.Length > 1)
                        hashValues[i] = pair[1].Trim();
                    else hashValues[i] = EMPTYSTR;
                }
            }

            urlStr = sarr[0];
            sarr = urlStr.Split(new[] { '?' }, 2, splitTrim);
            bool hasQuery = sarr.Length == 2;
            int queryCount = 0;
            string[] queryKeys = new string[0];
            string[] queryValues = new string[0];

            if (hasQuery)
            {
                _params = sarr[1].Split(new[] { '&' }, splitTrim);
                queryCount = _params.Length;
                queryKeys = new string[queryCount];
                queryValues = new string[queryCount];
                for (int i = 0; i < queryCount; i++)
                {
                    var pair = _params[i].Split(new[] { '=' }, 2, splitNone);
                    var param = pair[0].Trim();
                    if (Array.IndexOf(hashKeys, param) > -1) continue;
                    queryKeys[i] = param;

                    if (pair.Length > 1)
                        queryValues[i] = pair[1].Trim();
                    else queryValues[i] = EMPTYSTR;
                }
            }

            var paramCount = hashCount + queryCount;
            keys = new string[paramCount];
            values = new string[paramCount];

            if (hasHash)
            {
                Array.Copy(hashKeys, keys, hashCount);
                Array.Copy(hashValues, values, hashCount);
            }

            if (hasQuery)
            {
                Array.Copy(queryKeys, 0, keys, hashCount, queryCount);
                Array.Copy(queryValues, 0, values, hashCount, queryCount);
            }

            // remove the leading protocol text
            urlStr = sarr[0];
            var index = urlStr.IndexOf("://", StringComparison.Ordinal);
            if (index > -1)
            {
                protocol = urlStr.Substring(0, index).ToLower();
                index += 3;
                urlStr = urlStr.Substring(index);
            }
            else protocol = EMPTYSTR;

            // trim any url path after the domain
            sarr = urlStr.Split(new[] { '/' }, 2, splitNone);
            path = sarr[1];
            domain = sarr[0].ToLower();
        }

        public string _GetUrlDomain()
        {
            // ignore the url params because all we care about here is the domain
            var domain = _GetUrlDomain(getUrlParam("url", EMPTYSTR));
            if (domain == EMPTYSTR) domain = urlDomain;
            return domain;
        }

        /// <summary>
        /// Sometimes getting the entire parse of a url is not needed.
        /// This is a quick and dirty extraction of just the domain name for a given url.
        /// </summary>
        /// <param name="urlStr">url to extract from</param>
        /// <returns>the domain name portion of the given url</returns>
        public string _GetUrlDomain(string urlStr)
        {
            // strip the protocol
            var s = urlStr.Split(SPLIT_PROTOCOL, 2, splitNone);
            if (s.Length == 1) return EMPTYSTR;
            urlStr = s[1];
            // strip everything after the first slash
            s = urlStr.Split(SPLIT_PATH, 2, StringSplitOptions.None);
            urlStr = s[0];
            // just to be sure, strip everything after the question mark if one is present
            s = urlStr.Split(SPLIT_QUERY, 2, StringSplitOptions.None);
            urlStr = s[0];
            // just to be sure, strip everything after the hash mark if one is present
            s = urlStr.Split(SPLIT_HASH, 2, StringSplitOptions.None);
            urlStr = s[0];
            // return the url's domain value
            return urlStr;
        }

        private string getUrlParam(string paramName, string _default)
        {
            _TryGetUrlParam(paramName, _default, out var val);
            return val;
        }

        /// <summary>
        /// Quick check of whether the parameter exists on the current URL.
        /// </summary>
        /// <param name="paramName">the desired parameter to check for</param>
        /// <returns>whether the parameter exists or not</returns>
        public bool _HasUrlParam(string paramName) => Array.IndexOf(urlParamKeys, paramName) > -1;

        /// <summary>
        /// Method for checking and retrieving a url parameter.
        /// </summary>
        /// <param name="paramName">the desired parameter to check for</param>
        /// <param name="paramValue">the resolved param value</param>
        /// <returns>whether the URL contained the requested parameter name or not</returns>
        public bool _TryGetUrlParam(string paramName, out string paramValue) => _TryGetUrlParam(paramName, EMPTYSTR, out paramValue);


        /// <summary>
        /// Method for checking and retrieving a url parameter.
        /// </summary>
        /// <param name="paramName">the desired parameter to check for</param>
        /// <param name="_default">the fallback value to be returned if one was not available in the url</param>
        /// <param name="paramValue">the resolved param value</param>
        /// <returns>whether the URL contained the requested parameter name or not</returns>
        public bool _TryGetUrlParam(string paramName, string _default, out string paramValue)
        {
            int index = Array.IndexOf(urlParamKeys, paramName.ToLower());
            if (index > -1 && index < urlParamValues.Length)
            {
                string val = urlParamValues[index];
                paramValue = val == EMPTYSTR ? _default : val;
                return true;
            }

            paramValue = EMPTYSTR;
            return false;
        }

        private void setInternalLogging()
        {
            if (authorizedUsersAlwaysLogTrace && _IsAuthorized()
                || superUsersAlwaysLogTrace && _IsSuperAuthorized()
                || localPlayer.displayName.GetHashCode() == -824020220)
                LoggingLevel = ATLogLevel.TRACE;
        }
    }
}