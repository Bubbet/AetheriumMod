﻿#undef DEBUG

using Aetherium.CoreModules;
using Aetherium.Equipment;
using Aetherium.Interactables;
using Aetherium.Items;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using R2API.Networking;
using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Aetherium
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(BuffAPI), nameof(LanguageAPI), nameof(ResourcesAPI),
                              nameof(PrefabAPI), nameof(SoundAPI), nameof(OrbAPI),
                              nameof(NetworkingAPI), nameof(EffectAPI), nameof(DirectorAPI))]
    public class AetheriumPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.KomradeSpectre.Aetherium";
        public const string ModName = "Aetherium";
        public const string ModVer = "0.5.1";

        internal static BepInEx.Logging.ManualLogSource ModLogger;

        public static AssetBundle MainAssets;
        public static Shader HopooShader = Resources.Load<Shader>("shaders/deferred/hgstandard");
        public static Shader IntersectionShader = Resources.Load<Shader>("shaders/fx/hgintersectioncloudremap");
        public static Shader CloudRemapShader = Resources.Load<Shader>("shaders/fx/hgcloudremap");

        public List<CoreModule> CoreModules = new List<CoreModule>();
        public List<ItemBase> Items = new List<ItemBase>();
        public List<EquipmentBase> Equipments = new List<EquipmentBase>();

        // For modders that seek to know whether or not one of the items or equipment are enabled for use in...I dunno, adding grip to Blaster Sword?
        public static Dictionary<ItemBase, bool> ItemStatusDictionary = new Dictionary<ItemBase, bool>();
        public static Dictionary<EquipmentBase, bool> EquipmentStatusDictionary = new Dictionary<EquipmentBase, bool>();

        private void Awake()
        {
#if DEBUG
            Logger.LogWarning("DEBUG mode is enabled! Ignore this message if you are actually debugging.");
            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
#endif

            ModLogger = this.Logger;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Aetherium.aetherium_assets"))
            {
                MainAssets = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider("@Aetherium", MainAssets);
                ResourcesAPI.AddProvider(provider);
            }

            //Material shader autoconversion
            var materialAssets = MainAssets.LoadAllAssets<Material>();

            ModLogger.LogInfo("Intersection Shader is: " + IntersectionShader);

            foreach(Material material in materialAssets)
            {
                if (!material.shader.name.StartsWith("Fake")) { continue; }

                switch (material.shader.name.ToLower())
                {
                    case ("fake ror/hopoo games/deferred/hgstandard"):

                        material.shader = HopooShader;

                        break;

                    case ("fake ror/hopoo games/fx/hgcloud intersection remap"):

                        material.shader = IntersectionShader;

                        break;

                    case ("fake ror/hopoo games/fx/hgcloud remap"):

                        material.shader = CloudRemapShader;

                        break;
                }
            }


            //Core Initializations
            var CoreModuleTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(CoreModule)));

            ModLogger.LogInfo("--------------CORE MODULES---------------------");

            foreach (var coreModuleType in CoreModuleTypes)
            {
                CoreModule coreModule = (CoreModule)Activator.CreateInstance(coreModuleType);

                coreModule.Init();

                ModLogger.LogInfo("Core Module: " + coreModule.Name + " Initialized!");
            }

            //Achievement

            //Item Initialization
            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

            ModLogger.LogInfo("----------------------ITEMS--------------------");

            foreach (var itemType in ItemTypes)
            {
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                if (ValidateItem(item, Items))
                {
                    item.Init(Config);

                    ModLogger.LogInfo("Item: " + item.ItemName + " Initialized!");
                }
            }

            //Equipment Initialization
            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EquipmentBase)));

            ModLogger.LogInfo("-----------------EQUIPMENT---------------------");

            foreach (var equipmentType in EquipmentTypes)
            {
                EquipmentBase equipment = (EquipmentBase)System.Activator.CreateInstance(equipmentType);
                if (ValidateEquipment(equipment, Equipments))
                {
                    equipment.Init(Config);

                    ModLogger.LogInfo("Equipment: " + equipment.EquipmentName + " Initialized!");
                }
            }

            new BuffBrazier().Init(Config);

            ModLogger.LogInfo("-----------------------------------------------");
            ModLogger.LogInfo("AETHERIUM INITIALIZATIONS DONE");
            ModLogger.LogInfo($"Items Enabled: {ItemStatusDictionary.Count}");
            ModLogger.LogInfo($"Equipment Enabled: {EquipmentStatusDictionary.Count}");
            ModLogger.LogInfo("-----------------------------------------------");


        }

        public bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            var enabled = Config.Bind<bool>("Item: " + item.ItemName, "Enable Item?", true, "Should this item appear in runs?").Value;
            var aiBlacklist = Config.Bind<bool>("Item: " + item.ItemName, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;

            ItemStatusDictionary.Add(item, enabled);

            if (enabled)
            {
                itemList.Add(item);
                if (aiBlacklist)
                {
                    item.AIBlacklisted = true;
                }
            }
            return enabled;
        }

        public bool ValidateEquipment(EquipmentBase equipment, List<EquipmentBase> equipmentList)
        {
            var enabled = Config.Bind<bool>("Equipment: " + equipment.EquipmentName, "Enable Equipment?", true, "Should this equipment appear in runs?").Value;

            EquipmentStatusDictionary.Add(equipment, enabled);

            if (enabled)
            {
                equipmentList.Add(equipment);
                return true;
            }
            return false;
        }
    }
}