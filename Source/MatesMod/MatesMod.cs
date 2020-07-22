using System;
using Verse;

namespace MatesMod {
    [StaticConstructorOnStartup]
    public static class MatesMod {
        public const bool DEBUG = true;
        
        static MatesMod() {
        }

        public static void Log(String msg) {
            if (!DEBUG) return;
            
            Verse.Log.Message(msg);
        }
    }
}