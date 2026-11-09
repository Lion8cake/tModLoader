using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader.Core;
using static Terraria.ModLoader.BackupIO;

namespace Terraria.ModLoader;

public static class LiquidLoader
{
	private static int nextLiquid = LiquidID.Count;

	internal static readonly IList<ModLiquid> liquids = new List<ModLiquid>();

	internal static readonly IList<GlobalLiquid> globalLiquids = new List<GlobalLiquid>();

	private static bool loaded = false;

	public static int LiquidCount => nextLiquid;

	public static Asset<Texture2D>[] LiquidAssets = new Asset<Texture2D>[4];
	public static Asset<Texture2D>[] LiquidBlockAssets = new Asset<Texture2D>[4];
	public static Asset<Texture2D>[] LiquidSlopeAssets = new Asset<Texture2D>[4];

	internal static int ReserveLiquidID()
	{
		if (nextLiquid >= LiquidData.MaxLiquidTypes) {
			throw new Exception($"Cannot add more than {LiquidData.MaxLiquidTypes} liquid types");
		}

		int result = nextLiquid;
		nextLiquid++;
		return result;
	}

	/// <summary>
	/// Gets the ModLiquid instance with the given type. If no ModLiquid with the given type exists, returns null.
	/// </summary>
	public static ModLiquid GetLiquid(int type)
	{
		if (type < LiquidID.Count || type >= LiquidCount) {
			return null;
		}
		return liquids[type - LiquidID.Count];
	}

	internal static void ResizeArrays(bool unloading = false)
	{
		if (!Main.dedServ) {
			LiquidRenderer.Instance._cache = new LiquidRenderer.LiquidCache[1];
			LiquidRenderer.Instance._drawCache = new LiquidRenderer.LiquidDrawCache[1];
		}
		Array.Resize(ref LiquidAssets, nextLiquid);
		Array.Resize(ref LiquidBlockAssets, nextLiquid);
		Array.Resize(ref LiquidSlopeAssets, nextLiquid);
		Array.Resize(ref LiquidRenderer.WATERFALL_LENGTH, nextLiquid);
		Array.Resize(ref LiquidRenderer.DEFAULT_OPACITY, nextLiquid);
		Array.Resize(ref LiquidRenderer.WAVE_MASK_STRENGTH, nextLiquid);
		Array.Resize(ref LiquidRenderer.VISCOSITY_MASK, nextLiquid);
		Array.Resize(ref Main.liquidFrame, nextLiquid);
		Array.Resize(ref Main.liquidFrameState, nextLiquid);

		LoaderUtils.ResetStaticMembers(typeof(LiquidID));

		ModLoader.BuildGlobalHook(ref HookModifyLight, globalLiquids, (GlobalLiquid g) => g.ModifyLight);
		ModLoader.BuildGlobalHook(ref HookPreDraw, globalLiquids, (GlobalLiquid g) => g.PreDraw);
		ModLoader.BuildGlobalHook(ref HookPostDraw, globalLiquids, (GlobalLiquid g) => g.PostDraw);
		ModLoader.BuildGlobalHook(ref HookEmitEffects, globalLiquids, (GlobalLiquid g) => g.EmitEffects);
		ModLoader.BuildGlobalHook(ref HookDrawWaterfall, globalLiquids, (GlobalLiquid g) => g.ChooseWaterfallStyle);
		ModLoader.BuildGlobalHook(ref HookPreUpdate, globalLiquids, (GlobalLiquid g) => g.PreUpdate);
		ModLoader.BuildGlobalHook(ref HookEvaporation, globalLiquids, (GlobalLiquid g) => g.CanWaterEvaporateInUnderworld);
		ModLoader.BuildGlobalHook(ref HookSettleLiquidMovement, globalLiquids, (GlobalLiquid g) => g.SettleLiquidMovement);
		ModLoader.BuildGlobalHook(ref HookMergeTiles, globalLiquids, (GlobalLiquid g) => g.Merge);
		ModLoader.BuildGlobalHook(ref HookMergeTilesSounds, globalLiquids, (GlobalLiquid g) => g.MergeSound);
		ModLoader.BuildGlobalHook(ref HookPreMerge, globalLiquids, (GlobalLiquid g) => g.PreMerge);
		ModLoader.BuildGlobalHook(ref HookAllowFishing, globalLiquids, (GlobalLiquid g) => g.CanFish);
		ModLoader.BuildGlobalHook(ref HookModifyTilesNearby, globalLiquids, (GlobalLiquid g) => g.ModifyNearbyTiles);
		ModLoader.BuildGlobalHook(ref HookOnPump, globalLiquids, (GlobalLiquid g) => g.CanPump);
		ModLoader.BuildGlobalHook(ref HookOnPlayerCollision, globalLiquids, (GlobalLiquid g) => g.OnPlayerColliding);
		ModLoader.BuildGlobalHook(ref HookOnNPCCollision, globalLiquids, (GlobalLiquid g) => g.OnNPCColliding);
		ModLoader.BuildGlobalHook(ref HookOnProjectileCollision, globalLiquids, (GlobalLiquid g) => g.OnProjectileColliding);
		ModLoader.BuildGlobalHook(ref HookAnimateLiquid, globalLiquids, (GlobalLiquid g) => g.AnimateLiquid);
		ModLoader.BuildGlobalHook(ref HookNPCRippleModifier, globalLiquids, (GlobalLiquid g) => g.NPCRippleModifier);
		ModLoader.BuildGlobalHook(ref HookPlayerRippleModifier, globalLiquids, (GlobalLiquid g) => g.PlayerRippleModifier);

		if (!unloading) {
			loaded = true;
		}
	}

