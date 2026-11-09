using ExampleMod.Content.Biomes;
using ExampleMod.Content.Liquids;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Liquid;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExampleMod.Common.GlobalLiquids
{
	//The GlobalLiquid class can do everything ModLiquid can do, and more.
	//Allowing the editing of both vanilla and other modded liquids.
	//Here we do a range of different edits to vanilla liquids to show how to use this global class.
	public class ExampleGlobalLiquid : GlobalLiquid
	{
		// EvaporatesInHell allows us to specify which liquids evaporate and doesn't evaporate in the underworld under certain condition (or not).
		// This allows us to do things such as prevent water from evaporating and allowing other liquids to evaporate
		public override bool? CanWaterEvaporateInUnderworld() {
			// In this small example, water is only allowed to evaporate if it's pre hardmode
			return !Main.hardMode;
		}

		// In this example, we change how lava and water should interact before making a LiquidMerge.
		// If the EoC is defeated and the water and lava come in contact, there is 50% chance it will spawn an obsidian pillar instead of doing a regular tile merge
		public override bool PreMerge(int liquidX, int liquidY, int tileX, int tileY, int type, int otherLiquid) {
			// We start by checking if lava and water are the 2 liquid colliding together
			if (type == LiquidID.Water && otherLiquid == LiquidID.Lava ||
				type == LiquidID.Lava && otherLiquid == LiquidID.Water) {
				// Then we check if the Eye of Cthulhu has been defeated and run a Main.rand.NextBool() to determine if we generate an obsdian pillar or not
				if (NPC.downedBoss1 && Main.rand.NextBool()) {
					// If the check pass, we start making the pillar
					for (int i = tileY; i > tileY - 10; i--) {
						// We need to check if the pillar is within the bound of the world, if it isn't get out of the loop
						if (!WorldGen.InWorld(tileX, i)) {
							break;
						}

						Tile tile = Main.tile[tileX, i];
						tile.HasTile = true;
						tile.TileType = TileID.Obsidian;
						WorldGen.SquareTileFrame(tileX, i);
						if (Main.netMode != NetmodeID.MultiplayerClient) {
							NetMessage.SendTileSquare(-1, tileX, i, TileChangeType.None);
						}
					}
					// Then return false to prevent LiquidMerge from executing
					return false;
				}
			}

			// This second example here make it so Lava and Honey does not merge at all if they are in the underworld, allowing them to co-exist next to each other
			// if (type == LiquidID.Lava && otherLiquid == LiquidID.Honey ||
			//	type == LiquidID.Honey && otherLiquid == LiquidID.Lava && liquidY > Main.UnderworldLayer) {
			//	// By returning false, it effectively prevent the liquid from doing any sort of merging and make it so that LiquidMerge does not run in this case
			//	return false;
			//}
			return base.PreMerge(liquidX, liquidY, tileX, tileY, type, otherLiquid);
		}

		//Like modded liquids, we can also change the merge type of a vanilla liquid.
		//Here we set the merge between lava and shimmer to be orichalcum ore.
		// We also make so that lava and water have a 50/50 chance to produce either obsidian or obsidian brick
		public override int? Merge(int type, int otherLiquid) {
			// Due to the nature of how liquids work, we need to check both way for the interaction, as it depend on which liquid flow first in the other liquid.
			// In this case, we are checking if lava and shimmer collide, then make orichalcum
			if ((type == LiquidID.Lava && otherLiquid == LiquidID.Shimmer) || (type == LiquidID.Shimmer && otherLiquid == LiquidID.Lava)) {
				return TileID.Orichalcum;
			}

			// In this case, we check for lava and water colliding.
			if (type == LiquidID.Water && otherLiquid == LiquidID.Lava ||
				type == LiquidID.Lava && otherLiquid == LiquidID.Water) {
				// Then we check if the eye of cthulhu as been defeated
				if (NPC.downedBoss1) {
					// And finally we do use Main.rand.NextBool() to determine if obsidian (true) or Ancient Obsidian Brick (false will generate)
					return Main.rand.NextBool() ? TileID.Obsidian : TileID.AncientObsidianBrick;
				}
			}
			return null;
		}

		// In this example, we make it so that shimmer cannot be pumped if the moonlord was not defeated in the world
		public override bool CanPump(int inLiquidType, int inX, int inY, int outX, int outY) {
			// We check if the current liquid type is shimmer, if it is then we can check if the moon was not defeated.
			if (inLiquidType == LiquidID.Shimmer && !NPC.downedMoonlord) {
				// If this is the case, then we cannot pump shimmer until the moon lord has been defeated
				return false;
			}
			return base.CanPump(inLiquidType, inX, inY, outX, outY);
		}

		//AnimateLiquid in a GlobalLiquid returns a boolean
		//using this boolean, we can toggle the normal vanilla animating code or not
		//with that, we disable the shimmer's animation, forcing it to be stuck on the first frame
		public override bool AnimateLiquid(int type, GameTime gameTime, ref int frame, ref float frameState) {
			if (type == LiquidID.Shimmer) {
				return false;
			}
			return true;
		}
	}
}
