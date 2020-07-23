using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MatesMod {

  // ReSharper disable once UnusedType.Global
  // ReSharper disable once InconsistentNaming
  public class JoyGiver_DrinkMate : JoyGiver {
    private static readonly List<CompGatherSpot> WorkingSpots =
      new List<CompGatherSpot>();

    private static readonly int NumRadiusCells =
      GenRadial.NumCellsInRadius(3.9f);

    private static readonly List<IntVec3> RadialPatternMiddleOutward =
      GenRadial.RadialPattern
        .Take(NumRadiusCells)
        .OrderBy(c =>
          Mathf.Abs((c - IntVec3.Zero).LengthHorizontal - 1.95f))
        .ToList();

    private static readonly ThingDef MateDef = DefDatabase<ThingDef>.GetNamed("Mate");

    public override Job TryGiveJob(Pawn pawn) {
      MatesMod.Log("TryGiveJob");

      return TryGiveJobInt(pawn, null);
    }

    public override Job TryGiveJobInGatheringArea(Pawn pawn,
      IntVec3 gatheringSpot) {
      MatesMod.Log("TryGiveJobInGatheringArea");

      return TryGiveJobInt(pawn,
        x =>
          GatheringsUtility.InGatheringArea(x.parent.Position, gatheringSpot,
            pawn.Map));
    }

    private Job TryGiveJobInt(Pawn pawn,
      Predicate<CompGatherSpot> gatherSpotValidator) {
      if (pawn.Map.gatherSpotLister.activeSpots.Count == 0) {
        MatesMod.Log("pawn.Map.gatherSpotLister.activeSpots.Count == 0");
        return null;
      }

      WorkingSpots.Clear();
      foreach (var gatherSpot in pawn.Map.gatherSpotLister.activeSpots) {
        WorkingSpots.Add(gatherSpot);
      }
      
      MatesMod.Log("workingSpots.Count() = " + WorkingSpots.Count());

      while (WorkingSpots.TryRandomElement(out var result1)) {
        WorkingSpots.Remove(result1);

        if (result1.parent.IsForbidden(pawn)
            || !pawn.CanReach((LocalTargetInfo) result1.parent, PathEndMode.Touch, Danger.None)
            || (!result1.parent.IsSociallyProper(pawn) || !result1.parent.IsPoliticallyProper(pawn))
            || (gatherSpotValidator != null && !gatherSpotValidator(result1))) {
          continue;
        }

        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
            || !TryFindIngestibleToNurse(result1.parent.Position, pawn, out var ingestible)) {
          continue;
        }
        
        Job job;
        if (result1.parent.def.surfaceType == SurfaceType.Eat) {
          if (!TryFindChairBesideTable(result1.parent, pawn, out var chair)) {
            return null;
          }

          job = JobMaker.MakeJob(def.jobDef, 
            (LocalTargetInfo) result1.parent,
            (LocalTargetInfo) chair);
        } else {
          if (TryFindChairNear(result1.parent.Position, pawn, out var chair)) {
            job = JobMaker.MakeJob(
              def.jobDef, 
              (LocalTargetInfo) result1.parent, 
              (LocalTargetInfo) chair
            );
          } else {
            return null;
          }
        }

        job.targetC = (LocalTargetInfo) ingestible;
        job.count = 1;
            
        MatesMod.Log("job = " + job);
        return job;
      }
      
      MatesMod.Log("dead");
      return null;
    }

    private static bool TryFindIngestibleToNurse(IntVec3 center, Pawn ingester,
      out Thing ingestible) {
      if (ingester.IsTeetotaler()) {
        ingestible = null;
        return false;
      }

      if (ingester.drugs == null) {
        ingestible = null;
        return false;
      }

      var thingList = ingester.Map.listerThings.ThingsOfDef(MateDef);
      MatesMod.Log("thingList.Count() = " + thingList.Count);
      if (thingList.Count > 0) {
        bool Validator(Thing t) => ingester.CanReserve((LocalTargetInfo) t) && !t.IsForbidden(ingester);
        
        ingestible = GenClosest.ClosestThing_Global_Reachable(center,
          ingester.Map, thingList, PathEndMode.OnCell,
          TraverseParms.For(ingester), 40f, Validator);

        if (ingestible != null) {
          return true;
        }
      }

      ingestible = null;
      return false;
    }

    private static bool TryFindChairBesideTable(Thing table, Pawn sitter,
      out Thing chair) {
      for (var index = 0; index < 30; ++index) {
        var edifice = table.RandomAdjacentCellCardinal().GetEdifice(table.Map);
        if (edifice == null
            || !edifice.def.building.isSittable
            || !sitter.CanReserve((LocalTargetInfo) edifice)) {
          continue;
        }
        chair = edifice;
        return true;
      }

      chair = null;
      return false;
    }

    private static bool TryFindChairNear(IntVec3 center, Pawn sitter, out Thing chair) {
      foreach (var radialPattern in RadialPatternMiddleOutward) {
        Building edifice = (center + radialPattern).GetEdifice(sitter.Map);
        if (edifice == null
            || !edifice.def.building.isSittable
            || (!sitter.CanReserve((LocalTargetInfo) edifice) || edifice.IsForbidden(sitter))
            || !GenSight.LineOfSight(center, edifice.Position, sitter.Map, true)) {
          continue;
        }
        chair = edifice;
        return true;
      }

      chair = null;
      return false;
    }
  }
}