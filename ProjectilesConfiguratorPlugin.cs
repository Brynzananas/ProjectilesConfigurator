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
        public const string ModVer = "1.1.0";
        public static bool riskOfOptionsEnabled { get; private set; }
        public static ConfigFile configFile { get; private set; }
        public static ManualLogSource Log { get; private set; }
        //public static Dictionary<GameObject, List<ConfigEntryBase>> projectilesConfigs = [];
        public static Dictionary<string, int> names = [];
        public static ConfigureProjectile GetProjectileCustomConfigs;
        public static ConfigureProjectileAsync GetProjectileCustomConfigsAsync;
        public delegate void ConfigureProjectile(GameObject projectile, string sectionName);
        public delegate void ConfigureProjectileAsync(ParallelCoroutine parallelCoroutine, GameObject projectile, string sectionName);
        private static ParallelCoroutine parallelCoroutine;
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
            parallelCoroutine = new ParallelCoroutine();
            stopwatch = Stopwatch.StartNew();
            stopwatch.Start();
            int i = 0;
            foreach (GameObject projectile in ProjectileCatalog.projectilePrefabs)
            {
                i++;
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
                if (sectionName.IsNullOrWhiteSpace()) continue;
                int namesCount = 0;
                while (names.ContainsKey(sectionName + (namesCount == 0 ? "" : namesCount)))
                {
                    namesCount++;
                }
                sectionName += (namesCount == 0 ? "" : namesCount);
                names.Add(sectionName, namesCount);
                ConfigEntry<bool> enable = CreateConfig(sectionName, "Enable Config", false, "Enable configuration for this projectile?", null, false);
                if (!enable.Value) continue;
                parallelCoroutine.Add(ConfigureProjectileThread(projectile, sectionName));
                GetProjectileCustomConfigsAsync?.Invoke(parallelCoroutine, projectile, sectionName);
            }
            IEnumerator runLoadCoroutine()
            {
                yield return parallelCoroutine;
                Log.LogMessage("Finished configuring projectiles. Time took: " + stopwatch.ElapsedMilliseconds + "ms");
                stopwatch.Stop();
            }
            RoR2Application.instance.StartCoroutine(runLoadCoroutine());
        }
        private static IEnumerator ConfigureProjectileThread(GameObject projectile, string sectionName)
        {
            List<ConfigEntryBase> configEntryBases = [];
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
            ProjectileOverlapAttack projectileOverlapAttack = projectile.GetComponent<ProjectileOverlapAttack>();
            ConfigEntry<float> overlapDamageCoefficient = null;
            ConfigEntry<float> overlapProcCoefficient = null;
            ConfigEntry<float> overlapPushAwayForce = null;
            ConfigEntry<int> overlapMaximumOverlapTargets = null;
            ConfigEntry<float> overlapFireFrequency = null;
            ConfigEntry<float> overlapResetInterval = null;
            ConfigEntry<bool> overlapIsSphereOverlap = null;
            ConfigEntry<bool> overlapScaleWithVelocity = null;
            ConfigEntry<bool> overlapIsOverrideTeam = null;
            ConfigEntry<TeamIndex> overlapOverrideTeam = null;
            ConfigEntry<bool> overlapCanHitOwner = null;
            //ProjectileOwnerOrbiter projectileOwnerOrbiter = projectile.GetComponent<ProjectileOwnerOrbiter>();
            ProjectileSteerTowardTarget projectileSteerTowardTarget = projectile.GetComponent<ProjectileSteerTowardTarget>();
            ConfigEntry<bool> steerYAxisOnly = null;
            ConfigEntry<float> steerRotationSpeed = null;
            ConfigEntry<bool> steerIncreaseSpeedOvertime = null;
            ConfigEntry<float> steerMaxRotationSpeed = null;
            ConfigEntry<float> steerRotationAddPerSecond = null;
            BoomerangProjectile boomerangProjectile = projectile.GetComponent<BoomerangProjectile>();
            ConfigEntry<float> boomerangTravelSpeed = null;
            ConfigEntry<float> boomerangCharge = null;
            ConfigEntry<float> boomerangTransitionDuration = null;
            ConfigEntry<bool> boomerangCanHitCharacters = null;
            ConfigEntry<bool> boomerangCanHitWorld = null;
            MissileController missileController = projectile.GetComponent<MissileController>();
            ConfigEntry<float> missileMaxVelocity = null;
            ConfigEntry<float> missileRollVelocity = null;
            ConfigEntry<float> missileAcceleration = null;
            ConfigEntry<float> missileDelayTimer = null;
            ConfigEntry<float> missileGiveupTimer = null;
            ConfigEntry<float> missileDeathTimer = null;
            ConfigEntry<float> missileTurbulence = null;
            ConfigEntry<float> missileMaxSeekDistance = null;
            yield return null;
            if (projectileSimple)
            {
                desiredForwardSpeed = CreateConfig(sectionName, "Desired Forward Speed", projectileSimple.desiredForwardSpeed, "", configEntryBases);
                yield return null;
                if (projectileImpactExplosion && projectileImpactExplosion.explodeOnLifeTimeExpiration)
                {
                    lifetime = CreateConfig(sectionName, "Lifetime", projectileImpactExplosion.lifetime, "", configEntryBases);
                    yield return null;
                }
                else
                {
                    lifetime = CreateConfig(sectionName, "Lifetime", projectileSimple.lifetime, "", configEntryBases);
                    yield return null;
                }
            }
            else if (projectileCharacterController)
            {
                desiredForwardSpeed = CreateConfig(sectionName, "Desired Forward Speed", projectileCharacterController.velocity, "", configEntryBases);
                yield return null;
                if (projectileImpactExplosion && projectileImpactExplosion.explodeOnLifeTimeExpiration)
                {
                    lifetime = CreateConfig(sectionName, "Lifetime", projectileImpactExplosion.lifetime, "", configEntryBases);
                    yield return null;
                }
                else
                {
                    lifetime = CreateConfig(sectionName, "Lifetime", projectileCharacterController.lifetime, "", configEntryBases);
                    yield return null;
                }
            }
            if (projectileExplosion)
            {
                falloffModel = CreateConfig(sectionName, "Explosion Falloff Model", projectileExplosion.falloffModel, "", configEntryBases);
                yield return null;
                blastRadius = CreateConfig(sectionName, "Explosion Blast Radius", projectileExplosion.blastRadius, "", configEntryBases);
                yield return null;
                blastDamageCoefficient = CreateConfig(sectionName, "Explosion Blast Damage Coefficient", projectileExplosion.blastDamageCoefficient, "The percentage of the damage, proc coefficient, and force of the initial projectile. Ranges from 0-1", configEntryBases);
                yield return null;
                blastProcCoefficient = CreateConfig(sectionName, "Explosion Blast Proc Coefficient", projectileExplosion.blastProcCoefficient, "", configEntryBases);
                yield return null;
                hasChildren = projectileExplosion.childrenProjectilePrefab;
                if (hasChildren)
                {
                    childrenCount = CreateConfig(sectionName, "Explosion Children Count", projectileExplosion.childrenCount, "", configEntryBases);
                    yield return null;
                    childrenDamageCoefficient = CreateConfig(sectionName, "Explosion Children Damage Coefficient", projectileExplosion.childrenDamageCoefficient, "What percentage of our damage does the children get?", configEntryBases);
                    yield return null;
                    childrenInheritDamageType = CreateConfig(sectionName, "Explosion Children Inherit Damage Type", projectileExplosion.childrenInheritDamageType, "Should children inherit the damage type of this projectile?", configEntryBases);
                    yield return null;
                }
                canApplyDot = projectileExplosion.applyDot;
                if (canApplyDot)
                {
                    applyDot = CreateConfig(sectionName, "Explosion Apply DoT", projectileExplosion.applyDot, "", configEntryBases);
                    yield return null;
                    dotDuration = CreateConfig(sectionName, "Explosion DoT Duration", projectileExplosion.dotDuration, "Duration in seconds of the DoT. Unused if calculateTotalDamage is true.", configEntryBases);
                    yield return null;
                    dotDamageMultiplier = CreateConfig(sectionName, "Explosion DoT Damage Multiplier", projectileExplosion.dotDamageMultiplier, "Multiplier on the per-tick damage", configEntryBases);
                    yield return null;
                    calculateTotalDamage = CreateConfig(sectionName, "Explosion Calculate Total Damage", projectileExplosion.calculateTotalDamage, "If true, we disregard the duration and instead specify the total damage.", configEntryBases);
                    yield return null;
                    totalDamageMultiplier = CreateConfig(sectionName, "Explosion Total Damage Multiplier", projectileExplosion.totalDamageMultiplier, "totalDamage = totalDamageMultiplier * attacker's damage", configEntryBases);
                    yield return null;
                }
            }
            if (projectileOverlapAttack)
            {
                overlapDamageCoefficient = CreateConfig(sectionName, "Overlap Attack Damage Coefficient", projectileOverlapAttack.damageCoefficient, "", configEntryBases);
                yield return null;
                overlapProcCoefficient = CreateConfig(sectionName, "Overlap Attack Proc Coefficient", projectileOverlapAttack.overlapProcCoefficient, "", configEntryBases);
                yield return null;
                overlapPushAwayForce = CreateConfig(sectionName, "Overlap Attack Push Away Force", projectileOverlapAttack.pushAwayForce, "", configEntryBases);
                yield return null;
                overlapMaximumOverlapTargets = CreateConfig(sectionName, "Overlap Attack Maximum Overlap Targets", projectileOverlapAttack.maximumOverlapTargets, "", configEntryBases);
                yield return null;
                overlapFireFrequency = CreateConfig(sectionName, "Overlap Attack Fire Frequency", projectileOverlapAttack.fireFrequency, "", configEntryBases);
                yield return null;
                overlapResetInterval = CreateConfig(sectionName, "Overlap Attack Reset Interval", projectileOverlapAttack.resetInterval, "If non-negative, the attack clears its hit memory at the specified interval.", configEntryBases);
                yield return null;
                overlapIsSphereOverlap = CreateConfig(sectionName, "Overlap Attack Is Sphere Overlap", projectileOverlapAttack.isSphereOverlap, "Treat the hurtbox's scale as a radius to define a sphere. Assumes a uniformly scaled hurtbox", configEntryBases);
                yield return null;
                overlapScaleWithVelocity = CreateConfig(sectionName, "Overlap Attack Scale With Velocity", projectileOverlapAttack.ScaleWithVelocity, "", configEntryBases);
                yield return null;
                overlapIsOverrideTeam = CreateConfig(sectionName, "Overlap Attack Is Override Team", projectileOverlapAttack.isOverrideTeam, "", configEntryBases);
                yield return null;
                overlapOverrideTeam = CreateConfig(sectionName, "Overlap Attack Override Team Index", projectileOverlapAttack.OverrideTeamIndex, "", configEntryBases);
                yield return null;
                overlapCanHitOwner = CreateConfig(sectionName, "Overlap Attack Can Hit Owner", projectileOverlapAttack.canHitOwner, "If artifact of chaos is active, then this overlap attack can hurt the owner", configEntryBases);
                yield return null;
            }
            //if (projectileOwnerOrbiter)
            //{

            //}
            if (projectileSteerTowardTarget)
            {
                steerYAxisOnly = CreateConfig(sectionName, "Steer Toward Target Y Axis Only", projectileSteerTowardTarget.yAxisOnly, "", configEntryBases);
                yield return null;
                steerRotationSpeed = CreateConfig(sectionName, "Steer Toward Target Rotation Speed", projectileSteerTowardTarget.rotationSpeed, "", configEntryBases);
                yield return null;
                steerIncreaseSpeedOvertime = CreateConfig(sectionName, "Steer Toward Target Increase Speed Overtime", projectileSteerTowardTarget.increaseSpeedOverTime, "", configEntryBases);
                yield return null;
                steerMaxRotationSpeed = CreateConfig(sectionName, "Steer Toward Target Max Rotation Speed", projectileSteerTowardTarget.maxRotationSpeed, "", configEntryBases);
                yield return null;
                steerRotationAddPerSecond = CreateConfig(sectionName, "Steer Toward Target Rotation Add Per Second", projectileSteerTowardTarget.rotationAddPerSecond, "", configEntryBases);
                yield return null;
            }
            if (boomerangProjectile)
            {
                boomerangTravelSpeed = CreateConfig(sectionName, "Boomerang Travel Speed", boomerangProjectile.travelSpeed, "", configEntryBases);
                yield return null;
                boomerangCharge = CreateConfig(sectionName, "Boomerang Charge", boomerangProjectile.charge, "", configEntryBases);
                yield return null;
                boomerangTransitionDuration = CreateConfig(sectionName, "Boomerang Transition Duration", boomerangProjectile.transitionDuration, "", configEntryBases);
                yield return null;
                boomerangCanHitCharacters = CreateConfig(sectionName, "Boomerang Travel Can Hit Characters", boomerangProjectile.canHitCharacters, "", configEntryBases);
                yield return null;
                boomerangCanHitWorld = CreateConfig(sectionName, "Boomerang Travel Can Hit World", boomerangProjectile.canHitWorld, "", configEntryBases);
                yield return null;
            }
            if (missileController)
            {
                missileMaxVelocity = CreateConfig(sectionName, "Missile Max Velocity", missileController.maxVelocity, "", configEntryBases);
                yield return null;
                missileRollVelocity = CreateConfig(sectionName, "Missile Roll Velocity", missileController.rollVelocity, "", configEntryBases);
                yield return null;
                missileAcceleration = CreateConfig(sectionName, "Missile Acceleration", missileController.acceleration, "", configEntryBases);
                yield return null;
                missileDelayTimer = CreateConfig(sectionName, "Missile Delay Timer", missileController.delayTimer, "", configEntryBases);
                yield return null;
                missileGiveupTimer = CreateConfig(sectionName, "Missile Giveup Timer", missileController.giveupTimer, "", configEntryBases);
                yield return null;
                missileDeathTimer = CreateConfig(sectionName, "Missile Death Timer", missileController.deathTimer, "", configEntryBases);
                yield return null;
                missileTurbulence = CreateConfig(sectionName, "Missile Turbulence", missileController.turbulence, "", configEntryBases);
                yield return null;
                missileMaxSeekDistance = CreateConfig(sectionName, "Missile Max Seek Distance", missileController.maxSeekDistance, "", configEntryBases);
                yield return null;
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
                if (projectileOverlapAttack)
                {
                    projectileOverlapAttack.damageCoefficient = overlapDamageCoefficient.Value;
                    projectileOverlapAttack.overlapProcCoefficient = overlapProcCoefficient.Value;
                    projectileOverlapAttack.pushAwayForce = overlapPushAwayForce.Value;
                    projectileOverlapAttack.maximumOverlapTargets = overlapMaximumOverlapTargets.Value;
                    projectileOverlapAttack.fireFrequency = overlapFireFrequency.Value;
                    projectileOverlapAttack.resetInterval = overlapResetInterval.Value;
                    projectileOverlapAttack.isSphereOverlap = overlapIsSphereOverlap.Value;
                    projectileOverlapAttack.ScaleWithVelocity = overlapScaleWithVelocity.Value;
                    projectileOverlapAttack.isOverrideTeam = overlapIsOverrideTeam.Value;
                    projectileOverlapAttack.OverrideTeamIndex = overlapOverrideTeam.Value;
                    projectileOverlapAttack.canHitOwner = overlapCanHitOwner.Value;
                }
                if (projectileSteerTowardTarget)
                {
                    projectileSteerTowardTarget.yAxisOnly = steerYAxisOnly.Value;
                    projectileSteerTowardTarget.rotationSpeed = steerRotationSpeed.Value;
                    projectileSteerTowardTarget.increaseSpeedOverTime = steerIncreaseSpeedOvertime.Value;
                    projectileSteerTowardTarget.maxRotationSpeed = steerMaxRotationSpeed.Value;
                    projectileSteerTowardTarget.rotationAddPerSecond = steerRotationAddPerSecond.Value;
                }
                if (boomerangProjectile)
                {
                    boomerangProjectile.travelSpeed = boomerangTravelSpeed.Value;
                    boomerangProjectile.charge = boomerangCharge.Value;
                    boomerangProjectile.transitionDuration = boomerangTransitionDuration.Value;
                    boomerangProjectile.canHitCharacters = boomerangCanHitCharacters.Value;
                    boomerangProjectile.canHitWorld = boomerangCanHitWorld.Value;
                }
                if (missileController)
                {
                    missileController.maxVelocity = missileMaxVelocity.Value;
                    missileController.rollVelocity = missileRollVelocity.Value;
                    missileController.acceleration = missileAcceleration.Value;
                    missileController.delayTimer = missileDelayTimer.Value;
                    missileController.giveupTimer = missileGiveupTimer.Value;
                    missileController.deathTimer = missileDeathTimer.Value;
                    missileController.turbulence = missileTurbulence.Value;
                    missileController.maxSeekDistance = missileMaxSeekDistance.Value;
                }
            }
            void ConfigEntry_SettingChanged(object sender, EventArgs e) => UpdateProjectile();
            UpdateProjectile();
            GetProjectileCustomConfigs?.Invoke(projectile, sectionName);
        }
        private static ConfigEntry<T> CreateConfig<T>(string section, string key, T defaultValue, string description, List<ConfigEntryBase> configEntryBases) => CreateConfig(section, key, defaultValue, description, configEntryBases, true);
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
