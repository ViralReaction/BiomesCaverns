﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Caveworld_Flora_Unleashed
{
	public static class GenCaveFungusReproduction
	{
		public static void TryGetRandomMyceliumSpawnCell(ThingDef_FruitingBody plantDef, int newDesiredMyceliumSize, bool checkTemperature, Map map, out IntVec3 spawnCell)
		{
			spawnCell = IntVec3.Invalid;
			if (!CellFinderLoose.TryGetRandomCellWith(validator, map, 1000, out spawnCell))
			{
				spawnCell = IntVec3.Invalid;
			}
			bool validator(IntVec3 cell)
			{
				if (!IsValidPositionToGrowPlant(plantDef, map, cell, checkTemperature))
				{
					return false;
				}
				if (!IsMyceliumAreaClear(plantDef, newDesiredMyceliumSize, map, cell))
				{
					return false;
				}
				return true;
			}
		}

		public static FruitingBody TryGrowMycelium(Mycelium Mycelium, bool checkTemperature = true)
		{
			if (Mycelium.actualSize >= Mycelium.desiredSize)
			{
				return null;
			}
			TryGetRandomSpawnCellNearMycelium(Mycelium, checkTemperature, out var spawnCell);
			if (spawnCell.IsValid)
			{
				FruitingBody newPlant = ThingMaker.MakeThing(Mycelium.plantDef) as FruitingBody;
				GenSpawn.Spawn(newPlant, spawnCell, Mycelium.Map);
				newPlant.Mycelium = Mycelium;
				Mycelium.NotifyPlantAdded();
				return newPlant;
			}
			return null;
		}

		public static void TryGetRandomSpawnCellNearMycelium(Mycelium Mycelium, bool checkTemperature, out IntVec3 spawnCell)
		{
			spawnCell = IntVec3.Invalid;
			float maxSpawnDistance = GenRadial.RadiusOfNumCells(Mycelium.actualSize + 1);
			maxSpawnDistance += 2f;
			if (!CellFinder.TryFindRandomCellNear(Mycelium.Position, Mycelium.Map, (int)maxSpawnDistance, validator, out spawnCell))
			{
				spawnCell = IntVec3.Invalid;
			}
			bool validator(IntVec3 cell)
			{
				if (!cell.InHorDistOf(Mycelium.Position, maxSpawnDistance))
				{
					return false;
				}
				Room MyceliumRoom = Mycelium.GetRoom();
				Room cellRoom = cell.GetRoom(Mycelium.Map);
				if (cellRoom == null || cellRoom != MyceliumRoom)
				{
					return false;
				}
				return IsValidPositionToGrowPlant(Mycelium.plantDef, Mycelium.Map, cell, checkTemperature);
			}
		}

		public static FruitingBody TrySpawnNewMyceliumAwayFrom(Mycelium Mycelium)
		{
			int newDesiredMyceliumSize = Mycelium.plantDef.MyceliumSizeRange.RandomInRange;
			TryGetRandomSpawnCellAwayFromMycelium(Mycelium, newDesiredMyceliumSize, out var spawnCell);
			if (spawnCell.IsValid)
			{
				return Mycelium.SpawnNewMyceliumAt(Mycelium.Map, spawnCell, Mycelium.plantDef, newDesiredMyceliumSize);
			}
			return null;
		}

		public static void TryGetRandomSpawnCellAwayFromMycelium(Mycelium Mycelium, int newDesiredMyceliumSize, out IntVec3 spawnCell)
		{
			spawnCell = IntVec3.Invalid;
			float newMyceliumExclusivityRadius = Mycelium.GetExclusivityRadius(Mycelium.plantDef, newDesiredMyceliumSize);
			float newMyceliumMinDistance = Mycelium.ExclusivityRadius + newMyceliumExclusivityRadius;
			float newMyceliumMaxDistance = 2f * newMyceliumMinDistance;
			if (!CellFinder.TryFindRandomCellNear(Mycelium.Position, Mycelium.Map, (int)newMyceliumMaxDistance, validator, out spawnCell))
			{
				spawnCell = IntVec3.Invalid;
			}
			bool validator(IntVec3 cell)
			{
				if (cell.InHorDistOf(Mycelium.Position, newMyceliumMinDistance))
				{
					return false;
				}
				if (!cell.InHorDistOf(Mycelium.Position, newMyceliumMaxDistance))
				{
					return false;
				}
				if (cell.GetRoom(Mycelium.Map) != Mycelium.GetRoom())
				{
					return false;
				}
				if (!IsValidPositionToGrowPlant(Mycelium.plantDef, Mycelium.Map, cell))
				{
					return false;
				}
				if (!IsMyceliumAreaClear(Mycelium.plantDef, newDesiredMyceliumSize, Mycelium.Map, cell))
				{
					return false;
				}
				return true;
			}
		}

		public static bool IsMyceliumAreaClear(ThingDef_FruitingBody plantDef, int newDesiredMyceliumSize, Map map, IntVec3 position)
		{
			float newMyceliumExclusivityRadius = Mycelium.GetExclusivityRadius(plantDef, newDesiredMyceliumSize);
			foreach (Thing thing in map.listerThings.ThingsOfDef(Util_Caveworld_Flora_Unleashed.MyceliumDef))
			{
				Mycelium Mycelium = thing as Mycelium;
				if (Mycelium.plantDef != plantDef || !Mycelium.Position.InHorDistOf(position, Mycelium.ExclusivityRadius + newMyceliumExclusivityRadius))
				{
					continue;
				}
				return false;
			}
			return true;
		}

		public static bool IsValidPositionToGrowPlant(ThingDef_FruitingBody plantDef, Map map, IntVec3 position, bool checkTemperature = true)
		{
			if (!position.InBounds(map))
			{
				return false;
			}
			if (position.GetEdifice(map) != null || position.GetCover(map) != null)
			{
				return false;
			}
			if (!FruitingBody.CanTerrainSupportPlantAt(plantDef, map, position))
			{
				return false;
			}
			if (checkTemperature && !FruitingBody.IsTemperatureConditionOkAt(plantDef, map, position))
			{
				return false;
			}
			if (!FruitingBody.IsLightConditionOkAt(plantDef, map, position))
			{
				return false;
			}
			Thing plant = map.thingGrid.ThingAt(position, ThingCategory.Plant);
			if (plant != null && !(plant is Mycelium))
			{
				return false;
			}
			List<Thing> thingList = map.thingGrid.ThingsListAt(position);
			for (int thingIndex = 0; thingIndex < thingList.Count; thingIndex++)
			{
				Thing thing = thingList[thingIndex];
				if (!(thing is Mycelium))
				{
					if (thing.def.category == ThingCategory.Plant || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Pawn)
					{
						return false;
					}
				}
			}
			if (!PlantUtility.SnowAllowsPlanting(position, map))
			{
				return false;
			}
			return true;
		}
	}

}