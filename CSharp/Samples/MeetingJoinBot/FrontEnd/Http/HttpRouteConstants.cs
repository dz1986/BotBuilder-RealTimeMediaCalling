﻿/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

namespace FrontEnd.Http
{
    /// <summary>
    /// HTTP route constants for routing requests to CallController methods.
    /// </summary>
    public static class HttpRouteConstants
    {
        /// <summary>
        /// Route prefix for all incoming requests.
        /// </summary>
        public const string CallSignalingRoutePrefix = "api/calling";

        /// <summary>
        /// Route for incoming calls.
        /// </summary>
        public const string OnIncomingCallRoute = "call";

        /// <summary>
        /// Route for incoming calls.
        /// </summary>
        public const string OnIncomingMessageRoute = "";

        /// <summary>
        /// Route for existing call callbacks.
        /// </summary>
        public const string OnCallbackRoute = "callback";

        /// <summary>
        /// Route for existing call notifications.
        /// </summary>
        public const string OnNotificationRoute = "notification";

        /// <summary>
        /// Route for getting all calls
        /// </summary>
        public const string Calls = "calls";

        /// <summary>
        /// Route for getting Image for a call
        /// </summary>
        public const string Image = "image/{callid}";
    }
}
