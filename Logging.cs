using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Linq;
using System.Reflection;

namespace Unleasharp.DB.Base;

public static class Logging {
    private static bool                                   _LoggerOverriden = false;
    private static ILoggerFactory                         _loggerFactory   = CreateDefaultFactory();
    private static LogLevel                               _minimumLogLevel = LogLevel.Error;
    private static Action<SimpleConsoleFormatterOptions>? _configureConsoleAction;

    public static  ILogger CreateLogger<T>() => _loggerFactory.CreateLogger<T>();

    static Logging() {
        TryAdoptAspNetLoggerFactory();
    }

    public static void SetLoggerFactory(ILoggerFactory factory) {
        _loggerFactory   = factory;
        _LoggerOverriden = true;
    }

    public static void SetMinimumLogLevel(LogLevel logLevel) {
        // Logger has been overriden, managing it does not belong to this class anymore
        if (_LoggerOverriden) {
            return;
        }

        _minimumLogLevel = logLevel;
        _loggerFactory   = CreateDefaultFactory();
    }

    public static void SetLoggingOptions(Action<SimpleConsoleFormatterOptions> action) {
        // Logger has been overriden, managing it does not belong to this class anymore
        if (_LoggerOverriden) {
            return;
        }

        _configureConsoleAction = action;
        _loggerFactory          = CreateDefaultFactory();
    }

    private static ILoggerFactory CreateDefaultFactory() {
        return LoggerFactory.Create(builder => {
            builder.AddSimpleConsole(options => {
                // Default good format
                options.SingleLine      = true;
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] "; // 👈 ms precision
                options.UseUtcTimestamp = false;
                options.IncludeScopes   = true;

                // Allow external override if provided
                _configureConsoleAction?.Invoke(options);
            })
           .SetMinimumLevel(_minimumLogLevel);
        });
    }

    private static void TryAdoptAspNetLoggerFactory() {
        try {
            // Look for Microsoft.Extensions.DependencyInjection.ServiceProvider
            var hostingAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Microsoft.Extensions.Hosting");

            if (hostingAssembly == null)
                return;

            // Look for Generic Host's ServiceProvider
            var hostType = hostingAssembly.GetType("Microsoft.Extensions.Hosting.Internal.Host");
            if (hostType == null)
                return;

            // Search for static field/property holding the ServiceProvider
            var serviceProviderField = hostType
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => typeof(IServiceProvider).IsAssignableFrom(f.FieldType));

            if (serviceProviderField == null)
                return;

            // Get active host instance
            var activeHost = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == "Program"); // crude but works for most ASP.NET apps

            if (activeHost == null)
                return;

            var hostInstance = serviceProviderField.GetValue(activeHost);
            if (hostInstance is IServiceProvider sp) {
                var loggerFactory = sp.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                if (loggerFactory != null) {
                    _loggerFactory   = loggerFactory;
                    _LoggerOverriden = true;
                }
            }
        }
        catch {
            // swallow errors – fallback to default console logger
        }
    }
}