	internal static void Unload()
	{
		loaded = false;
		liquids.Clear();
		nextLiquid = LiquidID.Count;
		globalLiquids.Clear();
	}

	private delegate void DelegateModifyLight(int i, int j, int type, ref float r, ref float g, ref float b);
	private static DelegateModifyLight[] HookModifyLight;
	public static void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b)
	{
		GetLiquid(type)?.ModifyLight(i, j, ref r, ref g, ref b);
		foreach (var hook in HookModifyLight) {
			hook(i, j, type, ref r, ref g, ref b);
		}
	}

	private static Func<int, int, int, LiquidRenderer.LiquidDrawCache, Vector2, bool, int, float, bool>[] HookPreDraw;
	public static bool PreDraw(int i, int j, int type, LiquidRenderer.LiquidDrawCache liquidDrawCache, Vector2 drawOffset, bool isBackgroundDraw, int waterStyle, float globalAlpha)
	{
		foreach (var hook in HookPreDraw) {
			if (!hook(i, j, type, liquidDrawCache, drawOffset, isBackgroundDraw, waterStyle, globalAlpha)) {
				return false;
			}
		}
		return GetLiquid(type)?.PreDraw(i, j, liquidDrawCache, drawOffset, isBackgroundDraw) ?? true;
	}

	private static Action<int, int, int, LiquidRenderer.LiquidDrawCache, Vector2, bool, int, float>[] HookPostDraw;
	public static void PostDraw(int i, int j, int type, LiquidRenderer.LiquidDrawCache liquidDrawCache, Vector2 drawOffset, bool isBackgroundDraw, int waterStyle, float globalAlpha)
	{
		GetLiquid(type)?.PostDraw(i, j, liquidDrawCache, drawOffset, isBackgroundDraw);
		foreach (var hook in HookPostDraw) {
			hook(i, j, type, liquidDrawCache, drawOffset, isBackgroundDraw, waterStyle, globalAlpha);
		}
	}
	private static Func<int, int, int, LiquidRenderer.LiquidCache, bool>[] HookEmitEffects;
	public static bool EmitEffects(int i, int j, byte type, LiquidRenderer.LiquidCache liquidCache)
	{
		GetLiquid(type)?.EmitEffects(i, j, liquidCache);
		foreach (var hook in HookEmitEffects) {
			if (hook(i, j, type, liquidCache)) {
				return true;
			}
		}
		return false;
	}

	private static Func<int, int, int, int?>[] HookDrawWaterfall;
	public static int? DrawWaterfall(int i, int j, int type)
	{
		foreach (var hook in HookDrawWaterfall) {
			int? result = hook(i, j, type);
			if (result != null)
				return result;
		}
		return GetLiquid(type)?.ChooseWaterfallStyle(i, j) ?? null;
	}

	private static Func<int, int, int, Liquid, bool>[] HookPreUpdate;
	public static bool PreUpdate(int i, int j, int type, Liquid liquid)
	{
		foreach (var hook in HookPreUpdate) {
			if (!hook(i, j, type, liquid)) {
				return false;
			}
		}
		return GetLiquid(type)?.PreUpdate(i, j, liquid) ?? true;
	}

	private static Func<bool?>[] HookEvaporation;
	public static bool CanWaterEvaporateInUnderworld(int i, int j)
	{
		foreach (var hook in HookEvaporation) {
			bool? result = hook();
			if (result != null)
				return result.Value;
		}
		return true;
	}

	private static Func<int, int, int, bool>[] HookSettleLiquidMovement;
	public static bool SettleLiquidMovement(int i, int j, int type)
	{
		foreach (var hook in HookSettleLiquidMovement) {
			bool result = hook(i, j, type);
			if (!result) {
				return false;
			}
		}
		return GetLiquid(type)?.SettleLiquidMovement(i, j) ?? true;
	}

	private static Func<int, int, int?>[] HookMergeTiles;
	public static int? MergeTilesType(int type, int otherLiquid)
	{
		foreach (var t in HookMergeTiles) {
			int? result = t(type, otherLiquid);
			if (result != null)
				return result;
		}
		ModLiquid modLiquid = GetLiquid(type);
		ModLiquid otherModLiquid = GetLiquid(otherLiquid);
		if (modLiquid == null && otherModLiquid != null) {
			return otherModLiquid?.Merge(type) ?? null;
		}
		return modLiquid?.Merge(otherLiquid) ?? null;
	}

	private delegate void DelegateLiquidMergeTilesSound(int i, int j, int type, int otherLiquid, ref SoundStyle? collisionSound);
	private static DelegateLiquidMergeTilesSound[] HookMergeTilesSounds;
	public static void MergeSounds(int i, int j, int type, int otherLiquid, ref SoundStyle? collisionSound)
	{
		ModLiquid modLiquid = GetLiquid(type);
		ModLiquid otherModLiquid = GetLiquid(otherLiquid);
		if (modLiquid == null && otherModLiquid != null)
			otherModLiquid?.MergeSound(i, j, type, ref collisionSound);
		else
			modLiquid?.MergeSound(i, j, otherLiquid, ref collisionSound);

		foreach (var hook in HookMergeTilesSounds) {
			hook(i, j, type, otherLiquid, ref collisionSound);
		}
	}

	private static Func<int, int, int, int, int, int, bool>[] HookPreMerge;
	public static bool PreMerge(int liquidX, int liquidY, int tileX, int tileY, int type, int otherLiquid)
	{
		foreach (var hook in HookPreMerge) {
			if (!hook(liquidX, liquidY, tileX, tileY, type, otherLiquid)) {
				return false;
			}
		}
		ModLiquid modLiquid = GetLiquid(type);
		ModLiquid otherModLiquid = GetLiquid(otherLiquid);
		if (LoaderUtils.HasOverride(otherModLiquid, (Expression<Func<ModLiquid, Func<int, int, int, int, int, bool>>>)((ModLiquid t) => t.PreMerge))) {
			return otherModLiquid?.PreMerge(liquidX, liquidY, tileX, tileY, type) ?? true;
		}
		if (modLiquid == null && otherModLiquid != null) {
			return otherModLiquid?.PreMerge(liquidX, liquidY, tileX, tileY, type) ?? true;
		}
		return modLiquid?.PreMerge(liquidX, liquidY, tileX, tileY, otherLiquid) ?? true;
	}

	private static Func<int, Player, Projectile, bool?>[] HookAllowFishing;
	public static bool? CanFish(int liquidType, Player player, Projectile projectile)
	{
		ModLiquid modLiquid = GetLiquid(liquidType);
		bool? result = null;
		if (modLiquid != null) {
			result = modLiquid.CanFish(player, projectile);
		}
		return result;
	}

	private static Action<int, int, int, int, int>[] HookModifyTilesNearby;
	public static void ModifyNearbyTiles(int x, int y, int type, int liquidX, int liquidY)
	{
		GetLiquid(type)?.ModifyNearbyTiles(x, y, liquidX, liquidY);
		foreach (var hook in HookModifyTilesNearby) {
			hook(x, y, type, liquidX, liquidY);
		}
	}

	private static Func<int, int, int, int, int, bool>[] HookOnPump;
	public static bool CanPump(int type, int x, int y, int x2, int y2)
	{
		foreach (var hook in HookOnPump) {
			if (!hook(type, x, y, x2, y2)) {
				return false;
			}
		}
		return GetLiquid(type)?.CanPump(x, y, x2, y2) ?? true;
	}

	private static Action<Player, int>[] HookOnPlayerCollision;
	public static void OnPlayerColliding(int type, Player player)
	{
		GetLiquid(type)?.OnPlayerColliding(player);
		foreach (var hook in HookOnPlayerCollision) {
			hook(player, type);
		}
	}

	private static Action<NPC, int>[] HookOnNPCCollision;
	public static void OnNPCColliding(int type, NPC npc)
	{
		GetLiquid(type)?.OnNPCColliding(npc);
		foreach (var hook in HookOnNPCCollision) {
			hook(npc, type);
		}
	}

	private static Action<Projectile, int>[] HookOnProjectileCollision;
	public static void OnProjectileColliding(int type, Projectile proj)
	{
		GetLiquid(type)?.OnProjectileColliding(proj);
		foreach (var hook in HookOnProjectileCollision) {
			hook(proj, type);
		}
	}

	private delegate void DelegateModLiquidAnimateLiquid(GameTime gameTime, ref int frame, ref float frameState);
	private delegate bool DelegateAnimateLiquid(int type, GameTime gameTime, ref int frame, ref float frameState);
	private static DelegateAnimateLiquid[] HookAnimateLiquid;
	public static bool AnimateLiquid(int type, GameTime gameTime, ref int frame, ref float frameState)
	{
		bool hasOverride = false;
		ModLiquid modLiquid = GetLiquid(type);
		if (modLiquid != null) {
			if (LoaderUtils.HasOverride(modLiquid, (Expression<Func<ModLiquid, DelegateModLiquidAnimateLiquid>>)((ModLiquid t) => t.AnimateLiquid))) {
				hasOverride = true;
			}
			modLiquid.AnimateLiquid(gameTime, ref frame, ref frameState);
		}

		if (HookAnimateLiquid != null) {
			foreach (var hook in HookAnimateLiquid) {
				if (!hook(type, gameTime, ref frame, ref frameState)) {
					return false;
				}
			}
		}
		return !hasOverride;
	}

	private delegate void DelegateNPCRippleModifier(NPC npc, int type, ref float rippleStrength, ref float rippleOffset);
	private static DelegateNPCRippleModifier[] HookNPCRippleModifier;
	public static void NPCRippleModifier(int type, NPC npc, ref float rippleStrength, ref float rippleOffset)
	{
		GetLiquid(type)?.NPCRippleModifier(npc, ref rippleStrength, ref rippleOffset);
		foreach (var hook in HookNPCRippleModifier) {
			hook(npc, type, ref rippleStrength, ref rippleOffset);
		}
	}

	private delegate void DelegatePlayerRippleModifier(Player player, int type, ref float rippleStrength, ref float rippleOffset);
	private static DelegatePlayerRippleModifier[] HookPlayerRippleModifier;
	public static void PlayerRippleModifier(int type, Player player, ref float rippleStrength, ref float rippleOffset)
	{
		GetLiquid(type)?.PlayerRippleModifier(player, ref rippleStrength, ref rippleOffset);
		foreach (var hook in HookPlayerRippleModifier) {
			hook(player, type, ref rippleStrength, ref rippleOffset);
		}
	}
}
