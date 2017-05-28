/*
 * Copyright 2017 Hans Uhlig.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Empyrion
{
    public class HttpManagerConfiguration
    {
        public bool enabled { get; set; }
        public int port { get; set; }
        public bool whitelist { get; set; }
        public bool authentication { get; set; }
        public List<string> addresses { get; set; }
        public Dictionary<string, string> users { get; set; }
    }
    public class HttpManager
    {
        private readonly HttpManagerConfiguration _config;
        private readonly ScriptManager _scriptManager;
        private readonly ServerPlugin _serverPlugin;
        private readonly HttpListener _httplistener;
        private readonly UriRouter _httpRouter;

        public HttpManager(HttpManagerConfiguration config, ScriptManager scriptManager, ServerPlugin serverPlugin)
        {
            _config = config;
            _scriptManager = scriptManager;
            _serverPlugin = serverPlugin;
            if (_config.enabled)
            {
                _httpRouter = new UriRouter();
                _httplistener = new HttpListener();
                _httplistener.Prefixes.Add("http://*:" + _config.port + "/");
                if (_config.authentication)
                {
                    _httplistener.AuthenticationSchemes = AuthenticationSchemes.Basic;
                }
                else
                {
                    _httplistener.AuthenticationSchemes = AuthenticationSchemes.None;
                }
                _httpRouter.DefineRoute("/script/execute/:script", (req, res, parameters) =>
                {
                    LogMessage("Running Script " + parameters["script"]);
                    if (_scriptManager.ExecuteScript(parameters["script"]))
                    {
                        res.StatusCode = 200;
                        res.StatusDescription = "OK";
                    }
                    else
                    {
                        res.StatusCode = 404;
                        res.StatusDescription = "NOT FOUND";
                    }
                });
                _httpRouter.DefineRoute("/script/trigger/:trigger", (req, res, parameters) =>
                {
                    string trigger = parameters["trigger"];
                    IDictionary<string, string> queryParams = new Dictionary<string, string>();
                    foreach (var k in req.QueryString.AllKeys)
                    {
                        queryParams.Add(k, req.QueryString[k]);
                    }
                    LogMessage("Triggering Function '" + trigger + "' with " + ToString(queryParams));
                    if (_scriptManager.ExecuteTrigger(trigger, queryParams))
                    {
                        res.StatusCode = 200;
                        res.StatusDescription = "OK";
                    }
                    else
                    {
                        res.StatusCode = 404;
                        res.StatusDescription = "NOT FOUND";
                    }
                });
            }
        }

        public void StartServer()
        {
            if (_config.enabled)
            {
                _httplistener.Start();
                new Thread(() =>
                {
                    while (_httplistener.IsListening)
                    {
                        HttpListenerContext ctx = _httplistener.GetContext();
                        ThreadPool.QueueUserWorkItem((_) =>
                        {
                            string ipAddress = ctx.Request.RemoteEndPoint.Address.ToString();
                            string requestPath = ctx.Request.Url.AbsolutePath;
                            if (_config.whitelist)
                            {
                                if (!_config.addresses.Contains(ipAddress))
                                {
                                    LogMessage("Connection Refused from " + ipAddress + " to " + ctx.Request.Url.AbsolutePath);
                                    return;
                                }
                            }
                            string username = "unknown";
                            if (_config.authentication)
                            {
                                if (ctx.User.Identity.IsAuthenticated)
                                {
                                    HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)ctx.User.Identity;
                                    username = identity.Name;
                                    if (!_config.users.ContainsKey(username))
                                    {
                                        LogMessage("Connection Refused from " + ipAddress + " to " + requestPath
                                            + " for user " + username + ". User not found.");
                                        return;
                                    }
                                    if (!_config.users[identity.Name].Equals(identity.Password))
                                    {
                                        LogMessage("Connection Refused from " + ipAddress + " to " + requestPath
                                            + " for user " + username + ". Incorrect Password.");
                                        return;
                                    }

                                }
                                else
                                {
                                    LogMessage("Connection Refused from " + ipAddress + " to " + requestPath + " due to lack of credentials.");
                                    return;
                                }
                            }

                            LogMessage("Connection Accepted from " + ipAddress + " to " + requestPath + " for user " + username + ".");

                            _httpRouter.Route(ctx.Request.Url.AbsolutePath.Trim().ToLower(), ctx.Request, ctx.Response);

                            ctx.Response.Close();
                        });
                    }
                }).Start();
                if (_httplistener.IsListening)
                {
                    LogMessage("Started REST Server on Port " + _config.port);
                }
                else
                {
                    LogMessage("REST Server Disabled");
                }
            }
        }

        public void StopServer()
        {
            _httplistener.Close();
        }

        public void LogMessage(string message)
        {
            _serverPlugin.LogMessage("Http", message);
        }

        private string ToString(IDictionary<string, string> dictionary)
        {
            var str = new StringBuilder();
            str.Append("{ ");
            foreach (var pair in dictionary)
            {
                str.Append(String.Format("'{0}' = '{1}', ", pair.Key, pair.Value));
            }
            str.Length = str.Length - 2;
            str.Append(" }");
            return str.ToString();
        }
    }

    internal class UriRouter
    {
        // Delegate with a context object and the route parameters as parameters
        public delegate void MethodDelegate(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters);

        // Internal class storage for route definitions
        protected class RouteDefinition
        {
            public MethodDelegate Method;
            public string RoutePath;
            public Regex RouteRegEx;

            public RouteDefinition(string route, MethodDelegate method)
            {
                RoutePath = route;
                Method = method;

                // Build RegEx from route (:foo to named group (?<foo>[a-z0-9]+)).
                var routeFormat = new Regex("(:([a-z]+))\\b").Replace(route, "(?<$2>[A-Za-z0-9-_\\.]+)");

                // Build the match uri parameter to that regex.
                RouteRegEx = new Regex(routeFormat);
            }
        }

        private readonly List<RouteDefinition> _routes;

        public UriRouter()
        {
            _routes = new List<RouteDefinition>();
        }

        public void DefineRoute(string route, MethodDelegate method)
        {
            _routes.Add(new RouteDefinition(route, method));
        }

        public void Route(string uri, HttpListenerRequest req, HttpListenerResponse res)
        {
            foreach (var route in _routes)
            {
                // Execute the regex to check whether the uri correspond to the route
                var match = route.RouteRegEx.Match(uri);

                if (!match.Success)
                {
                    continue;
                }

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                string[] groups = route.RouteRegEx.GetGroupNames();
                for (int i = 1; i < groups.Length; i++)
                {
                    string groupName = groups[i];
                    if (match.Groups[groupName].Success && match.Groups[groupName].Captures.Count > 0)
                    {
                        parameters[groupName] = match.Groups[groupName].Value;
                    }
                }

                // Invoke the method
                route.Method.Invoke(req, res, parameters);

                // Only the first match is executed
                return;
            }

            // No match found
            throw new Exception("No match found");
        }
    }

}