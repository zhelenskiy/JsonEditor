using System;
using System.Runtime.Serialization;

namespace JsonEditor
{
    internal class JsonEditorException : Exception
    {
        public JsonEditorException()
        {
        }

        public JsonEditorException(string message) : base(message)
        {
        }

        public JsonEditorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected JsonEditorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}