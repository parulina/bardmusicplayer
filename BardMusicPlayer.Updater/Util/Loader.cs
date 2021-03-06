﻿/*
 * Copyright(c) 2021 MoogleTroupe, trotlinebeercan
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BardMusicPlayer.Updater.Util
{
    internal static class Loader
    {
        private static readonly List<string> Loaded = new();
        internal static async Task<bool> Load(BmpVersion version, string exePath, string resourcePath, string dataPath, string[] cliParameters)
        {
                if (Loaded.Contains(version.entryClass))
                    throw new Exception(version.entryClass + " from " + version.entryDll + " has already been loaded.");


                Type viewsType = null;
                foreach (var item in version.items.Where(item => item.load))
                {
                    if (item.destination.Equals(version.entryDll))
                        viewsType = Assembly
                            .LoadFrom(resourcePath + item.destination)
                            .GetType(version.entryClass);
                    else
                        Assembly.LoadFrom(resourcePath + item.destination);
                }

                dynamic main = Activator.CreateInstance(viewsType ?? throw new InvalidOperationException("Unable to run " + version.entryClass + " from " + version.entryDll));
                Loaded.Add(version.entryClass);
                return await main.StartUp(version.beta, version.build, version.commit, exePath, resourcePath, dataPath, cliParameters);
        }
    }
}
