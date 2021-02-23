// Copyright (c) iterate GmbH.
// Licensed under Apache License, Version 2.0.

using Microsoft.Windows.Sdk;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static Microsoft.Windows.Sdk.Constants;
using static Microsoft.Windows.Sdk.PInvoke;

RootCommand app;

unsafe static void FindHandler(FileInfo resourceDll, string[] text)
{
    using var library = new FreeLibrarySafeHandle(LoadLibrary(resourceDll.Name));

    var langId = (ushort)CultureInfo.CurrentUICulture.LCID;

    char* localRT_STRING = (char*)RT_STRING;
    var result = Enumerable.Range(0, ushort.MaxValue + 1).Select<int, (int, Exception, string)>(x =>
    {
        var id = x / 16 + 1;
        var localId = (char*)id;
        var hrsrc = FindResourceEx(library.DangerousGetHandle(), localRT_STRING, localId, langId);
        if (hrsrc == 0)
        {
            return (x, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()), default);
        }

        var hglob = LoadResource(library.DangerousGetHandle(), hrsrc);
        if (hglob == 0)
        {
            return (x, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()), default);
        }

        var pwsz = (char*)LockResource(hglob);
        if (pwsz is null)
        {
            return (x, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()), default);
        }
        for (int i = 0; i < (x & 15); i++)
        {
            pwsz += 1 + (uint)*pwsz;
        }
        var length = *pwsz;
        pwsz += 1;

        var res = new string(pwsz, 0, length);
        return (x, default, res);
    }).Where(x => x.Item2 is not null || !string.IsNullOrWhiteSpace(x.Item3));

    var map = result.ToLookup(x => x.Item2 is null);

    var join = ParallelEnumerable.Join(map[true].AsParallel(), text.AsParallel(), x => x.Item3, x => x, (match, _) => (id: match.Item1, match: match.Item3), SimpleEqualityComparer.Default);

    foreach (var (id, match) in join.OrderBy(x => x.id))
    {
        Console.WriteLine($"{id}: {match}");
    }
}

unsafe static void ResolveHandler(FileInfo resourceDll, uint[] resourceId)
{
    using var library = new FreeLibrarySafeHandle(LoadLibrary(resourceDll.Name));
    foreach (var id in resourceId)
    {
        char* buffer = default;
        var requiredCapacity = LoadString(library, id, (char*)&buffer, 0);
        if (buffer is null)
        {
            Console.WriteLine($"Resource Id \"{id}\" does not exist.");
            return;
        }
        if (requiredCapacity == 0)
        {
            Console.WriteLine($"Resource Id \"{id}\" failed:\n{Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())}");
            return;
        }

        Console.WriteLine($"{id}: {new ReadOnlySpan<char>(buffer, requiredCapacity).ToString()}");
    }
}

app = new RootCommand()
{
    new Argument<FileInfo>("resourceDll"),
    new Command("find")
    {
        new Argument<string[]>("text")
    }.Configure(x => x.Handler = CommandHandler.Create<FileInfo, string[]>(FindHandler)),
    new Command("resolve")
    {
        new Argument<uint[]>("resourceId")
    }.Configure(x => x.Handler = CommandHandler.Create<FileInfo, uint[]>(ResolveHandler))
};

return await app.InvokeAsync(Environment.CommandLine);

internal static class Extensions
{
    public static T Configure<T>(this T t, Action<T> configure)
    {
        configure(t);
        return t;
    }
}

internal class SimpleEqualityComparer : IEqualityComparer<string>
{
    public static SimpleEqualityComparer Default { get; } = new SimpleEqualityComparer();

    public bool Equals(string x, string y) => x.IndexOf(y, StringComparison.OrdinalIgnoreCase) > -1 || y.IndexOf(x, StringComparison.OrdinalIgnoreCase) > -1;

    public int GetHashCode([DisallowNull] string obj) => -1;
}
