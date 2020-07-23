using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MatesMod {
  // ReSharper disable once InconsistentNaming
  // ReSharper disable once UnusedType.Global
  public class JobDriver_DrinkMate : JobDriver {
    private const TargetIndex GatherSpotParentInd = TargetIndex.A;
    private const TargetIndex ChairOrSpotInd = TargetIndex.B;
    private const TargetIndex MateInd = TargetIndex.C;

    private Thing GatherSpotParent => job.GetTarget(GatherSpotParentInd).Thing;

    private bool HasChair => job.GetTarget(ChairOrSpotInd).HasThing;

    private IntVec3 ClosestGatherSpotParentCell => GatherSpotParent.OccupiedRect().ClosestCellTo(pawn.Position);

    public override bool TryMakePreToilReservations(bool errorOnFailed) {
      return pawn.Reserve(job.GetTarget(ChairOrSpotInd), job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils() {
      this.EndOnDespawnedOrNull(GatherSpotParentInd);
      if (HasChair) {
        this.EndOnDespawnedOrNull(ChairOrSpotInd);
      }
      this.FailOnDestroyedNullOrForbidden(MateInd);
      yield return Toils_Goto.GotoThing(MateInd, PathEndMode.OnCell)
        .FailOnSomeonePhysicallyInteracting(MateInd);
      yield return Toils_Haul.StartCarryThing(MateInd);
      yield return Toils_Goto.GotoCell(ChairOrSpotInd, PathEndMode.OnCell);
      var chew = new Toil {
        tickAction = delegate {
          pawn.rotationTracker.FaceCell(ClosestGatherSpotParentCell);
          pawn.GainComfortFromCellIfPossible();
          JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.GoToNextToil);
        },
        handlingFacing = true,
        defaultCompleteMode = ToilCompleteMode.Delay,
        defaultDuration = job.def.joyDuration, 
        socialMode = RandomSocialMode.SuperActive
      };
      chew.AddFinishAction(delegate {
        JoyUtility.TryGainRecRoomThought(pawn);
      });
      Toils_Ingest.AddIngestionEffects(chew, pawn, MateInd, TargetIndex.None);
      yield return chew;
      
      Toil toilDrinkMate = new Toil();
      toilDrinkMate.initAction = () => {
        var ingester = pawn;
        var actor = toilDrinkMate.actor;
        var curJob = actor.jobs.curJob;
        var thing = curJob.GetTarget(MateInd).Thing;
        if (ingester.needs.mood != null 
            && thing.def.IsNutritionGivingIngestible 
            && thing.def.ingestible.chairSearchRadius > 10.0) {
          var room = ingester.GetRoom();
          if (room != null) {
            var scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness));
            if (ThoughtDefOf.AteInImpressiveDiningRoom.stages[scoreStageIndex] != null)
              ingester.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(ThoughtDefOf.AteInImpressiveDiningRoom, scoreStageIndex));
          }
        }

        var num2 = thing.Ingested(ingester, ingester.needs.food.NutritionWanted);
        if (!ingester.Dead)
          ingester.needs.food.CurLevel += num2;
        ingester.records.AddTo(RecordDefOf.NutritionEaten, num2);
      };
      toilDrinkMate.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return toilDrinkMate;
    }

    public override bool ModifyCarriedThingDrawPos(
      ref Vector3 drawPos,
      ref bool behind,
      ref bool flip) {
      IntVec3 gatherSpotParentCell = ClosestGatherSpotParentCell;
      return JobDriver_Ingest.ModifyCarriedThingDrawPosWorker(ref drawPos, ref behind, ref flip, gatherSpotParentCell, pawn);
    }
    
    
  }
}