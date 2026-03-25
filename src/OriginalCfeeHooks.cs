using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Asta.CfeeLowVersionFix;

internal static class OriginalCfeeHooks
{
    private const BindingFlags AnyMethod =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static readonly HashSet<string> InstalledTargets = new(StringComparer.Ordinal);
    private static MethodInfo? _generateEggLineMethod;
    private static bool _loggedGenerateEggLineFailure;

    internal static void Install(Harmony harmony)
    {
        TryInstallBypassPatch(
            harmony,
            fullTypeName: "testModLethalCompany.Patches.CoordinateForEasterEggs",
            fallbackTypeName: "CoordinateForEasterEggs",
            methodName: "SetExplodeOnThrowClinetRPCExtension",
            prefixName: nameof(SkipOriginalSetExplodeExtension));

        TryInstallBypassPatch(
            harmony,
            fullTypeName: "testModLethalCompany.EggLineSync",
            fallbackTypeName: "EggLineSync",
            methodName: "EggLinesClientRpc",
            prefixName: nameof(SkipEggLinesClientRpc));
    }

    internal static bool TryGenerateEggLine(Vector3 position, bool explodeOnThrow)
    {
        MethodInfo? method = ResolveGenerateEggLineMethod();
        if (method is null)
        {
            return false;
        }

        try
        {
            method.Invoke(obj: null, parameters: new object[] { position, explodeOnThrow });
            return true;
        }
        catch (Exception exception)
        {
            CfeeLowVersionFixPlugin.Log.LogError($"Failed to invoke CoordinateForEasterEggs.GenerateEggLine: {exception}");
            return false;
        }
    }

    private static bool SkipOriginalSetExplodeExtension()
    {
        return false;
    }

    private static bool SkipEggLinesClientRpc()
    {
        return false;
    }

    private static void TryInstallBypassPatch(
        Harmony harmony,
        string fullTypeName,
        string fallbackTypeName,
        string methodName,
        string prefixName)
    {
        Type? targetType = FindType(fullTypeName, fallbackTypeName);
        if (targetType is null)
        {
            return;
        }

        MethodInfo? targetMethod = targetType
            .GetMethods(AnyMethod)
            .FirstOrDefault(method => string.Equals(method.Name, methodName, StringComparison.Ordinal));

        if (targetMethod is null)
        {
            return;
        }

        string targetKey = $"{targetMethod.DeclaringType?.FullName}.{targetMethod.Name}";
        if (!InstalledTargets.Add(targetKey))
        {
            return;
        }

        MethodInfo? prefixMethod = typeof(OriginalCfeeHooks).GetMethod(prefixName, AnyMethod);
        if (prefixMethod is null)
        {
            CfeeLowVersionFixPlugin.Log.LogWarning($"Bypass prefix '{prefixName}' could not be found.");
            return;
        }

        harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
        CfeeLowVersionFixPlugin.Log.LogInfo($"Installed bypass for {targetKey}.");
    }

    private static MethodInfo? ResolveGenerateEggLineMethod()
    {
        if (_generateEggLineMethod is not null)
        {
            return _generateEggLineMethod;
        }

        Type? coordinateType = FindType(
            fullName: "testModLethalCompany.Patches.CoordinateForEasterEggs",
            fallbackName: "CoordinateForEasterEggs");

        if (coordinateType is null)
        {
            LogMissingGenerateEggLine("CoordinateForEasterEggs type was not found.");
            return null;
        }

        _generateEggLineMethod = coordinateType
            .GetMethods(AnyMethod)
            .FirstOrDefault(method =>
            {
                if (!string.Equals(method.Name, "GenerateEggLine", StringComparison.Ordinal))
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(Vector3)
                    && parameters[1].ParameterType == typeof(bool);
            });

        if (_generateEggLineMethod is null)
        {
            LogMissingGenerateEggLine("CoordinateForEasterEggs.GenerateEggLine(Vector3, bool) was not found.");
        }

        return _generateEggLineMethod;
    }

    private static Type? FindType(string fullName, string fallbackName)
    {
        Type? type = AccessTools.TypeByName(fullName);
        if (type is not null)
        {
            return type;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                type = assembly.GetType(fullName, throwOnError: false);
                if (type is not null)
                {
                    return type;
                }

                type = assembly.GetTypes().FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, fallbackName, StringComparison.Ordinal));

                if (type is not null)
                {
                    return type;
                }
            }
            catch (ReflectionTypeLoadException exception)
            {
                type = exception.Types.FirstOrDefault(candidate =>
                    candidate is not null
                    && (string.Equals(candidate.FullName, fullName, StringComparison.Ordinal)
                        || string.Equals(candidate.Name, fallbackName, StringComparison.Ordinal)));

                if (type is not null)
                {
                    return type;
                }
            }
        }

        return null;
    }

    private static void LogMissingGenerateEggLine(string message)
    {
        if (_loggedGenerateEggLineFailure)
        {
            return;
        }

        _loggedGenerateEggLineFailure = true;
        CfeeLowVersionFixPlugin.Log.LogWarning(message);
    }
}
