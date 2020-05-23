using System;
using System.Runtime.Serialization;

namespace JsonEditor
{
    /// <summary>
    /// Класс отвечает за исключения, возникшие в результате некоректных данных выданных JsonEditor.
    /// </summary>
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