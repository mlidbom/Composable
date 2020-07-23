using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Composable.Persistence.EventStore;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ReflectionCE;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Composable.Logging
{

    enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    class ConsoleLogger : ILogger
    {
        readonly Type _type;

        LogLevel _logLevel = LogLevel.Info;

        ConsoleLogger(Type type) => _type = type;

        public static ILogger Create(Type type) => new ConsoleLogger(type);
        public ILogger WithLogLevel(LogLevel level) => new ConsoleLogger(_type){_logLevel =  level};
        public void Error(Exception exception, string? message)
        {
            if(_logLevel >= LogLevel.Error)
            {
                SafeConsole.WriteLine($@"
############################################# ERROR in : {_type.GetFullNameCompilable()} #############################################
MESSAGE: {message} 
EXCEPTION: {exception}

{(exception is AggregateException aggregateException
      ? $@"
############################################# SERIALIZED AGGREGATE EXCEPTION #############################################
{SerializeExceptions(aggregateException.InnerExceptions)}"
      : $@"
############################################# SERIALIZED EXCEPTION #############################################
{SerializeException(exception)}")}

############################################# END ERROR #############################################
");
            }
        }



        static string SerializeExceptions(ReadOnlyCollection<Exception> exceptions) =>
            exceptions.Select((exception, index) => $@"

############################################# INNER EXCEPTION {index + 1} #############################################
{SerializeException(exception)}
############################################# END EXCEPTION {index + 1} #############################################

").Join(string.Empty);

        static string SerializeException(Exception exception)
        {
            try
            {
                return JsonConvert.SerializeObject(exception, Formatting.Indented, ExceptionSerializationSettings);
            }
            catch(Exception e)
            {
                return $"Serialization Failed with message: {e.Message}";
            }
        }

        public void Warning(string message)
        {
            if(_logLevel >= LogLevel.Warning)
            {
                SafeConsole.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        public void Warning(Exception exception, string message)
        {
            if(_logLevel >= LogLevel.Warning)
            {
                SafeConsole.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}, \n: Exception: {exception}");
            }
        }

        public void Info(string message)
        {
            if(_logLevel >= LogLevel.Info)
            {
                SafeConsole.WriteLine($"INFO:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }
        public void Debug(string message)
        {
            if(_logLevel >= LogLevel.Debug)
            {
                SafeConsole.WriteLine($"DEBUG:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        [StringFormatMethod(formatParameterName:"message")]
        public void DebugFormat(string message, params object[] arguments) => StringCE.FormatInvariant(message, arguments);

        static readonly JsonSerializerSettings ExceptionSerializationSettings =
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ContractResolver = IgnoreStackTraces.Instance
            };

        class IgnoreStackTraces : IncludeMembersWithPrivateSettersResolver
        {
            public new static readonly IgnoreStackTraces Instance = new IgnoreStackTraces();
            IgnoreStackTraces()
            {
                IgnoreSerializableInterface = true;
                IgnoreSerializableAttribute = true;
            }
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                if(property.PropertyName == nameof(Exception.StackTrace))
                {
                    property.Ignored = true;
                }

                if(property.PropertyName == "StackTraceString")
                {
                    property.Ignored = true;
                }

                return property;
            }
        }
    }
}
