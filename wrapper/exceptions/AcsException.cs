
using System;
using CO.Systems.Services.Acs.AcsWrapper.wrapper.status;

namespace CO.Systems.Services.Acs.AcsWrapper.wrapper.exceptions
{
    public class AcsException : Exception, IAcsException
    {

        public ConveyorErrorCode ConveyorErrorCode { get; }

        public AcsException(string message) : base(message)
        {
        }

        public AcsException(string message, ConveyorErrorCode conveyorErrorCode) : this(message)
        {
            ConveyorErrorCode = conveyorErrorCode;
        }
    }
}