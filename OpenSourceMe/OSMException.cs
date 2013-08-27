using System;

namespace OpenSourceMe
{
    internal class OSMException : Exception
    {
        internal OSMException(string _mess) : base(_mess) {}
    }

    internal class OSMCriticalException : Exception
    {
        internal OSMCriticalException(string _mess) : base(_mess) {}
    }
}
