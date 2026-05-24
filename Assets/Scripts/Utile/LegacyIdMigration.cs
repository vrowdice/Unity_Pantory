using System;
using System.Collections.Generic;

/// <summary>
/// 구 id(News_, Effect_ 등) → snake_case id 마이그레이션.
/// 세이브 로드 시 <see cref="DataManager"/>에서 호출합니다.
/// </summary>
public static class LegacyIdMigration
{
    private static readonly Dictionary<string, string> IdMap = BuildIdMap();

    public static string MigrateId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return id;
        }

        if (IdMap.TryGetValue(id, out string mapped))
        {
            return mapped;
        }

        return id;
    }

    public static void MigrateSaveData(GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (saveData.activeNews != null)
        {
            for (int i = 0; i < saveData.activeNews.Count; i++)
            {
                NewsState news = saveData.activeNews[i];
                if (news == null)
                {
                    continue;
                }

                news.id = MigrateId(news.id);
            }
        }

        if (saveData.effects == null)
        {
            return;
        }

        MigrateEffectList(saveData.effects.globalEffects);
        MigrateEffectList(saveData.effects.instanceEffects);
    }

    private static void MigrateEffectList(List<GlobalEffectStateSaveData> globalEffects)
    {
        if (globalEffects == null)
        {
            return;
        }

        for (int i = 0; i < globalEffects.Count; i++)
        {
            MigrateEffectStates(globalEffects[i]?.effects);
        }
    }

    private static void MigrateEffectList(List<InstanceEffectStateSaveData> instanceEffects)
    {
        if (instanceEffects == null)
        {
            return;
        }

        for (int i = 0; i < instanceEffects.Count; i++)
        {
            MigrateEffectStates(instanceEffects[i]?.effects);
        }
    }

    private static void MigrateEffectStates(List<EffectState> effects)
    {
        if (effects == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            EffectState state = effects[i];
            if (state == null)
            {
                continue;
            }

            state.id = MigrateRuntimeEffectId(state.id);
        }
    }

    private static string MigrateRuntimeEffectId(string runtimeId)
    {
        if (string.IsNullOrEmpty(runtimeId))
        {
            return runtimeId;
        }

        if (runtimeId.StartsWith("News::", StringComparison.Ordinal) ||
            runtimeId.StartsWith("news::", StringComparison.Ordinal))
        {
            string[] parts = runtimeId.Split(new[] { "::" }, StringSplitOptions.None);
            if (parts.Length >= 4)
            {
                parts[0] = "news";
                parts[1] = MigrateId(parts[1]);
                parts[2] = MigrateId(parts[2]);
                return string.Join("::", parts);
            }
        }

        return MigrateId(runtimeId);
    }

    private static Dictionary<string, string> BuildIdMap()
    {
        Dictionary<string, string> map = new Dictionary<string, string>();

        AddNewsIds(map);
        AddEffectIds(map);
        AddResearchCategoryIds(map);

        return map;
    }

    private static void AddNewsIds(Dictionary<string, string> map)
    {
        string[] newsIds =
        {
            "News_Arms_Race",
            "News_Auto_Surge",
            "News_Aviation_Breakthrough",
            "News_Chip_Shortage",
            "News_Cyber_Monday",
            "News_Gold_Surge",
            "News_Housing_Boom",
            "News_Luxury_Ban",
            "News_Mining_Collapse",
            "News_Oil_Shock",
            "News_Peace_Treaty",
            "News_Steel_Tariffs",
            "News_Textile_Strike",
        };

        for (int i = 0; i < newsIds.Length; i++)
        {
            string oldId = newsIds[i];
            map[oldId] = ToNewsSnakeCase(oldId);
            map[oldId + "_Desc"] = ToNewsSnakeCase(oldId) + "_Desc";
        }
    }

    private static void AddEffectIds(Dictionary<string, string> map)
    {
        string[] effectIds =
        {
            "Effect_Air_Price_Up_25",
            "Effect_Airplane_Price_Down_20",
            "Effect_Alum_Price_Up_15",
            "Effect_AluminumOre_Price_Up_20",
            "Effect_Arms_Price_Up_15",
            "Effect_BCloth_Price_Up_20",
            "Effect_BasicFurniture_Price_Up_20",
            "Effect_Car_Price_Up_30",
            "Effect_Coal_Price_Up_20",
            "Effect_CopIng_Price_Up_30",
            "Effect_CopOre_Price_Up_50",
            "Effect_Elec_Price_Up_35",
            "Effect_Engine_Price_Up_15",
            "Effect_Fabric_Price_Up_40",
            "Effect_FineWood_Price_Up_10",
            "Effect_Gold_Price_Up",
            "Effect_HeArms_Price_Down_40",
            "Effect_HeavyArms_Price_Up_20",
            "Effect_Iron_Price_Up_15",
            "Effect_IronOre_Price_Up_20",
            "Effect_Mach_Price_Up_10",
            "Effect_Muni_Price_Up_20",
            "Effect_Munitions_Price_Down_20",
            "Effect_Oil_Price_Up_40",
            "Effect_PCloth_Price_Down_20",
            "Effect_PFurn_Price_Down_20",
            "Effect_PGold_Price_Down_10",
            "Effect_PrecisionTools_Price_Up_20",
            "Effect_PremiumCloth_Price_Up_20",
            "Effect_PremiumFurniture_Price_Up_20",
            "Effect_PremiumWood_Price_Up_10",
            "Effect_PureGold_Price_Up_20",
            "Effect_SimTool_Price_Up_10",
            "Effect_SmArms_Price_Down_30",
            "Effect_SmallArms_Price_Up_20",
            "Effect_Steel_Price_Up_25",
            "Effect_Tank_Price_Down_20",
            "Effect_Tank_Price_Up_10",
            "Effect_Wood_Price_Up_10",
        };

        for (int i = 0; i < effectIds.Length; i++)
        {
            map[effectIds[i]] = ToEffectSnakeCase(effectIds[i]);
        }
    }

    private static void AddResearchCategoryIds(Dictionary<string, string> map)
    {
        map["UnlockBuilding"] = "unlock_building";
        map["RawResourceSearch"] = "raw_resource_search";
        map["ProductionEfficiency"] = "production_efficiency";
    }

    private static string ToNewsSnakeCase(string newsId)
    {
        if (newsId.StartsWith("News_", StringComparison.Ordinal))
        {
            return "news_" + newsId.Substring(5).ToLowerInvariant();
        }

        return newsId.ToLowerInvariant();
    }

    private static string ToEffectSnakeCase(string effectId)
    {
        if (effectId.StartsWith("Effect_", StringComparison.Ordinal))
        {
            return "effect_" + effectId.Substring(7).ToLowerInvariant();
        }

        return effectId.ToLowerInvariant();
    }
}
