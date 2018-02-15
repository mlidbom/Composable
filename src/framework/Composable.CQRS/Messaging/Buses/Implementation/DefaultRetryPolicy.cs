﻿using System;

namespace Composable.Messaging.Buses.Implementation
{
    internal class DefaultRetryPolicy
    {
        internal static int Tries = 5;
        int _remainingTries;
        internal DefaultRetryPolicy(BusApi.IMessage message) => _remainingTries = Tries;
        public bool Retry(Exception exception) => --_remainingTries > 0;
    }
}
