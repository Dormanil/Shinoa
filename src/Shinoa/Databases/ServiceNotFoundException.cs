namespace Shinoa.Databases
{
    using System;
    using System.Runtime.Serialization;

    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException()
            : base()
        {
        }

        public ServiceNotFoundException(string message)
            : base(message)
        {
        }

        public ServiceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ServiceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
