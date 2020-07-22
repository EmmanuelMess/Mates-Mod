using System;
using Verse;

namespace MatesMod {
    [StaticConstructorOnStartup]
    public static class MatesMod {
        private const bool Debug = true;
        
        public static void Log(String msg) {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
#pragma warning disable 162
            if (!Debug) return;
#pragma warning restore 162
            
            Verse.Log.Message("Mate: " + msg);
        }
    }
}