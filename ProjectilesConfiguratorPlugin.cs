using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HG.Coroutines;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static RoR2.Skills.SkillFamily;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: HG.Reflection.SearchableAttribute.OptIn]
[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]
[module: UnverifiableCode]
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace ProjectilesConfigurator
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(ModCompatabilities.RiskOfOptionsCompatability.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [System.Serializable]
    public class ProjectilesConfiguratorPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.brynzananas.projectilesconfigurator";
        public const string ModName = "Projectiles Configurator";
        public const string ModVer = "1.0.0";
        public static bool riskOfOptionsEnabled { get; private set; }
        public static ConfigFile configFile { get; private set; }
        public static ManualLogSource Log { get; private set; }
        //public static Dictionary<GameObject, List<ConfigEntryBase>> projectilesConfigs = [];
        public static Dictionary<string, int> names = [];
        public static Action<GameObject> GetProjectileCustomConfigs;
        private static Stopwatch stopwatch;
        public void Awake()
        {
            configFile = Config;
            Log = Logger;
            riskOfOptionsEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ModCompatabilities.RiskOfOptionsCompatability.GUID);
        }
        /*[ConCommand(commandName = "regenerate_skill_configs", flags = ConVarFlags.None)]
        public static void CCReset(ConCommandArgs args)
        {
            Reset();
        }
        public static void Reset()
        {
            skillConfigs.Clear();
            names.Clear();
            ConfigureSkillsStart();
        }*/
        [SystemInitializer(typeof(ProjectileCatalog))]
        private static void ConfigureProjectilesStart()
        {
            Log.LogMessage("Begin configuring projectiles");
            ParallelCoroutine loadCoroutine = new ParallelCoroutine();
            stopwatch = Stopwatch.StartNew();
            stopwatch.Start();
            int i = 0;
            foreach (GameObject projectile in ProjectileCatalog.projectilePrefabs)
            {
                i++;
                loadCoroutine.Add(ConfigureProjectileThread(projectile, i));
            }
            IEnumerator runLoadCoroutine()
            {
                yield return loadCoroutine;
                Log.LogMessage("Finished configuring projectiles. Time took: " + stopwatch.ElapsedMilliseconds + "ms");
                stopwatch.Stop();
            }
            RoR2Application.instance.StartCoroutine(runLoadCoroutine());
        }
        private static IEnumerator ConfigureProjectileThread(GameObject projectile, int loc)
        {
            string sectionName = projectile.name;
            char[] forbiddenCharacters = { '\n', '\t', '\"', '\'', '[', ']' };
            foreach (char forbiddenCharacter in forbiddenCharacters)
            {
                while (sectionName.Contains(forbiddenCharacter))
                {
                    sectionName = sectionName.Replace(forbiddenCharacter, ' ');
                }
            }
            sectionName.Trim();
            if (sectionName.IsNullOrWhiteSpace()) yield break;
            int namesCount = 0;
            while (names.ContainsKey(sectionName + (namesCount == 0 ? "" : namesCount)))
            {
                namesCount++;
            }
            sectionName += (namesCount == 0 ? "" : namesCount);
            names.Add(sectionName, namesCount);
            //if (projectilesConfigs.ContainsKey(projectile)) yield break;
            List<ConfigEntryBase> configEntryBases = [];
            //projectilesConfigs.Add(projectile, configEntryBases);
            yield return null;
            ConfigEntry<bool> enable = CreateConfig(sectionName, "Enable Config", false, "Enable configuration for this projectile?", null, false);
            if (enable.Value)
            {
                yield return null;
                ProjectileSimple projectileSimple = projectile.GetComponent<ProjectileSimple>();
                ConfigEntry<float> desiredForwardSpeed = null;
                ConfigEntry<float> lifetime = null;
                ProjectileExplosion projectileExplosion = projectile.GetComponent<ProjectileExplosion>();
                ConfigEntry<BlastAttack.FalloffModel> falloffModel = null;
                ConfigEntry<float> blastRadius = null;
                ConfigEntry<float> blastDamageCoefficient = null;
                ConfigEntry<float> blastProcCoefficient = null;
                bool hasChildren = false;
                ConfigEntry<int> childrenCount = null;
                ConfigEntry<float> childrenDamageCoefficient = null;
                ConfigEntry<bool> childrenInheritDamageType = null;
                bool canApplyDot = false;
                ConfigEntry<bool> applyDot = null;
                ConfigEntry<float> dotDuration = null;
                ConfigEntry<float> dotDamageMultiplier = null;
                ConfigEntry<bool> calculateTotalDamage = null;
                ConfigEntry<float> totalDamageMultiplier = null;
                ProjectileCharacterController projectileCharacterController = projectile.GetComponent<ProjectileCharacterController>();
                ProjectileImpactExplosion projectileImpactExplosion = projectileExplosion as ProjectileImpactExplosion;
                yield return null;
                if (projectileSimple)
                {
                    desiredForwardSpeed = CreateConfig(sectionName, "Desired Forward Speed", projectileSimple.desiredForwardSpeed, "", configEntryBases, true);
                    yield return null;
                    if (projectileImpactExplosion && projectileImpactExplosion.explodeOnLifeTimeExpiration)
                    {
                        lifetime = CreateConfig(sectionName, "Lifetime", projectileImpactExplosion.lifetime, "", configEntryBases, true);
                        yield return null;
                    }
                    else
                    {
                        lifetime = CreateConfig(sectionName, "Lifetime", projectileSimple.lifetime, "", configEntryBases, true);
                        yield return null;
                    }
                }
                else if (projectileCharacterController)
                {
                    desiredForwardSpeed = CreateConfig(sectionName, "Desired Forward Speed", projectileCharacterController.velocity, "", configEntryBases, true);
                    yield return null;
                    if (projectileImpactExplosion && projectileImpactExplosion.explodeOnLifeTimeExpiration)
                    {
                        lifetime = CreateConfig(sectionName, "Lifetime", projectileImpactExplosion.lifetime, "", configEntryBases, true);
                        yield return null;
                    }
                    else
                    {
                        lifetime = CreateConfig(sectionName, "Lifetime", projectileCharacterController.lifetime, "", configEntryBases, true);
                        yield return null;
                    }
                }
                if (projectileExplosion)
                {
                    falloffModel = CreateConfig(sectionName, "Falloff Model", projectileExplosion.falloffModel, "", configEntryBases, true);
                    yield return null;
                    blastRadius = CreateConfig(sectionName, "Blast Radius", projectileExplosion.blastRadius, "", configEntryBases, true);
                    yield return null;
                    blastDamageCoefficient = CreateConfig(sectionName, "Blast Damage Coefficient", projectileExplosion.blastDamageCoefficient, "The percentage of the damage, proc coefficient, and force of the initial projectile. Ranges from 0-1", configEntryBases, true);
                    yield return null;
                    blastProcCoefficient = CreateConfig(sectionName, "Blast Proc Coefficient", projectileExplosion.blastProcCoefficient, "", configEntryBases, true);
                    yield return null;
                    hasChildren = projectileExplosion.childrenProjectilePrefab;
                    if (hasChildren)
                    {
                        childrenCount = CreateConfig(sectionName, "Children Count", projectileExplosion.childrenCount, "", configEntryBases, true);
                        yield return null;
                        childrenDamageCoefficient = CreateConfig(sectionName, "Children Damage Coefficient", projectileExplosion.childrenDamageCoefficient, "What percentage of our damage does the children get?", configEntryBases, true);
                        yield return null;
                        childrenInheritDamageType = CreateConfig(sectionName, "Children Inherit Damage Type", projectileExplosion.childrenInheritDamageType, "Should children inherit the damage type of this projectile?", configEntryBases, true);
                        yield return null;
                    }
                    canApplyDot = projectileExplosion.applyDot;
                    if (canApplyDot)
                    {
                        applyDot = CreateConfig(sectionName, "Apply DoT", projectileExplosion.applyDot, "", configEntryBases, true);
                        yield return null;
                        dotDuration = CreateConfig(sectionName, "DoT Duration", projectileExplosion.dotDuration, "Duration in seconds of the DoT. Unused if calculateTotalDamage is true.", configEntryBases, true);
                        yield return null;
                        dotDamageMultiplier = CreateConfig(sectionName, "DoT Damage Multiplier", projectileExplosion.dotDamageMultiplier, "Multiplier on the per-tick damage", configEntryBases, true);
                        yield return null;
                        calculateTotalDamage = CreateConfig(sectionName, "Calculate Total Damage", projectileExplosion.calculateTotalDamage, "If true, we disregard the duration and instead specify the total damage.", configEntryBases, true);
                        yield return null;
                        totalDamageMultiplier = CreateConfig(sectionName, "Total Damage Multiplier", projectileExplosion.totalDamageMultiplier, "totalDamage = totalDamageMultiplier * attacker's damage", configEntryBases, true);
                        yield return null;
                    }
                }
                foreach (ConfigEntryBase configEntryBase in configEntryBases)
                {
                    ConfigEntry<float> configEntry = configEntryBase as ConfigEntry<float>;
                    if (configEntry == null)
                    {
                        ConfigEntry<int> configEntry2 = configEntryBase as ConfigEntry<int>;
                        if (configEntry2 == null)
                        {
                            ConfigEntry<bool> configEntry1 = configEntryBase as ConfigEntry<bool>;
                            if (configEntry1 == null) continue;
                            configEntry1.SettingChanged += ConfigEntry_SettingChanged;
                        }
                        else
                        {
                            configEntry2.SettingChanged += ConfigEntry_SettingChanged;
                        }
                    }
                    else
                    {
                        configEntry.SettingChanged += ConfigEntry_SettingChanged;
                    }
                }
                void UpdateProjectile()
                {
                    if (projectileSimple)
                    {
                        projectileSimple.desiredForwardSpeed = desiredForwardSpeed.Value;
                        if (projectileImpactExplosion && projectileImpactExplosion.explodeOnLifeTimeExpiration)
                        {
                            projectileImpactExplosion.lifetime = lifetime.Value;
                            projectileSimple.lifetime = Mathf.Infinity;
                        }
                        else
                        {
                            projectileSimple.lifetime = lifetime.Value;
                        }
                    }
                    if (projectileExplosion)
                    {
                        projectileExplosion.blastRadius = blastRadius.Value;
                        projectileExplosion.blastDamageCoefficient = blastDamageCoefficient.Value;
                        projectileExplosion.blastProcCoefficient = blastProcCoefficient.Value;
                        if (hasChildren)
                        {
                            projectileExplosion.childrenCount = childrenCount.Value;
                            projectileExplosion.childrenDamageCoefficient = childrenDamageCoefficient.Value;
                            projectileExplosion.childrenInheritDamageType = childrenInheritDamageType.Value;
                        }
                        if (canApplyDot)
                        {
                            projectileExplosion.applyDot = applyDot.Value;
                            projectileExplosion.dotDuration = dotDuration.Value;
                            projectileExplosion.dotDamageMultiplier = dotDamageMultiplier.Value;
                            projectileExplosion.calculateTotalDamage = calculateTotalDamage.Value;
                            projectileExplosion.totalDamageMultiplier = totalDamageMultiplier.Value;
                        }
                    }
                }
                void ConfigEntry_SettingChanged(object sender, EventArgs e) => UpdateProjectile();
                GetProjectileCustomConfigs?.Invoke(projectile);
                UpdateProjectile();
            }
        }
        private static ConfigEntry<T> CreateConfig<T>(string section, string key, T defaultValue, string description, List<ConfigEntryBase> configEntryBases, bool enableRiskOfOptions)
        {
            ConfigDefinition configDefinition = new ConfigDefinition(section, key);
            ConfigDescription configDescription = new ConfigDescription(description);
            ConfigEntry<T> entry = configFile.Bind(configDefinition, defaultValue, configDescription);
            configEntryBases?.Add(entry);
            if (enableRiskOfOptions && riskOfOptionsEnabled) ModCompatabilities.RiskOfOptionsCompatability.AddConfig(entry, defaultValue);
            return entry;
        }
        public static ConfigEntry<T> CreateConfig<T>(string section, string key, T defaultValue, string description, bool autoHandleRiskOfOptions)
        {
            ConfigDefinition configDefinition = new ConfigDefinition(section, key);
            ConfigDescription configDescription = new ConfigDescription(description);
            ConfigEntry<T> entry = configFile.Bind(configDefinition, defaultValue, configDescription);
            if (autoHandleRiskOfOptions && riskOfOptionsEnabled) ModCompatabilities.RiskOfOptionsCompatability.AddConfig(entry, defaultValue);
            return entry;
        }
        public static ConfigEntry<T> CreateConfig<T>(string section, string key, T defaultValue, string description) => CreateConfig(section, key, defaultValue, description, true);
    }
}
