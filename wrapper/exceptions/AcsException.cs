
using System;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.exceptions
{
    public class AcsException : Exception, IAcsException
    {
        public AcsException(string message) : base(message)
        {
        }
    }
}