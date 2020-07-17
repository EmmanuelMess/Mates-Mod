using RimWorld;
using Verse;
using Verse.AI;

namespace MatesMod {
    public class JoyGiver_DrinkMate : JoyGiver_InteractBuilding
    {
        protected override bool CanInteractWith(Pawn pawn, Thing t, bool inBed)
        {
            if (!base.CanInteractWith(pawn, t, inBed))
                return false;
            if (!inBed)
                return true;
            Building_Bed bed = pawn.CurrentBed();
            return WatchBuildingUtility.CanWatchFromBed(pawn, bed, t);
        }

        protected override Job TryGivePlayJob(Pawn pawn, Thing t)
        {
            IntVec3 result;
            Building chair;
            return !WatchBuildingUtility.TryFindBestWatchCell(t, pawn, this.def.desireSit, out result, out chair) ? (Job) null : JobMaker.MakeJob(this.def.jobDef, (LocalTargetInfo) t, (LocalTargetInfo) result, (LocalTargetInfo) (Thing) chair);
        }
    }
}