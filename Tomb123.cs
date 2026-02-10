using ConnectorLib;
using ConnectorLib.Inject;
using ConnectorLib.Memory;
using CrowdControl.Common;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static CrowdControl.Games.EffectPack;
using AddressChain = ConnectorLib.Memory.AddressChain<ConnectorLib.Inject.InjectConnector>;
using ConnectorType = CrowdControl.Common.ConnectorType;
using Log = CrowdControl.Common.Log;

namespace CrowdControl.Games.Packs.Tomb123
{

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class Tomb123 : InjectEffectPack
    {
        private readonly string _mainGame = "tomb123.exe";
        private readonly string _tomb1Dll = "tomb1.dll";
        private ulong _tomb1DllBase;
        private readonly string _tomb2Dll = "tomb2.dll";
        private ulong _tomb2DllBase;
        private readonly string _tomb3Dll = "tomb3.dll";
        private ulong _tomb3DllBase;
        private bool? _modifyingGraphics = false;
        private bool _modifyingControls = false;
        private bool _forcingMovement = false;
        private bool? _wasOGGraphics = null;
        private bool? _wasTankControls = null;
        private bool _affectingPistolDamage = false;
        private bool _affectingMagnumDamage = false;
        private bool _affectingShotgunDamage = false;
        private bool _affectingUziDamage = false;
        private bool _affectingFallDamage = false;
        private bool _affectingAutoPistolDamage = false;
        private bool _affectingM16Damage = false;
        private bool _affectingGrenadeDamage = false;
        private bool _affectingHarpoonDamage = false;
        private bool _affectingMP5Damage = false;
        private bool _affectingDesertEagleDamage = false;
        private bool _affectingRocketDamage = false;
        private bool _affectingStamina = false;
        private bool _forcingUnequip = false;
        private readonly TimeSpan _gameStatusPollInterval = TimeSpan.FromSeconds(5);
        private System.Threading.Timer? _gameStatusTimer;
        private CurrentGame? _lastReportedGame;
        private readonly object _statusLock = new();
        private static readonly Dictionary<GameEffect, HashSet<CurrentGame>> _effectGameRestrictions = new()
        {
            { GameEffect.tr1GiveMagnums, new HashSet<CurrentGame> { CurrentGame.TR1 } },
            { GameEffect.tr1TakeMagnums, new HashSet<CurrentGame> { CurrentGame.TR1 } },
            { GameEffect.tr1GiveMagnumAmmo, new HashSet<CurrentGame> { CurrentGame.TR1 } },
            { GameEffect.tr1TakeMagnumAmmo, new HashSet<CurrentGame> { CurrentGame.TR1 } },
            { GameEffect.tr1DoubleMagnumDamage, new HashSet<CurrentGame> { CurrentGame.TR1 } },
            { GameEffect.tr1DisableMagnumDamage, new HashSet<CurrentGame> { CurrentGame.TR1 } },
            { GameEffect.giveAutoPistols, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.takeAutoPistols, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.giveM16, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.takeM16, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.giveAutoPistolAmmo, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.takeAutoPistolAmmo, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.giveM16Ammo, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.takeM16Ammo, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.doubleM16Damage, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.disableM16Damage, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.doubleAutoPistolsDamage, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.disableAutoPistolsDamage, new HashSet<CurrentGame> { CurrentGame.TR2 } },
            { GameEffect.giveDesertEagle, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.takeDesertEagle, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.giveMP5, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.takeMP5, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.giveRocketLauncher, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.takeRocketLauncher, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.giveMP5Ammo, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.takeMP5Ammo, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.giveDeagleAmmo, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.takeDeagleAmmo, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.giveRocketAmmo, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.takeRocketAmmo, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.doubleMP5Damage, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.disableMP5Damage, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.doubleDeagleDamage, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.disableDeagleDamage, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.disableStamina, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.halfStamina, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.infiniteStamina, new HashSet<CurrentGame> { CurrentGame.TR3 } },
            { GameEffect.giveFlare, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.takeFlare, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.giveGrenadeLauncher, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.takeGrenadeLauncher, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.giveHarpoonGun, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.takeHarpoonGun, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.giveHarpoonAmmo, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.takeHarpoonAmmo, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.giveGrenadeAmmo, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.takeGrenadeAmmo, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.doubleHarpoonDamage, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.disableHarpoonDamage, new HashSet<CurrentGame> { CurrentGame.TR2, CurrentGame.TR3 } },
            { GameEffect.darkLara, new HashSet<CurrentGame> { CurrentGame.TR1, CurrentGame.TR2 } },
        };
        private readonly Dictionary<GameEffect, Effect> _effectById = new();
        private readonly Dictionary<GameEffect, string> _effectIdByGameEffect = new();
        private static readonly HashSet<string> _gameEffectIds = new(Enum.GetNames(typeof(GameEffect)));
        private static readonly Dictionary<string, GameEffect> _effectNameToId = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Dark Lara", GameEffect.darkLara },
            { "Give Magnums", GameEffect.tr1GiveMagnums },
            { "Take Magnums", GameEffect.tr1TakeMagnums },
            { "Give Magnum Ammo", GameEffect.tr1GiveMagnumAmmo },
            { "Take Magnum Ammo", GameEffect.tr1TakeMagnumAmmo },
            { "Double Magnum Damage", GameEffect.tr1DoubleMagnumDamage },
            { "Disable Magnum Damage", GameEffect.tr1DisableMagnumDamage },
            { "Give Automatic Pistols", GameEffect.giveAutoPistols },
            { "Take Automatic Pistols", GameEffect.takeAutoPistols },
            { "Give M16", GameEffect.giveM16 },
            { "Take M16", GameEffect.takeM16 },
            { "Give Automatic Pistol Ammo", GameEffect.giveAutoPistolAmmo },
            { "Take Automatic Pistol Ammo", GameEffect.takeAutoPistolAmmo },
            { "Give M16 Ammo", GameEffect.giveM16Ammo },
            { "Take M16 Ammo", GameEffect.takeM16Ammo },
            { "Double M16 Damage", GameEffect.doubleM16Damage },
            { "Disable M16 Damage", GameEffect.disableM16Damage },
            { "Double Automatic Pistols Damage", GameEffect.doubleAutoPistolsDamage },
            { "Disable Automatic Pistols Damage", GameEffect.disableAutoPistolsDamage },
            { "Give Desert Eagle", GameEffect.giveDesertEagle },
            { "Take Desert Eagle", GameEffect.takeDesertEagle },
            { "Give MP5", GameEffect.giveMP5 },
            { "Take MP5", GameEffect.takeMP5 },
            { "Give Rocket Launcher", GameEffect.giveRocketLauncher },
            { "Take Rocket Launcher", GameEffect.takeRocketLauncher },
            { "Give MP5 Ammo", GameEffect.giveMP5Ammo },
            { "Take MP5 Ammo", GameEffect.takeMP5Ammo },
            { "Give Desert Eagle Ammo", GameEffect.giveDeagleAmmo },
            { "Take Desert Eagle Ammo", GameEffect.takeDeagleAmmo },
            { "Give Rockets", GameEffect.giveRocketAmmo },
            { "Take Rockets", GameEffect.takeRocketAmmo },
            { "Double MP5 Damage", GameEffect.doubleMP5Damage },
            { "Disable MP5 Damage", GameEffect.disableMP5Damage },
            { "Double Desert Eagle Damage", GameEffect.doubleDeagleDamage },
            { "Disable Desert Eagle Damage", GameEffect.disableDeagleDamage },
            { "Disable Stamina", GameEffect.disableStamina },
            { "Half Stamina", GameEffect.halfStamina },
            { "Infinite Stamina", GameEffect.infiniteStamina },
            { "Give Flare", GameEffect.giveFlare },
            { "Take Flare", GameEffect.takeFlare },
            { "Give Grenade Launcher", GameEffect.giveGrenadeLauncher },
            { "Take Grenade Launcher", GameEffect.takeGrenadeLauncher },
            { "Give Harpoon Gun", GameEffect.giveHarpoonGun },
            { "Take Harpoon Gun", GameEffect.takeHarpoonGun },
            { "Give Harpoons", GameEffect.giveHarpoonAmmo },
            { "Take Harpoons", GameEffect.takeHarpoonAmmo },
            { "Give Grenades", GameEffect.giveGrenadeAmmo },
            { "Take Grenades", GameEffect.takeGrenadeAmmo },
            { "Double Harpoon Damage", GameEffect.doubleHarpoonDamage },
            { "Disable Harpoon Damage", GameEffect.disableHarpoonDamage },
        };

        #region Global Values
        private const uint _currentGameOffset = 0x2F35F0;
        private const uint _tr123GraphicsAndControlOffset = 0x2641C4;
        private const uint _tr123OutfitOffset = 0x263CD8; //1 -> 14 (Classic 1, Training 1, Classic 2, Training 2, Wetsuit, Bomber, Bathrobe, Training 3, Nevada, Pacific, Catsuit, Antarctica, Bloody Classic, Vegas
        private const uint _tr123SunniesOffset = 0x263CE4;
        private const uint _tr123InMenuBoolean = 0x264040; //0 is good, 1 is in menu
        private const uint _tr123GameStateSppt = 0x2073C43; //67 is good, anything else is not
        private const uint _tr123MovementByte = 0x2639D4;
        private const uint _laraHealthOffset = 0x26;
        private const uint _laraVisibilityOffset = 0xC; //(works perfect for 1, but for 2 and 3 it does not hide her hair)
        private const uint _laraLightingOffset = 0x2F; //(works perfect for 1 and 2, but does not work for 3)
        private const uint _laraYPosOffset = 0x5C; //same for all 3 games
        private readonly byte[] maxHPCodeBytes = [0xB8, 0xE8, 0x03, 00, 00]; //seems same for all 3 pogbones
        private const uint _roomSize = 0xA8;
        private const uint _floodStateOffset = 0x66;
        private uint _maxO2 = 1800;
        private uint _maxHP = 1000;
        private bool _laraIsPoisoned = false;
        private uint _hpBeforePoison = uint.MaxValue;
        #endregion

        #region TR1 Values
        private const uint _tr1BackpackSizeOffset = 0xE2ABC;
        private const uint _tr1BackpackSlotCountsOffset = 0xF8DD8;
        private readonly List<uint> _tr1BackpackSlotOffsets =
        [
            0xF8D20,0xF8D28,0xF8D30,0xF8D38,0xF8D40,0xF8D48,0xF8D50
        ];
        private readonly List<BackpackItem> _tr1BackpackItemsWithAmmo =
        [
            BackpackItem.TR1_Shotgun, BackpackItem.TR1_Uzis, BackpackItem.TR1_Magnums
        ];
        private readonly List<BackpackItem> _tr1BackpackEntityOrder =
        [
            BackpackItem.TR1_Compass, BackpackItem.TR1_Pistols, BackpackItem.TR1_Shotgun, BackpackItem.TR1_Magnums,
            BackpackItem.TR1_Uzis, BackpackItem.TR1_LargeMedi, BackpackItem.TR1_SmallMedi
        ];
        private const uint _tr1MagnumAmmoOffset = 0x310FC8;
        private const uint _tr1UziAmmoOffset = 0x310FD0;
        private const uint _tr1ShotgunAmmoOffset = 0x310FD8;
        private const uint _tr1LaraOffset = 0x311030;
        private const uint _tr1OxygenOffset = 0x310E96;
        private const uint _tr1EquippedWeapon = 0x310E86;
        private const uint _tr1LaraStateByte = 0x310E8C;
        private const uint _tr1RoomCountOffset = 0x3F2030;
        private const uint _tr1RoomPtrOffset = 0x3F2168;
        private Dictionary<int, byte> _floodedRoomStateOriginal = [];
        private List<OffsetAddressChain<InjectConnector>> tr1MaxHPDeclarations = [];
        private const uint _tr1MaxO2Offset = 0x27B94;
        private const uint _tr1PistolDamageOffset = 0xF9700;
        private const uint _tr1MagnumDamageOffset = 0xF9730;
        private const uint _tr1UziDamageOffset = 0xF9760;
        private const uint _tr1ShotgunDamageOffset = 0xF9790;
        private const uint _tr1FallDamageOffset = 0x24BCD;
        private const uint _tr1LevelCompletedFlagOffset = 0xFD750;
        private const uint _tr1CurrentLevelOffset = 0xE2AB8;
        #endregion

        #region TR2 Values
        private const uint _tr2BackpackSizeOffset = 0x113EDC;
        private const uint _tr2BackpackSlotCountsOffset = 0x12E698;
        private readonly List<uint> _tr2BackpackSlotOffsets =
        [
            0x12E5E0,0x12E5E8,0x12E5F0,0x12E5F8,0x12E600,0x12E608,
            0x12E610,0x12E618,0x12E620,0x12E628,0x12E630
        ];
        private readonly List<BackpackItem> _tr2BackpackItemsWithAmmo =
        [
            BackpackItem.TR2_Shotgun, BackpackItem.TR2_Uzis, BackpackItem.TR2_AutomaticPistols,
            BackpackItem.TR2_GrenadeLauncher, BackpackItem.TR2_HarpoonGun, BackpackItem.TR2_M16,
        ];
        private readonly List<BackpackItem> _tr2BackpackEntityOrder =
        [
            BackpackItem.TR2_Compass, BackpackItem.TR2_Pistols, BackpackItem.TR2_Shotgun, BackpackItem.TR2_AutomaticPistols,
            BackpackItem.TR2_Uzis, BackpackItem.TR2_M16, BackpackItem.TR2_GrenadeLauncher, BackpackItem.TR2_HarpoonGun,
            BackpackItem.TR2_Flares, BackpackItem.TR2_LargeMedi, BackpackItem.TR2_SmallMedi
        ];
        private const uint _tr2AutomaticPistolsAmmoOffset = 0x346108;
        private const uint _tr2UziAmmoOffset = 0x346110;
        private const uint _tr2ShotgunAmmoOffset = 0x346118;
        private const uint _tr2HarpoonGunAmmoOffset = 0x346120;
        private const uint _tr2GrenadeLauncherAmmoOffset = 0x346128;
        private const uint _tr2M16AmmoOffset = 0x346138;
        private const uint _tr2EquippedWeapon = 0x345FC6; //345FC8 controls what you take out when you unholster?
        private const uint _tr2LaraStateByte = 0x345FCC;
        private const uint _tr2RoomCountOffset = 0x3FD1B0;
        private const uint _tr2RoomPtrOffset = 0x427360;
        private const uint _tr2LaraOffset = 0x346170;
        private const uint _tr2OxygenOffset = 0x345FD6;
        private const uint _tr2PistolDamageOffset = 0x12EA30;
        private const uint _tr2AutomaticPistolsDamageOffset = 0x12EA60;
        private const uint _tr2UziDamageOffset = 0x12EA90;
        private const uint _tr2ShotgunDamageOffset = 0x12EAC0;
        private const uint _tr2HarpoonGunDamageOffset = 0x12EB50;
        private const uint _tr2GrenadeLauncherDamageOffset = 0x12EB20; //doesnt work
        private const uint _tr2M16DamageOffset = 0x12EAF0;
        private const uint _tr2FallDamageOffset = 0x43D99;
        private const uint _tr2LevelCompletedFlagOffset = 0x1330B8;
        private const uint _tr2CurrentLevelOffset = 0x132B58;
        private List<OffsetAddressChain<InjectConnector>> tr2MaxHPDeclarations = [];
        private const uint _tr2InitialMaxO2Offset = 0x5167C;
        private const uint _tr2RegainedMaxO2Offset = 0x5146B;
        #endregion

        #region TR3 Values
        private const uint _tr3BackpackSizeOffset = 0x1693E8;
        private const uint _tr3BackpackSlotCountsOffset = 0x1899D8;
        private readonly List<uint> _tr3BackpackSlotOffsets =
        [
            0x189920,0x189928,0x189930,0x189938,0x189940,0x189948,
            0x189950,0x189958,0x189960,0x189968,0x189970,0x189978
        ];
        private readonly List<BackpackItem> _tr3BackpackItemsWithAmmo =
        [
            BackpackItem.TR3_Shotgun, BackpackItem.TR3_Uzis, BackpackItem.TR3_DesertEagle,
            BackpackItem.TR3_GrenadeLauncher, BackpackItem.TR2_HarpoonGun, BackpackItem.TR3_MP5,
            BackpackItem.TR3_RocketLauncher
        ];
        private readonly List<BackpackItem> _tr3BackpackEntityOrder =
        [
            BackpackItem.TR3_Compass, BackpackItem.TR3_Pistols, BackpackItem.TR3_Shotgun, BackpackItem.TR3_DesertEagle,
            BackpackItem.TR3_Uzis, BackpackItem.TR3_MP5, BackpackItem.TR3_RocketLauncher, BackpackItem.TR3_GrenadeLauncher,
            BackpackItem.TR3_HarpoonGun, BackpackItem.TR3_Flares, BackpackItem.TR3_LargeMedi, BackpackItem.TR3_SmallMedi
        ];
        private const uint _tr3DeagleAmmoOffset = 0x3A2008;
        private const uint _tr3UziAmmoOffset = 0x3A2010;
        private const uint _tr3ShotgunAmmoOffset = 0x3A2018;
        private const uint _tr3HarpoonGunAmmoOffset = 0x3A2020;
        private const uint _tr3RocketLauncherAmmoOffset = 0x3A2028;
        private const uint _tr3GrenadeLauncherAmmoOffset = 0x3A2030;
        private const uint _tr3MP5AmmoOffset = 0x3A2038;
        private const uint _tr3EquippedWeapon = 0x3A1EC4;
        private const uint _tr3LaraStateByte = 0x3A1ECC;
        private const uint _tr3RoomCountOffset = 0x460290;
        private const uint _tr3RoomPtrOffset = 0x461140;
        private const uint _tr3LaraOffset = 0x3A2070;
        private const uint _tr3OxygenOffset = 0x3A1ED6;
        private const uint _tr3PistolDamageOffset = 0x189DA6;
        private const uint _tr3DeagleDamageOffset = 0x189DCC;
        private const uint _tr3UziDamageOffset = 0x189DF2;
        private const uint _tr3ShotgunDamageOffset = 0x189E18;
        private const uint _tr3MP5DamageOffset = 0x189E3E;
        private const uint _tr3RocketLauncherDamageOffset = 0x189E64; //doesnt work
        private const uint _tr3GrenadeLauncherDamageOffset = 0x189E8A; //doesnt work
        private const uint _tr3HarpoonGunDamageOffset = 0x189EB0;
        private const uint _tr3FallDamageOffset = 0x5FDC7;
        private const uint _tr3LevelCompletedFlagOffset = 0x18E690;
        private const uint _tr3CurrentLevelOffset = 0x18E16C;
        private List<OffsetAddressChain<InjectConnector>> tr3MaxHPDeclarations = [];
        private const uint _tr3InitialMaxO2Offset = 0x7457A;
        private const uint _tr3RegainedMaxO2Offset = 0x747B4;
        private const uint _tr3MaxStaminaOffset = 0x741DB;
        private const uint _tr3CurrentStaminaOffset = 0x3A1EF4;
        private const uint _tr3TemperatureOffset = 0x3A1EF6;
        private const uint _tr3MaxTemperatureOffset = 0x74A76;
        private const byte _defaultMaxStamina = 0x78;
        #endregion


        public Tomb123(UserRecord player, Func<CrowdControlBlock, bool> responseHandler, Action<object> statusUpdateHandler) : base(player, responseHandler, statusUpdateHandler)
        {
            VersionProfiles = [new("Tomb123", InitGame, DeinitGame)];
        }

        private void InitGame()
        {
            Connector.PointerFormat = PointerFormat.Absolute64LE;

            AddressChain<InjectConnector> gameExe = AddressChain.Parse(Connector, _mainGame);
            Log.Debug($"Game base: {gameExe.Address}");
            _tomb1DllBase = AddressChain.ModuleBase(Connector, _tomb1Dll).Address;
            Log.Debug($"TR1 base: {_tomb1DllBase}");
            _tomb2DllBase = AddressChain.ModuleBase(Connector, _tomb2Dll).Address;
            Log.Debug($"TR2 base: {_tomb2DllBase}");
            _tomb3DllBase = AddressChain.ModuleBase(Connector, _tomb3Dll).Address;
            Log.Debug($"TR3 base: {_tomb3DllBase}");
            InitializeEffectStatusTracking();
        }

        private void DeinitGame()
        {
            StopEffectStatusTracking();
        }

        public override Game Game { get; } = new("Tomb Raider I-III Remastered", "Tomb123", "PC", ConnectorType.InjectConnector);

        public enum GameEffect
        {
            classic1Outfit,
            training1Outfit,
            classic2Outfit,
            training2Outfit,
            wetsuitOutfit,
            bomberOutfit,
            bathrobeOutfit,
            training3Outfit,
            nevadaOutfit,
            pacificOutfit,
            catsuitOutfit,
            antarcticaOutfit,
            bloodyClassicOutfit,
            vegasOutfit,
            ogLaraOutfit,
            forceClassicGraphics,
            forceRemasterGraphics,
            forceTankControls,
            forceModernControls,
            forceMoveForward,
            forceMoveBackward,
            forceJump,
            forceWalk,
            forceSwanDive,
            putOnSunglasses,
            takeOffSunglasses,
            restartLevel,
            giveShotgun,
            takeShotgun,
            giveUzis,
            takeUzis,
            tr1GiveMagnums,
            tr1TakeMagnums,
            giveAutoPistols,
            takeAutoPistols,
            giveHarpoonGun,
            takeHarpoonGun,
            giveGrenadeLauncher,
            takeGrenadeLauncher,
            giveM16,
            takeM16,
            giveDesertEagle,
            takeDesertEagle,
            giveMP5,
            takeMP5,
            giveRocketLauncher,
            takeRocketLauncher,
            giveSmallMedi,
            takeSmallMedi,
            giveLargeMedi,
            takeLargeMedi,
            giveFlare,
            takeFlare,
            giveShotgunAmmo,
            takeShotgunAmmo,
            giveUziAmmo,
            takeUziAmmo,
            tr1GiveMagnumAmmo,
            tr1TakeMagnumAmmo,
            giveAutoPistolAmmo,
            takeAutoPistolAmmo,
            giveHarpoonAmmo,
            takeHarpoonAmmo,
            giveGrenadeAmmo,
            takeGrenadeAmmo,
            giveM16Ammo,
            takeM16Ammo,
            giveDeagleAmmo,
            takeDeagleAmmo,
            giveMP5Ammo,
            takeMP5Ammo,
            giveRocketAmmo,
            takeRocketAmmo,
            healLara,
            hurtLara,
            giveO2,
            takeO2,
            forceUnequip,
            invisibleLara,
            darkLara,
            poisonLara,
            floodLevel,
            superJump,
            increaseMaxHP,
            decreaseMaxHP,
            increaseMaxO2,
            decreaseMaxO2,
            doublePistolDamage,
            disablePistolDamage,
            tr1DoubleMagnumDamage,
            tr1DisableMagnumDamage,
            doubleShotgunDamage,
            disableShotgunDamage,
            doubleUziDamage,
            disableUziDamage,
            doubleHarpoonDamage,
            disableHarpoonDamage,
            doubleM16Damage,
            disableM16Damage,
            doubleAutoPistolsDamage,
            disableAutoPistolsDamage,
            doubleDeagleDamage,
            disableDeagleDamage,
            doubleMP5Damage,
            disableMP5Damage,
            halfFallDamage,
            doubleFallDamage,
            disableStamina,
            halfStamina,
            infiniteStamina
        }

        private const string CosmeticEffects = "Cosmetics";
        private const string MovementEffects = "Movement";
        private const string BackpackEffects = "Backpack";
        private const string MeterEffects = "Meters";
        private const string AmmoEffects = "Ammo";
        private const string OtherEffects = "Other";
        private const string CategoryTR1 = "TR1";
        private const string CategoryTR2 = "TR2";
        private const string CategoryTR3 = "TR3";
        private static readonly string[] AllGameCategories = [CategoryTR1, CategoryTR2, CategoryTR3];

        private static string[] BuildCategories(GameEffect effect, string baseCategory)
        {
            if (_effectGameRestrictions.TryGetValue(effect, out HashSet<CurrentGame>? games))
                return [baseCategory, ..games.OrderBy(game => game).Select(game => game.ToString())];

            return [baseCategory, ..AllGameCategories];
        }

        public override EffectList Effects { get; } = new List<Effect>
        {

            #region Cosmetics
            new("Force Classic Graphics", GameEffect.forceClassicGraphics.ToString()) { Price = 10, Description = "Force classic graphics", Category = BuildCategories(GameEffect.forceClassicGraphics, CosmeticEffects), Duration = 60, IsDurationEditable = true},
            new("Force Remaster Graphics", GameEffect.forceRemasterGraphics.ToString()) { Price = 10, Description = "Force remaster graphics", Category = BuildCategories(GameEffect.forceRemasterGraphics, CosmeticEffects), Duration = 60, IsDurationEditable = true},
            new("TR1 Classic Outfit", GameEffect.classic1Outfit.ToString()) { Price = 10, Description = "Change Lara into TR1's classic outfit", Category = BuildCategories(GameEffect.classic1Outfit, CosmeticEffects)},
            new("TR1 Training Outfit", GameEffect.training1Outfit.ToString()) { Price = 10, Description = "Change Lara into TR1's training outfit", Category = BuildCategories(GameEffect.training1Outfit, CosmeticEffects)},
            new("TR2 Classic Outfit", GameEffect.classic2Outfit.ToString()) { Price = 10, Description = "Change Lara into TR2's classic outfit", Category = BuildCategories(GameEffect.classic2Outfit, CosmeticEffects)},
            new("TR2 Training Outfit", GameEffect.training2Outfit.ToString()) { Price = 10, Description = "Change Lara into TR2's training outfit", Category = BuildCategories(GameEffect.training2Outfit, CosmeticEffects)},
            new("Wetsuit Outfit", GameEffect.wetsuitOutfit.ToString()) { Price = 10, Description = "Change Lara into the wetsuit outfit", Category = BuildCategories(GameEffect.wetsuitOutfit, CosmeticEffects)},
            new("Bomber Outfit", GameEffect.bomberOutfit.ToString()) { Price = 10, Description = "Change Lara into the bomber outfit", Category = BuildCategories(GameEffect.bomberOutfit, CosmeticEffects)},
            new("Bathrobe Outfit", GameEffect.bathrobeOutfit.ToString()) { Price = 10, Description = "Change Lara into the bathrobe outfit", Category = BuildCategories(GameEffect.bathrobeOutfit, CosmeticEffects)},
            new("TR3 Training Outfit", GameEffect.training3Outfit.ToString()) { Price = 10, Description = "Change Lara into TR3's training outfit", Category = BuildCategories(GameEffect.training3Outfit, CosmeticEffects)},
            new("Nevada Outfit", GameEffect.nevadaOutfit.ToString()) { Price = 10, Description = "Change Lara into the Nevada outfit", Category = BuildCategories(GameEffect.nevadaOutfit, CosmeticEffects)},
            new("Pacific Outfit", GameEffect.pacificOutfit.ToString()) { Price = 10, Description = "Change Lara into the Pacific outfit", Category = BuildCategories(GameEffect.pacificOutfit, CosmeticEffects)},
            new("Catsuit Outfit", GameEffect.catsuitOutfit.ToString()) { Price = 10, Description = "Change Lara into the catsuit outfit", Category = BuildCategories(GameEffect.catsuitOutfit, CosmeticEffects)},
            new("Antarctica Outfit", GameEffect.antarcticaOutfit.ToString()) { Price = 10, Description = "Change Lara into the Antarctica outfit", Category = BuildCategories(GameEffect.antarcticaOutfit, CosmeticEffects)},
            new("Bloody Classic Outfit", GameEffect.bloodyClassicOutfit.ToString()) { Price = 10, Description = "Change Lara into the bloody classic outfit", Category = BuildCategories(GameEffect.bloodyClassicOutfit, CosmeticEffects)},
            new("OG Outfit", GameEffect.ogLaraOutfit.ToString()) { Price = 10, Description = "Change Lara into OG Lara in remastered graphics", Category = BuildCategories(GameEffect.ogLaraOutfit, CosmeticEffects)},
            new("Vegas Outfit", GameEffect.vegasOutfit.ToString()) { Price = 10, Description = "Change Lara into the Vegas outfit", Category = BuildCategories(GameEffect.vegasOutfit, CosmeticEffects)},
            new("Put on Sunglasses", GameEffect.putOnSunglasses.ToString()) { Price = 10, Description = "Put on Lara's signature sunglasses", Category = BuildCategories(GameEffect.putOnSunglasses, CosmeticEffects)},
            new("Take off Sunglasses", GameEffect.takeOffSunglasses.ToString()) { Price = 10, Description = "Take off Lara's signature sunglasses", Category = BuildCategories(GameEffect.takeOffSunglasses, CosmeticEffects)},
            #endregion

            #region Movement
            new("Force Tank Controls", GameEffect.forceTankControls.ToString()) { Price = 50, Description = "Force tank controls", Category = BuildCategories(GameEffect.forceTankControls, MovementEffects), Duration = 60, IsDurationEditable = true},
            new("Force Modern Controls", GameEffect.forceModernControls.ToString()) { Price = 50, Description = "Force modern controls", Category = BuildCategories(GameEffect.forceModernControls, MovementEffects), Duration = 60, IsDurationEditable = true},
            new("Force Forward Movement", GameEffect.forceMoveForward.ToString()) { Price = 150, Description = "Force Lara to move forward", Category = BuildCategories(GameEffect.forceMoveForward, MovementEffects), Duration = 1, IsDurationEditable = true},
            new("Force Backward Movement", GameEffect.forceMoveBackward.ToString()) { Price = 150, Description = "Force Lara to move backward", Category = BuildCategories(GameEffect.forceMoveBackward, MovementEffects), Duration = 1, IsDurationEditable = true},
            new("Force Jump", GameEffect.forceJump.ToString()) { Price = 150, Description = "Force Lara to jump", Category = BuildCategories(GameEffect.forceJump, MovementEffects), Duration = 1, IsDurationEditable = true},
            new("Force Walk", GameEffect.forceWalk.ToString()) { Price = 150, Description = "Force Lara to walk", Category = BuildCategories(GameEffect.forceWalk, MovementEffects), Duration = 10, IsDurationEditable = true},
            new("Force Swan Dive", GameEffect.forceSwanDive.ToString()) { Price = 150, Description = "Force Lara to Swan Dive", Category = BuildCategories(GameEffect.forceSwanDive, MovementEffects), Duration = 2, IsDurationEditable = true},
            #endregion

            #region Other
            new("Restart Current Level", GameEffect.restartLevel.ToString()) { Price = 400, Description = "Restart the current level", Category = BuildCategories(GameEffect.restartLevel, OtherEffects)},
            new("Invisi-Lara", GameEffect.invisibleLara.ToString()) { Price = 50, Description = "Turn Lara invisible(forces OG graphics)", Category = BuildCategories(GameEffect.invisibleLara, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Dark Lara", GameEffect.darkLara.ToString()) { Note = "TR1/TR2", Price = 50, Description = "Turn Lara into Dark Lara(forces OG graphics)", Category = BuildCategories(GameEffect.darkLara, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Force Unequip", GameEffect.forceUnequip.ToString()) { Price = 50, Description = "Force Lara to put her weapons away", Category = BuildCategories(GameEffect.forceUnequip, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Halve Fall Damage", GameEffect.halfFallDamage.ToString()) { Price = 50, Description = "Halve the fall damage Lara takes", Category = BuildCategories(GameEffect.halfFallDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double Fall Damage", GameEffect.doubleFallDamage.ToString()) { Price = 50, Description = "Double the fall damage Lara takes", Category = BuildCategories(GameEffect.doubleFallDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Flood Level", GameEffect.floodLevel.ToString()) { Price = 500, Description = "Flood the current level", Category = BuildCategories(GameEffect.floodLevel, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Super \"Jump\"", GameEffect.superJump.ToString()) { Price = 150, Description = "Immediately teleport Lara into the air directly above where she is", Category = BuildCategories(GameEffect.superJump, OtherEffects), SessionCooldown = SITimeSpan.FromSeconds(10)},
            new("Double Pistol Damage", GameEffect.doublePistolDamage.ToString()) { Price = 100, Description = "Double the damage Lara's pistols deal", Category = BuildCategories(GameEffect.doublePistolDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double Shotgun Damage", GameEffect.doubleShotgunDamage.ToString()) { Price = 100, Description = "Double the damage Lara's Shotgun deals", Category = BuildCategories(GameEffect.doubleShotgunDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double Uzi Damage", GameEffect.doubleUziDamage.ToString()) { Price = 100, Description = "Double the damage Lara's Uzis deal", Category = BuildCategories(GameEffect.doubleUziDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable Pistol Damage", GameEffect.disablePistolDamage.ToString()) { Price = 100, Description = "Disable Lara's pistols from dealing damage", Category = BuildCategories(GameEffect.disablePistolDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable Shotgun Damage", GameEffect.disableShotgunDamage.ToString()) { Price = 100, Description = "Disable Lara's Shotgun from dealing damage", Category = BuildCategories(GameEffect.disableShotgunDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable Uzi Damage", GameEffect.disableUziDamage.ToString()) { Price = 100, Description = "Disable Lara's Uzis from dealing damage", Category = BuildCategories(GameEffect.disableUziDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double Magnum Damage", GameEffect.tr1DoubleMagnumDamage.ToString()) { Note = "TR1", Price = 100, Description = "Double the damage Lara's Magnums deal", Category = BuildCategories(GameEffect.tr1DoubleMagnumDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable Magnum Damage", GameEffect.tr1DisableMagnumDamage.ToString()) { Note = "TR1", Price = 100, Description = "Disable Lara's Magnums from dealing damage", Category = BuildCategories(GameEffect.tr1DisableMagnumDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double Harpoon Damage", GameEffect.doubleHarpoonDamage.ToString()) { Note = "TR2/TR3", Price = 100, Description = "Double the damage Lara's Harpoon Gun deals", Category = BuildCategories(GameEffect.doubleHarpoonDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable Harpoon Damage", GameEffect.disableHarpoonDamage.ToString()) { Note = "TR2/TR3",Price = 100, Description = "Disable Lara's Harpoon Gun from dealing damage", Category = BuildCategories(GameEffect.disableHarpoonDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double Automatic Pistols Damage", GameEffect.doubleAutoPistolsDamage.ToString()) { Note = "TR2", Price = 100, Description = "Double the damage Lara's Automatic Pistols deal", Category = BuildCategories(GameEffect.doubleAutoPistolsDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable Automatic Pistols Damage", GameEffect.disableAutoPistolsDamage.ToString()) { Note = "TR2", Price = 100, Description = "Disable Lara's Automatic Pistols from dealing damage", Category = BuildCategories(GameEffect.disableAutoPistolsDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double M16 Damage", GameEffect.doubleM16Damage.ToString()) { Note = "TR2", Price = 100, Description = "Double the damage Lara's M16 deals", Category = BuildCategories(GameEffect.doubleM16Damage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable M16 Damage", GameEffect.disableM16Damage.ToString()) { Note = "TR2", Price = 100, Description = "Disable Lara's M16 from dealing damage", Category = BuildCategories(GameEffect.disableM16Damage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double Desert Eagle Damage", GameEffect.doubleDeagleDamage.ToString()) { Note = "TR3",Price = 100, Description = "Double the damage Lara's Desert Eagle deals", Category = BuildCategories(GameEffect.doubleDeagleDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable Desert Eagle Damage", GameEffect.disableDeagleDamage.ToString()) { Note = "TR3", Price = 100, Description = "Disable Lara's Desert Eagle from dealing damage", Category = BuildCategories(GameEffect.disableDeagleDamage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Double MP5 Damage", GameEffect.doubleMP5Damage.ToString()) { Note = "TR3", Price = 100, Description = "Double the damage Lara's MP5 deals", Category = BuildCategories(GameEffect.doubleMP5Damage, OtherEffects), Duration = 30, IsDurationEditable = true},
            new("Disable MP5 Damage", GameEffect.disableMP5Damage.ToString()) { Note = "TR3", Price = 100, Description = "Disable Lara's MP5 from dealing damage", Category = BuildCategories(GameEffect.disableMP5Damage, OtherEffects), Duration = 30, IsDurationEditable = true},
            
            #endregion 

            #region Backpack
            /*new("Give Pistols", "tr1GivePistols") { Description="Put Lara's pistols in her backpack", Category="Backpack" },
            new("Take Pistols", "tr1TakePistols") { Description="Take Lara's pistols out of her backpack", Category="Backpack"},*/
            new("Give Small Medi Pack", GameEffect.giveSmallMedi.ToString()) { Price = 10, Description="Give Lara a small medi pack", Category = BuildCategories(GameEffect.giveSmallMedi, BackpackEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Small Medi Pack", GameEffect.takeSmallMedi.ToString()) { Price = 10, Description="Take away one of Lara's small medi packs", Category = BuildCategories(GameEffect.takeSmallMedi, BackpackEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Give Large Medi Pack", GameEffect.giveLargeMedi.ToString()) { Price = 25, Description="Give Lara a large medi pack", Category = BuildCategories(GameEffect.giveLargeMedi, BackpackEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Large Medi Pack", GameEffect.takeLargeMedi.ToString()) { Price = 25, Description="Take away one of Lara's large medi packs", Category = BuildCategories(GameEffect.takeLargeMedi, BackpackEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Give Flare", GameEffect.giveFlare.ToString()) { Note = "TR2/TR3",Price = 10, Description="Give Lara a flare", Category = BuildCategories(GameEffect.giveFlare, BackpackEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Flare", GameEffect.takeFlare.ToString()) { Note = "TR2/TR3",Price = 10, Description="Take away one of Lara's flares", Category = BuildCategories(GameEffect.takeFlare, BackpackEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },

            new("Give Shotgun", GameEffect.giveShotgun.ToString()) { Price = 50, Description="Put Lara's shotgun in her backpack", Category=BuildCategories(GameEffect.giveShotgun, BackpackEffects) },
            new("Take Shotgun", GameEffect.takeShotgun.ToString()) { Price = 50, Description="Take Lara's shotgun out of her backpack", Category=BuildCategories(GameEffect.takeShotgun, BackpackEffects)},
            new("Give Uzis", GameEffect.giveUzis.ToString()) { Price = 50, Description="Put Lara's Uzis in her backpack", Category=BuildCategories(GameEffect.giveUzis, BackpackEffects) },
            new("Take Uzis", GameEffect.takeUzis.ToString()) { Price = 50, Description="Take Lara's Uzis out of her backpack", Category=BuildCategories(GameEffect.takeUzis, BackpackEffects)},
            new("Give Magnums", GameEffect.tr1GiveMagnums.ToString()) { Note = "TR1", Price = 50, Description="Put Lara's Magnums in her backpack", Category=BuildCategories(GameEffect.tr1GiveMagnums, BackpackEffects) },
            new("Take Magnums", GameEffect.tr1TakeMagnums.ToString()) { Note = "TR1", Price = 50, Description="Take Lara's Magnums out of her backpack", Category=BuildCategories(GameEffect.tr1TakeMagnums, BackpackEffects)},
            new("Give Automatic Pistols", GameEffect.giveAutoPistols.ToString()) { Note = "TR2", Price = 50, Description="Put Lara's Automatic Pistols in her backpack", Category=BuildCategories(GameEffect.giveAutoPistols, BackpackEffects) },
            new("Take Automatic Pistols", GameEffect.takeAutoPistols.ToString()) { Note = "TR2", Price = 50, Description="Take Lara's Automatic Pistols out of her backpack", Category=BuildCategories(GameEffect.takeAutoPistols, BackpackEffects)},
            new("Give M16", GameEffect.giveM16.ToString()) { Note = "TR2", Price = 50, Description="Put Lara's M16 in her backpack", Category=BuildCategories(GameEffect.giveM16, BackpackEffects) },
            new("Take M16", GameEffect.takeM16.ToString()) { Note = "TR2", Price = 50, Description="Take Lara's M16 out of her backpack", Category=BuildCategories(GameEffect.takeM16, BackpackEffects)},
            new("Give Grenade Launcher", GameEffect.giveGrenadeLauncher.ToString()) { Note = "TR2/TR3", Price = 50, Description="Put Lara's Grenade Launcher in her backpack", Category=BuildCategories(GameEffect.giveGrenadeLauncher, BackpackEffects) },
            new("Take Grenade Launcher", GameEffect.takeGrenadeLauncher.ToString()) { Note = "TR2/TR3", Price = 50, Description="Take Lara's Grenade Launcher out of her backpack", Category=BuildCategories(GameEffect.takeGrenadeLauncher, BackpackEffects)},
            new("Give Harpoon Gun", GameEffect.giveHarpoonGun.ToString()) { Note = "TR2/TR3", Price = 50, Description="Put Lara's Harpoon Gun in her backpack", Category=BuildCategories(GameEffect.giveHarpoonGun, BackpackEffects) },
            new("Take Harpoon Gun", GameEffect.takeHarpoonGun.ToString()) { Note = "TR2/TR3", Price = 50, Description="Take Lara's Harpoon Gun out of her backpack", Category=BuildCategories(GameEffect.takeHarpoonGun, BackpackEffects)},
            new("Give Desert Eagle", GameEffect.giveDesertEagle.ToString()) { Note = "TR3", Price = 50, Description="Put Lara's Desert Eagle in her backpack", Category=BuildCategories(GameEffect.giveDesertEagle, BackpackEffects) },
            new("Take Desert Eagle", GameEffect.takeDesertEagle.ToString()) { Note = "TR3", Price = 50, Description="Take Lara's Desert Eagle out of her backpack", Category=BuildCategories(GameEffect.takeDesertEagle, BackpackEffects)},
            new("Give MP5", GameEffect.giveMP5.ToString()) { Note = "TR3", Price = 50, Description="Put Lara's MP5 in her backpack", Category=BuildCategories(GameEffect.giveMP5, BackpackEffects)},
            new("Take MP5",  GameEffect.takeMP5.ToString()) { Note = "TR3", Price = 50, Description="Take Lara's MP5 out of her backpack", Category=BuildCategories(GameEffect.takeMP5, BackpackEffects)},
            new("Give Rocket Launcher", GameEffect.giveRocketLauncher.ToString()) { Note = "TR3",Price = 50, Description="Put Lara's Rocket Launcher in her backpack", Category=BuildCategories(GameEffect.giveRocketLauncher, BackpackEffects) },
            new("Take Rocket Launcher", GameEffect.takeRocketLauncher.ToString()) { Note = "TR3", Price = 50, Description="Take Lara's Rocket Launcher out of her backpack", Category=BuildCategories(GameEffect.takeRocketLauncher, BackpackEffects)},
            #endregion

            #region Items and Ammo
            /*new("Give Compass", "tr1GiveCompass") { Price = 50, Description = "Give Lara her compass", Category="Items"},
            new("Take Compass", "tr1TakeCompass") { Price = 50, Description = "Take Lara's compass away", Category="Items"},*/
            new("Give Shotgun Ammo", GameEffect.giveShotgunAmmo.ToString()) { Price = 10, Description="Give Lara 2 shotgun shells", Category= BuildCategories(GameEffect.giveShotgunAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Shotgun Ammo", GameEffect.takeShotgunAmmo.ToString()) { Price = 10, Description="Take 2 of Lara's shotgun shells", Category= BuildCategories(GameEffect.takeShotgunAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give Uzi Ammo", GameEffect.giveUziAmmo.ToString()) { Price = 10, Description="Give Lara 50 Uzi bullets", Category= BuildCategories(GameEffect.giveUziAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Uzi Ammo", GameEffect.takeUziAmmo.ToString()) { Price = 10, Description="Take 50 of Lara's Uzi bullets", Category= BuildCategories(GameEffect.takeUziAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give Magnum Ammo", GameEffect.tr1GiveMagnumAmmo.ToString()) { Note ="TR1", Price = 25, Description="Give Lara 25 Magnum bullets", Category= BuildCategories(GameEffect.tr1GiveMagnumAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Magnum Ammo", GameEffect.tr1TakeMagnumAmmo.ToString()) { Note = "TR1", Price = 25, Description="Take 25 of Lara's Magnum bullets", Category= BuildCategories(GameEffect.tr1TakeMagnumAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give Automatic Pistol Ammo", GameEffect.giveAutoPistolAmmo.ToString()) { Note = "TR2", Price = 10, Description="Give Lara 25 automatic pistol bullets", Category = BuildCategories(GameEffect.giveAutoPistolAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Automatic Pistol Ammo", GameEffect.takeAutoPistolAmmo.ToString()) { Note = "TR2", Price = 10, Description="Take 25 of Lara's automatic pistol bullets", Category = BuildCategories(GameEffect.takeAutoPistolAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give M16 Ammo", GameEffect.giveM16Ammo.ToString()) { Note = "TR2", Price = 10, Description="Give Lara 30 M16 bullets", Category=BuildCategories(GameEffect.giveM16Ammo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take M16 Ammo", GameEffect.takeM16Ammo.ToString()) { Note = "TR2", Price = 10, Description="Take 30 of Lara's M16 bullets", Category=BuildCategories(GameEffect.takeM16Ammo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give Grenades", GameEffect.giveGrenadeAmmo.ToString()) { Note = "TR2/TR3", Price = 10, Description="Give Lara 2 grenades", Category=BuildCategories(GameEffect.giveGrenadeAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Grenades", GameEffect.takeGrenadeAmmo.ToString()) { Note = "TR2/TR3", Price = 10, Description="Take 2 of Lara's grenades", Category=BuildCategories(GameEffect.takeGrenadeAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give Harpoons", GameEffect.giveHarpoonAmmo.ToString()) { Note = "TR2/TR3",Price = 10, Description="Give Lara 4 harpoons", Category=BuildCategories(GameEffect.giveHarpoonAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Harpoons", GameEffect.takeHarpoonAmmo.ToString()) { Note = "TR2/TR3", Price = 10, Description="Take 4 of Lara's harpoons", Category= BuildCategories(GameEffect.takeHarpoonAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give MP5 Ammo", GameEffect.giveMP5Ammo.ToString()) { Note = "TR3", Price = 10, Description="Give Lara 30 MP5 bullets", Category= BuildCategories(GameEffect.giveMP5Ammo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take MP5 Ammo", GameEffect.takeMP5Ammo.ToString()) { Note = "TR3", Price = 10, Description="Take 30 of Lara's MP5 bullets", Category= BuildCategories(GameEffect.takeMP5Ammo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give Desert Eagle Ammo", GameEffect.giveDeagleAmmo.ToString()) { Note = "TR3", Price = 10, Description="Give Lara 7 Desert Eagle bullets", Category= BuildCategories(GameEffect.giveDeagleAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Desert Eagle Ammo", GameEffect.takeDeagleAmmo.ToString()) { Note = "TR3", Price = 10, Description="Take 7 of Lara's Desert Eagle bullets", Category= BuildCategories(GameEffect.takeDeagleAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            new("Give Rockets", GameEffect.giveRocketAmmo.ToString()) { Note = "TR3", Price = 10, Description="Give Lara 2 rockets", Category= BuildCategories(GameEffect.giveRocketAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF) },
            new("Take Rockets", GameEffect.takeRocketAmmo.ToString()) { Note = "TR3", Price = 10, Description="Take 2 of Lara's rockets", Category= BuildCategories(GameEffect.takeRocketAmmo, AmmoEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 0x7FFF)},
            
            #endregion

            #region Lara's Meters
            new("Heal Lara", GameEffect.healLara.ToString()) { Price = 100, Description="Heal Lara for 10% HP. Who needs medi packs when you can heal directly?", Category= BuildCategories(GameEffect.healLara, MeterEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 10) },
            new("Increase Lara's Max HP", GameEffect.increaseMaxHP.ToString()) { Price = 100, Description = "Permanently increase Lara's max HP by 10%", Category = BuildCategories(GameEffect.increaseMaxHP, MeterEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 10) },
            new("Hurt Lara", GameEffect.hurtLara.ToString()) { Price = 100, Description="Hurt Lara for 10% HP. You monster.", Category= BuildCategories(GameEffect.hurtLara, MeterEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 10) },
            new("Decrease Lara's Max HP", GameEffect.decreaseMaxHP.ToString()) { Price = 100, Description = "Permanently decrease Lara's max HP by 10%", Category = BuildCategories(GameEffect.decreaseMaxHP, MeterEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 10) },
            new("Give Lara O2", GameEffect.giveO2.ToString()) { Price = 100, Description="Give Lara a breath of fresh air, +10% O2", Category= BuildCategories(GameEffect.giveO2, MeterEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 10) },
            new("Increase Lara's Max O2", GameEffect.increaseMaxO2.ToString()) { Price = 100, Description = "Permanently increase Lara's max O2 by 10%", Category = BuildCategories(GameEffect.increaseMaxO2, MeterEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 10) },
            new("Take Lara O2", GameEffect.takeO2.ToString()) { Price = 100, Description="Take Lara's breath away. Literally. -10% O2", Category= BuildCategories(GameEffect.takeO2, MeterEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 10) },
            new("Decrease Lara's Max O2", GameEffect.decreaseMaxO2.ToString()) { Price = 100, Description = "Permanently decrease Lara's max O2 by 10%", Category = BuildCategories(GameEffect.decreaseMaxO2, MeterEffects), DefaultQuantity = 1, Quantity = new QuantityRange(1, 10) },
            new("\"Poison\" Lara", GameEffect.poisonLara.ToString()) { Price = 300, Description = "Slowly tick away Lara's HP until Lara uses a medi pack, the poison ticks out, or Lara dies", Category = BuildCategories(GameEffect.poisonLara, MeterEffects), Duration = 60, IsDurationEditable = true},
            new("Disable Stamina", GameEffect.disableStamina.ToString()) { Note = "TR3", Price = 200, Description = "Disable Lara from using stamina for some time", Category = BuildCategories(GameEffect.disableStamina, MeterEffects), Duration = 30, IsDurationEditable =true},
            new("Half Stamina", GameEffect.halfStamina.ToString()) { Note = "TR2/TR3", Price = 100, Description = "Set Lara's max stamina to 1/2 for some time", Category = BuildCategories(GameEffect.halfStamina, MeterEffects), Duration = 30, IsDurationEditable =true},
            new("Infinite Stamina", GameEffect.infiniteStamina.ToString()) { Note = "TR3", Price = 200, Description = "Give Lara infinite stamina for some time", Category = BuildCategories(GameEffect.infiniteStamina, MeterEffects), Duration = 30, IsDurationEditable =true},
            #endregion
        };

        private enum CurrentGame
        {
            TR1 = 0,
            TR2 = 1,
            TR3 = 2
        }

        private enum BackpackItem
        {
            TR1_Compass = 0xF1940,
            TR1_Pistols = 0xECC60,
            TR1_Shotgun = 0xED930,
            TR1_Magnums = 0xF6620,
            TR1_Uzis = 0xF3FB0,
            TR1_LargeMedi = 0xEB2C0,
            TR1_SmallMedi = 0xE7F80,
            TR1_ShotgunAmmo = 0xE65E0, //1 pickup = 2 shells [TR1]
            TR1_MagnumAmmo = 0xEFFA0, //1 pickup = 25 bullets 
            TR1_UziAmmo = 0xF4C80, //1 pickup = 50 bullets [TR1]
            TR2_Compass = 0x126110,
            TR2_Pistols = 0x121430,
            TR2_Shotgun = 0x122DD0,
            TR2_GrenadeLauncher = 0x1160D0,
            TR2_AutomaticPistols = 0x12BF00,
            TR2_Uzis = 0x128BC0,
            TR2_M16 = 0x11EDC0,
            TR2_HarpoonGun = 0x119410,
            TR2_Flares = 0x12B230,
            TR2_LargeMedi = 0x11E0F0,
            TR2_SmallMedi = 0x11A0E0,
            TR2_ShotgunAmmo = 0x117A70,
            TR2_APAmmo = 0x124770,
            TR2_UziAmmo = 0x129890,
            TR2_M16Ammo = 0x11D420,
            TR2_GrenadeAmmo = 0x120760,
            TR2_HarpoonAmmo = 0x122100,
            TR3_Compass = 0x17FAB0,
            TR3_Pistols = 0x177A90,
            TR3_Shotgun = 0x17ADD0,
            TR3_DesertEagle = 0x186570,
            TR3_Uzis = 0x182560,
            TR3_MP5 = 0x175420,
            TR3_RocketLauncher = 0x16AD90,
            TR3_GrenadeLauncher = 0x184BD0,
            TR3_HarpoonGun = 0x16FA70,
            TR3_Flares = 0x1858A0,
            TR3_LargeMedi = 0x174750,
            TR3_SmallMedi = 0x170740,
            TR3_ShotgunAmmo = 0x16E0D0,
            TR3_DesertEagleAmmo = 0x17E110,
            TR3_UziAmmo = 0x183230,
            TR3_MP5Ammo = 0x173A80,
            TR3_RocketAmmo = 0x176DC0,
            TR3_GrenadeAmmo = 0x17C770,
            TR3_HarpoonAmmo = 0x179430
        }

        private CurrentGame DetermineCurrentGame()
        {
            AddressChain gameOffset = AddressChain.Parse(Connector, $"{_mainGame}+{_currentGameOffset:X}");

            if (gameOffset.TryGetByte(out byte currentGameState))
            {
                switch (currentGameState)
                {
                    case 0:
                        if (tr1MaxHPDeclarations.Count == 0)
                        {
                            OffsetAddressChain<InjectConnector>[] maxHPDeclarations = AddressChain.Scan(Connector, new ReadOnlySpan<byte>(maxHPCodeBytes), false, _tomb1DllBase, _tomb1DllBase + 0x4D2000);
                            Log.Debug($"Found {maxHPDeclarations.Length} max HP declarations for TR1");
                            tr1MaxHPDeclarations.AddRange(maxHPDeclarations);
                        }
                        return CurrentGame.TR1;
                    case 1:
                        if (tr2MaxHPDeclarations.Count == 0)
                        {
                            OffsetAddressChain<InjectConnector>[] maxHPDeclarations = AddressChain.Scan(Connector, new ReadOnlySpan<byte>(maxHPCodeBytes), false, _tomb2DllBase, _tomb2DllBase + 0x509000);
                            Log.Debug($"Found {maxHPDeclarations.Length} max HP declarations for TR2");
                            tr2MaxHPDeclarations.AddRange(maxHPDeclarations);
                        }
                        return CurrentGame.TR2;
                    case 2:
                        if (tr3MaxHPDeclarations.Count == 0)
                        {
                            OffsetAddressChain<InjectConnector>[] maxHPDeclarations = AddressChain.Scan(Connector, new ReadOnlySpan<byte>(maxHPCodeBytes), false, _tomb3DllBase, _tomb3DllBase + 0x56F000);
                            Log.Debug($"Found {maxHPDeclarations.Length} max HP declarations for TR3");
                            tr3MaxHPDeclarations.AddRange(maxHPDeclarations);
                        }
                        return CurrentGame.TR3;
                }
            }

            Log.Error("Could not detect the current game. Going to try and assume TR1, but will likely not work");
            return CurrentGame.TR1;
        }

        private void InitializeEffectStatusTracking()
        {
            BuildEffectLookup();
            UpdateEffectMenuStatus(DetermineCurrentGame(), true);
            _gameStatusTimer?.Dispose();
            _gameStatusTimer = new System.Threading.Timer(_ => SafeUpdateEffectMenuStatus(), null, _gameStatusPollInterval, _gameStatusPollInterval);
        }

        private void StopEffectStatusTracking()
        {
            _gameStatusTimer?.Dispose();
            _gameStatusTimer = null;
            _lastReportedGame = null;
        }

        private void SafeUpdateEffectMenuStatus()
        {
            try
            {
                UpdateEffectMenuStatus(DetermineCurrentGame());
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to update menu effect status: {ex.Message}");
            }
        }

        private void UpdateEffectMenuStatus(CurrentGame currentGame, bool force = false)
        {
            BuildEffectLookup();
            lock (_statusLock)
            {
                if (!force && _lastReportedGame == currentGame)
                    return;

                _lastReportedGame = currentGame;

                List<(Effect effect, string? effectId, EffectStatus status)> updates = [];
                foreach (KeyValuePair<GameEffect, HashSet<CurrentGame>> restriction in _effectGameRestrictions)
                {
                    if (!_effectById.TryGetValue(restriction.Key, out Effect? effect))
                        continue;

                    bool isVisible = restriction.Value.Contains(currentGame);
                    EffectStatus status = isVisible ? EffectStatus.MenuVisible : EffectStatus.MenuHidden;
                    _effectIdByGameEffect.TryGetValue(restriction.Key, out string? effectId);
                    updates.Add((effect, effectId, status));
                }

                if (updates.Count == 0)
                    return;

                List<(string effectId, EffectStatus status)> statusUpdates = [];
                foreach ((Effect _, string? effectId, EffectStatus status) in updates)
                {
                    if (!string.IsNullOrWhiteSpace(effectId))
                        statusUpdates.Add((effectId, status));
                }

                if (statusUpdates.Count == 0)
                    return;

                if (TryReportStatusBatch(statusUpdates))
                    return;

                List<string> hiddenIds = [];
                List<string> visibleIds = [];
                foreach ((string effectId, EffectStatus status) in statusUpdates)
                {
                    if (status == EffectStatus.MenuHidden)
                        hiddenIds.Add(effectId);
                    else if (status == EffectStatus.MenuVisible)
                        visibleIds.Add(effectId);
                }

                bool reported = true;
                if (hiddenIds.Count > 0)
                {
                    reported &= (TryReportStatusByIdList(hiddenIds, EffectStatus.MenuHidden, true)
                        || TryReportStatusByIdList(hiddenIds, EffectStatus.MenuHidden));
                }
                if (visibleIds.Count > 0)
                {
                    reported &= (TryReportStatusByIdList(visibleIds, EffectStatus.MenuVisible, true)
                        || TryReportStatusByIdList(visibleIds, EffectStatus.MenuVisible));
                }

                if (reported)
                    return;

                foreach ((Effect effect, string? _, EffectStatus status) in updates)
                    ReportStatus(effect, status);
            }
        }

        private void BuildEffectLookup()
        {
            if (_effectById.Count > 0)
                return;

            foreach (Effect effect in Effects)
            {
                string? effectId = GetEffectId(effect);
                if (effectId != null && Enum.TryParse(effectId, out GameEffect gameEffect))
                {
                    _effectById[gameEffect] = effect;
                    _effectIdByGameEffect[gameEffect] = effectId;
                    continue;
                }

                string? displayName = GetEffectDisplayName(effect);
                if (displayName != null && _effectNameToId.TryGetValue(displayName, out GameEffect mappedEffect))
                {
                    _effectById[mappedEffect] = effect;
                    _effectIdByGameEffect[mappedEffect] = displayName;
                }
            }
        }

        private bool TryReportStatusBatch(IReadOnlyList<(string effectId, EffectStatus status)> updates)
        {
            try
            {
                foreach (System.Reflection.MethodInfo method in GetAllInstanceMethods(GetType()))
                {
                    if (!string.Equals(method.Name, "ReportStatus", StringComparison.Ordinal))
                        continue;
                    System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 1)
                        continue;
                    Type paramType = parameters[0].ParameterType;
                    if (!TryGetEnumerableElementType(paramType, out Type? elementType))
                        continue;

                    Array batch = Array.CreateInstance(elementType, updates.Count);
                    for (int i = 0; i < updates.Count; i++)
                    {
                        object item = Activator.CreateInstance(elementType)!;
                        SetStatusUpdateValues(item, updates[i].effectId, updates[i].status);
                        batch.SetValue(item, i);
                    }

                    method.Invoke(this, [batch]);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to report status batch: {ex.Message}");
            }
            return false;
        }


        private static bool TryGetEnumerableElementType(Type type, out Type? elementType)
        {
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return elementType != null;
            }

            if (type == typeof(IEnumerable))
            {
                elementType = typeof(object);
                return true;
            }

            foreach (Type iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    elementType = iface.GetGenericArguments()[0];
                    return true;
                }
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private bool TryReportStatusByIdList(IReadOnlyList<string> effectIds, EffectStatus status)
        {
            try
            {
                string[] idArray = effectIds.ToArray();
                foreach (System.Reflection.MethodInfo method in GetAllInstanceMethods(GetType()))
                {
                    if (!string.Equals(method.Name, "ReportStatus", StringComparison.Ordinal))
                        continue;
                    System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 2)
                    {
                        Type idsType = parameters[0].ParameterType;
                        Type statusType = parameters[1].ParameterType;
                        if (statusType != typeof(EffectStatus))
                            continue;
                        if (!idsType.IsAssignableFrom(typeof(string[])) && !idsType.IsAssignableFrom(typeof(IEnumerable<string>)))
                            continue;

                        method.Invoke(this, [idArray, status]);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to report status by ID list: {ex.Message}");
            }
            return false;
        }

        private bool TryReportStatusByIdList(IReadOnlyList<string> effectIds, EffectStatus status, bool withIdentifier)
        {
            if (!withIdentifier)
                return TryReportStatusByIdList(effectIds, status);

            try
            {
                string[] idArray = effectIds.ToArray();
                foreach (System.Reflection.MethodInfo method in GetAllInstanceMethods(GetType()))
                {
                    if (!string.Equals(method.Name, "ReportStatus", StringComparison.Ordinal))
                        continue;
                    System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 3)
                        continue;

                    Type idsType = parameters[0].ParameterType;
                    Type identifierType = parameters[1].ParameterType;
                    Type statusType = parameters[2].ParameterType;
                    if (statusType != typeof(EffectStatus))
                        continue;
                    if (!idsType.IsAssignableFrom(typeof(string[])) && !idsType.IsAssignableFrom(typeof(IEnumerable<string>)))
                        continue;

                    object? identifierValue = Enum.GetValues(identifierType)
                        .Cast<object?>()
                        .FirstOrDefault(value => string.Equals(value?.ToString(), "Effect", StringComparison.OrdinalIgnoreCase));
                    if (identifierValue == null)
                        continue;

                    method.Invoke(this, [idArray, identifierValue, status]);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to report status by ID list with identifier: {ex.Message}");
            }
            return false;
        }


        private static IEnumerable<System.Reflection.MethodInfo> GetAllInstanceMethods(Type type)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic;
            for (Type? current = type; current != null; current = current.BaseType)
            {
                foreach (System.Reflection.MethodInfo method in current.GetMethods(flags))
                    yield return method;
            }
        }

        private static void SetStatusUpdateValues(object item, string effectId, EffectStatus status)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic;

            if (!SetMemberValue(item, "EffectID", effectId, flags))
            {
                if (!SetMemberValue(item, "EffectId", effectId, flags))
                {
                    if (!SetMemberValue(item, "Id", effectId, flags))
                        SetMemberValue(item, "ID", effectId, flags);
                }
            }

            if (!SetMemberValue(item, "Status", status, flags))
            {
                if (!SetMemberValue(item, "State", status, flags))
                    SetMemberValue(item, "EffectStatus", status, flags);
            }
        }

        private static bool SetMemberValue(object target, string name, object value, System.Reflection.BindingFlags flags)
        {
            System.Reflection.PropertyInfo? property = target.GetType().GetProperty(name, flags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            System.Reflection.FieldInfo? field = target.GetType().GetField(name, flags);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }

            return false;
        }

        private static string? GetEffectId(Effect effect)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic;
            string? id = effect.GetType().GetProperty("Code", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetProperty("ID", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetProperty("Id", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetProperty("EffectID", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetProperty("EffectId", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetField("Code", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetField("ID", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetField("Id", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetField("EffectID", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetField("EffectId", flags)?.GetValue(effect)?.ToString();
            if (id != null && _gameEffectIds.Contains(id))
                return id;

            foreach (System.Reflection.PropertyInfo property in effect.GetType().GetProperties(flags))
            {
                if (property.PropertyType == typeof(string)
                    && property.GetValue(effect) is string value
                    && _gameEffectIds.Contains(value))
                {
                    return value;
                }
            }

            foreach (System.Reflection.FieldInfo field in effect.GetType().GetFields(flags))
            {
                if (field.FieldType == typeof(string)
                    && field.GetValue(effect) is string value
                    && _gameEffectIds.Contains(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string? GetEffectDisplayName(Effect effect)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic;
            return effect.GetType().GetProperty("Name", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetProperty("DisplayName", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetProperty("Title", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetField("Name", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetField("DisplayName", flags)?.GetValue(effect)?.ToString()
                ?? effect.GetType().GetField("Title", flags)?.GetValue(effect)?.ToString();
        }

        protected override GameState GetGameState()
        {
            try
            {
                if (IsPausedOrMenu() || IsLevelCompleteScreen())
                    return GameState.WrongMode;
                return GameState.Ready;
            }
            catch { return GameState.Unknown; }
        }

        private int GetBackpackItemCount(CurrentGame currentGame)
        {
            bool success;
            byte backpackSize;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    success = AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1BackpackSizeOffset:X}").TryGetByte(out backpackSize);
                    if (!success)
                    {
                        throw new Exception("Failed to get backpack item count for current game!");
                    }
                    Log.Debug($"Backpack size is {backpackSize}");
                    return (int)backpackSize;
                case CurrentGame.TR2:
                    success = AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2BackpackSizeOffset:X}").TryGetByte(out backpackSize);
                    if (!success)
                    {
                        throw new Exception("Failed to get backpack item count for current game!");
                    }
                    Log.Debug($"Backpack size is {backpackSize}");
                    return (int)backpackSize;
                case CurrentGame.TR3:
                    success = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3BackpackSizeOffset:X}").TryGetByte(out backpackSize);
                    if (!success)
                    {
                        throw new Exception("Failed to get backpack item count for current game!");
                    }
                    Log.Debug($"Backpack size is {backpackSize}");
                    return (int)backpackSize;
                default:
                    return 0;
            }
        }

        private bool? IsInBackpack(CurrentGame currentGame, BackpackItem itemToCheck)
        {
            try
            {
                return GetBackpackStatus(currentGame, itemToCheck) != -1;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the index of the backpack slot the item is in, or -1 if not present
        /// </summary>
        /// <param name="itemToCheck"></param>
        /// <returns></returns>
        private int GetBackpackStatus(CurrentGame currentGame, BackpackItem itemToCheck)
        {
            int backpackSize = GetBackpackItemCount(currentGame);
            ulong dllBase;
            List<uint> backpackSlotOffsets;
            string currentDll;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    currentDll = _tomb1Dll;
                    dllBase = _tomb1DllBase;
                    backpackSlotOffsets = _tr1BackpackSlotOffsets;
                    break;
                case CurrentGame.TR2:
                    currentDll = _tomb2Dll;
                    dllBase = _tomb2DllBase;
                    backpackSlotOffsets = _tr2BackpackSlotOffsets;
                    break;
                case CurrentGame.TR3:
                    currentDll = _tomb3Dll;
                    dllBase = _tomb3DllBase;
                    backpackSlotOffsets = _tr3BackpackSlotOffsets;
                    break;
                default:
                    throw new Exception("Unknown and invalid game selected");
            }
            try
            {
                for (int i = 0; i < backpackSize; i++)
                {
                    bool success = AddressChain.Parse(Connector, $"{currentDll}+{backpackSlotOffsets[i]:X}").TryGetULong(out ulong backpackItemBytes);
                    if (!success)
                    {
                        throw new Exception($"Failed to get backpack status for {itemToCheck}");
                    }
                    Log.Debug($"Address found at index #{i}: {backpackItemBytes:X}");
                    BackpackItem backpackItem = (BackpackItem)(backpackItemBytes - dllBase);
                    if (backpackItem == itemToCheck)
                    {
                        Log.Debug($"Found {itemToCheck} at index {i} in backpack");
                        return i;
                    }
                }
            }
            catch
            {
                Log.Error($"Failed to search through backpack for {itemToCheck}");
                throw new Exception($"Failed to search through backpack for {itemToCheck}");
            }
            return -1;
        }

        private AddressChain<InjectConnector> GetBackpackSlotCountAddress(CurrentGame currentGame, int index)
        {
            uint slotCountsOffset;
            string gameDll;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    slotCountsOffset = _tr1BackpackSlotCountsOffset;
                    gameDll = _tomb1Dll;
                    break;
                case CurrentGame.TR2:
                    gameDll = _tomb2Dll;
                    slotCountsOffset = _tr2BackpackSlotCountsOffset;
                    break;
                case CurrentGame.TR3:
                    gameDll = _tomb3Dll;
                    slotCountsOffset = _tr3BackpackSlotCountsOffset;
                    break;
                default:
                    throw new Exception("Invalid game detected, cannot get backpack slot counts");
            }
            return AddressChain.Parse(Connector, $"{gameDll}+{(slotCountsOffset + (index * 2)):X}");
        }

        private bool AdjustBackpackIndexCount(CurrentGame currentGame, int index, ushort count, bool increase, BackpackItem itemAdjusted)
        {
            AddressChain<InjectConnector> currentCountAddressChain = GetBackpackSlotCountAddress(currentGame, index);
            Log.Debug($"Location we're looking at for current count: {currentCountAddressChain.Address:X}");
            bool gotCurrentCount = currentCountAddressChain.TryGetShort(out short currentCount);
            if (gotCurrentCount)
            {
                Log.Debug($"Current count of {itemAdjusted} in backpack is: {currentCount}");
                if (increase)
                {
                    if (currentCount == 0x7FFF)
                    {
                        //already at max, cannot add more
                        return false;
                    }
                    if (currentCount + count < 0x7FFF)
                    {
                        Log.Debug($"Attempting to increase by {count}...");
                        return currentCountAddressChain.TrySetShort((short)(currentCount + count));
                    }
                    else
                    {
                        Log.Debug("Setting to max value...");
                        return currentCountAddressChain.TrySetShort(0x7FFF);
                    }
                }
                else
                {
                    if (currentCount > count)
                    {
                        return currentCountAddressChain.TrySetShort((short)(currentCount - count));
                    }
                    else
                    {
                        bool setToZero = currentCountAddressChain.TrySetShort(0);
                        if (setToZero)
                        {
                            bool removedFromBackpack = AdjustBackpackStatus(currentGame, itemAdjusted, false);
                            if (removedFromBackpack)
                            {
                                Log.Error($"Successfully zeroed {itemAdjusted} count and removed from backpack");
                                return true;
                            }
                            else
                            {
                                Log.Error($"Failed to remove {itemAdjusted} from backpack, resetting count in backpack");
                                if (currentCountAddressChain.TrySetShort(currentCount))
                                {
                                    Log.Debug("Resetting count successful");
                                }
                                else
                                {
                                    Log.Error("Failed to reset count in backpack!");
                                }
                            }
                        }
                        else
                        {
                            Log.Error($"Failed to set count of {itemAdjusted} to zero");
                        }
                    }
                }
            }
            else
            {
                Log.Error($"Failed to get current count of {itemAdjusted} in backpack.");
            }
            return false;
        }

        private bool AdjustLargeMedipackCount(CurrentGame currentGame, uint count, bool increase)
        {
            var backpackItem = currentGame switch
            {
                CurrentGame.TR1 => BackpackItem.TR1_LargeMedi,
                CurrentGame.TR2 => BackpackItem.TR2_LargeMedi,
                CurrentGame.TR3 => BackpackItem.TR3_LargeMedi,
                _ => throw new Exception("Unknown hooked game detected"),
            };
            int indexInBackpack = GetBackpackStatus(currentGame, backpackItem);

            if (increase)
            {
                if (indexInBackpack == -1)
                {
                    bool adjustedBackpackStatus = AdjustBackpackStatus(currentGame, backpackItem, true);
                    if (!adjustedBackpackStatus)
                        return false;
                    Log.Debug("Added large medi pack to backpack");
                    indexInBackpack = GetBackpackStatus(currentGame, backpackItem);
                    count--; //reduce count as we just increased it by 1
                }
            }
            else
            {
                if (indexInBackpack == -1)
                {
                    //not in backpack, cannot reduce count
                    return false;
                }
            }
            return AdjustBackpackIndexCount(currentGame, indexInBackpack, (ushort)count, increase, backpackItem);
        }

        private bool AdjustFlareCount(CurrentGame currentGame, uint count, bool increase)
        {
            var backpackItem = currentGame switch
            {
                CurrentGame.TR1 => throw new Exception("TR1 does not have flares, cannot adjust flare count"),
                CurrentGame.TR2 => BackpackItem.TR2_Flares,
                CurrentGame.TR3 => BackpackItem.TR3_Flares,
                _ => throw new Exception("Unknown hooked game detected"),
            };
            int indexInBackpack = GetBackpackStatus(currentGame, backpackItem);

            if (increase)
            {
                if (indexInBackpack == -1)
                {
                    bool adjustedBackpackStatus = AdjustBackpackStatus(currentGame, backpackItem, true);
                    if (!adjustedBackpackStatus)
                    {
                        Log.Debug("Failed to add flare to backpack");
                        return false;
                    }
                    Log.Debug("Added flare to backpack");
                    indexInBackpack = GetBackpackStatus(currentGame, backpackItem);
                    count--; //reduce count as we increase by 1 in AdjustBackpackStatus
                }
            }
            else
            {
                if (indexInBackpack == -1)
                {
                    //not in backpack, cannot reduce count
                    return false;
                }
            }
            return AdjustBackpackIndexCount(currentGame, indexInBackpack, (ushort)count, increase, backpackItem);
        }

        private bool AdjustSmallMedipackCount(CurrentGame currentGame, uint count, bool increase)
        {
            var backpackItem = currentGame switch
            {
                CurrentGame.TR1 => BackpackItem.TR1_SmallMedi,
                CurrentGame.TR2 => BackpackItem.TR2_SmallMedi,
                CurrentGame.TR3 => BackpackItem.TR3_SmallMedi,
                _ => throw new Exception("Unknown hooked game detected"),
            };
            int indexInBackpack = GetBackpackStatus(currentGame, backpackItem);

            if (increase)
            {
                if (indexInBackpack == -1)
                {
                    bool adjustedBackpackStatus = AdjustBackpackStatus(currentGame, backpackItem, true);
                    if (!adjustedBackpackStatus)
                        return false;
                    Log.Debug("Added small medi pack to backpack");
                    indexInBackpack = GetBackpackStatus(currentGame, backpackItem);
                    count--; //reduce count as we increase by 1 in AdjustBackpackStatus
                }
            }
            else
            {
                if (indexInBackpack == -1)
                {
                    //not in backpack, cannot reduce count
                    return false;
                }
            }
            return AdjustBackpackIndexCount(currentGame, indexInBackpack, (ushort)count, increase, backpackItem);
        }

        private static BackpackItem GetAmmoTypeOfGun(BackpackItem gun)
        {
            BackpackItem ammoItem = gun switch
            {
                BackpackItem.TR1_Shotgun => BackpackItem.TR1_ShotgunAmmo,
                BackpackItem.TR1_Uzis => BackpackItem.TR1_UziAmmo,
                BackpackItem.TR1_Magnums => BackpackItem.TR1_MagnumAmmo,
                BackpackItem.TR2_Shotgun => BackpackItem.TR2_ShotgunAmmo,
                BackpackItem.TR2_Uzis => BackpackItem.TR2_UziAmmo,
                BackpackItem.TR2_AutomaticPistols => BackpackItem.TR2_APAmmo,
                BackpackItem.TR2_GrenadeLauncher => BackpackItem.TR2_GrenadeAmmo,
                BackpackItem.TR2_HarpoonGun => BackpackItem.TR2_HarpoonAmmo,
                BackpackItem.TR2_M16 => BackpackItem.TR2_M16Ammo,
                BackpackItem.TR3_Shotgun => BackpackItem.TR3_ShotgunAmmo,
                BackpackItem.TR3_Uzis => BackpackItem.TR3_UziAmmo,
                BackpackItem.TR3_DesertEagle => BackpackItem.TR3_DesertEagleAmmo,
                BackpackItem.TR3_GrenadeLauncher => BackpackItem.TR3_GrenadeAmmo,
                BackpackItem.TR3_RocketLauncher => BackpackItem.TR3_RocketAmmo,
                BackpackItem.TR3_HarpoonGun => BackpackItem.TR3_HarpoonAmmo,
                BackpackItem.TR3_MP5 => BackpackItem.TR3_MP5Ammo,
                _ => throw new Exception("Impossible state reached"),
            };

            return ammoItem;
        }

        private bool AdjustBackpackStatus(CurrentGame currentGame, BackpackItem itemToChange, bool addToBackpack)
        {
            bool? isInBackpack = IsInBackpack(currentGame, itemToChange);
            if (addToBackpack)
            {
                if (isInBackpack != false)
                    return false;
            }
            else
            {
                if (isInBackpack != true)
                    return false;
            }

            string gameDll;
            List<uint> backpackSlotOffsets;
            ulong dllBase;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    gameDll = _tomb1Dll;
                    dllBase = _tomb1DllBase;
                    backpackSlotOffsets = _tr1BackpackSlotOffsets;
                    break;
                case CurrentGame.TR2:
                    gameDll = _tomb2Dll;
                    dllBase = _tomb2DllBase;
                    backpackSlotOffsets = _tr2BackpackSlotOffsets;
                    break;
                case CurrentGame.TR3:
                    gameDll = _tomb3Dll;
                    dllBase = _tomb3DllBase;
                    backpackSlotOffsets = _tr3BackpackSlotOffsets;
                    break;
                default:
                    throw new Exception($"Invalid game detected, cannot adjust backpack status for {currentGame}");
            }
            int backpackSize = GetBackpackItemCount(currentGame);
            if (addToBackpack)
            {
                if (GetBackpackStatus(currentGame, itemToChange) != -1)
                {
                    throw new ArgumentException($"{itemToChange} is already in the backpack");
                }
                if (_tr1BackpackItemsWithAmmo.Contains(itemToChange) ||
                    _tr2BackpackItemsWithAmmo.Contains(itemToChange) ||
                    _tr3BackpackItemsWithAmmo.Contains(itemToChange))
                {
                    //replace ammo in the inventory, if there are any
                    BackpackItem ammoItem = GetAmmoTypeOfGun(itemToChange);
                    int ammoSlot = GetBackpackStatus(currentGame, ammoItem);
                    if (ammoSlot != -1 && ammoSlot < backpackSize)
                    {
                        AddressChain<InjectConnector> backpackSlotCount = GetBackpackSlotCountAddress(currentGame, ammoSlot);
                        if (GetAmmoCount(ammoItem) <= 0)
                        {
                            int ammoToAdd = 0;
                            switch (ammoItem)
                            {
                                case BackpackItem.TR1_ShotgunAmmo:
                                case BackpackItem.TR2_ShotgunAmmo:
                                case BackpackItem.TR3_ShotgunAmmo:
                                    ammoToAdd = 12 * backpackSlotCount.GetShort(); //its 2 shells for each pack, but *6 because memory is weird(i think cuz lara shoots 6 bullets per trigger pull, but w/e)
                                    if (ammoToAdd == 0)
                                        ammoToAdd = 12;
                                    break;
                                case BackpackItem.TR1_MagnumAmmo:
                                case BackpackItem.TR2_APAmmo:
                                    ammoToAdd = 25 * backpackSlotCount.GetShort();
                                    if (ammoToAdd == 0)
                                        ammoToAdd = 25;
                                    break;
                                case BackpackItem.TR1_UziAmmo:
                                case BackpackItem.TR2_UziAmmo:
                                case BackpackItem.TR3_UziAmmo:
                                    ammoToAdd = 50 * backpackSlotCount.GetShort();
                                    if (ammoToAdd == 0)
                                        ammoToAdd = 50;
                                    break;
                                case BackpackItem.TR3_DesertEagle:
                                    ammoToAdd = 7 * backpackSlotCount.GetShort();
                                    if (ammoToAdd == 0)
                                        ammoToAdd = 7;
                                    break;
                                case BackpackItem.TR2_M16Ammo:
                                case BackpackItem.TR3_MP5Ammo:
                                    ammoToAdd = 30 * backpackSlotCount.GetShort();
                                    if (ammoToAdd == 0)
                                        ammoToAdd = 30;
                                    break;
                                case BackpackItem.TR2_HarpoonAmmo:
                                case BackpackItem.TR3_HarpoonAmmo:
                                    ammoToAdd = 8 * backpackSlotCount.GetShort();
                                    if (ammoToAdd == 0)
                                        ammoToAdd = 8;
                                    break;
                                case BackpackItem.TR2_GrenadeAmmo:
                                case BackpackItem.TR3_GrenadeAmmo:
                                case BackpackItem.TR3_RocketAmmo:
                                    ammoToAdd = 1 * backpackSlotCount.GetShort();
                                    if (ammoToAdd == 0)
                                        ammoToAdd = 2;
                                    break;
                            }
                            AdjustAmmoCount(ammoItem, (uint)ammoToAdd, true);
                        }
                        return AddressChain.Parse(Connector, $"{gameDll}+{backpackSlotOffsets[ammoSlot]:X}").
                            TrySetULong(dllBase + (uint)itemToChange);
                    }
                    else
                    {
                        return AddToBackpack(currentGame, itemToChange, backpackSize);
                    }
                }
                else
                {
                    //otherwise just increase the slots and add it to the end
                    return AddToBackpack(currentGame, itemToChange, backpackSize);
                }
            }
            else
            {
                int backpackSlot = GetBackpackStatus(currentGame, itemToChange);
                if (_tr1BackpackItemsWithAmmo.Contains(itemToChange) ||
                    _tr2BackpackItemsWithAmmo.Contains(itemToChange) ||
                    _tr3BackpackItemsWithAmmo.Contains(itemToChange))
                {
                    //replace gun with ammo, if possible
                    BackpackItem ammoItem = itemToChange switch
                    {
                        BackpackItem.TR1_Shotgun => BackpackItem.TR1_ShotgunAmmo,
                        BackpackItem.TR1_Uzis => BackpackItem.TR1_UziAmmo,
                        BackpackItem.TR1_Magnums => BackpackItem.TR1_MagnumAmmo,
                        BackpackItem.TR2_Shotgun => BackpackItem.TR2_ShotgunAmmo,
                        BackpackItem.TR2_Uzis => BackpackItem.TR2_UziAmmo,
                        BackpackItem.TR2_AutomaticPistols => BackpackItem.TR2_APAmmo,
                        BackpackItem.TR2_GrenadeLauncher => BackpackItem.TR2_GrenadeAmmo,
                        BackpackItem.TR2_HarpoonGun => BackpackItem.TR2_HarpoonAmmo,
                        BackpackItem.TR2_M16 => BackpackItem.TR2_M16Ammo,
                        BackpackItem.TR3_Shotgun => BackpackItem.TR3_ShotgunAmmo,
                        BackpackItem.TR3_Uzis => BackpackItem.TR3_UziAmmo,
                        BackpackItem.TR3_DesertEagle => BackpackItem.TR3_DesertEagleAmmo,
                        BackpackItem.TR3_GrenadeLauncher => BackpackItem.TR3_GrenadeAmmo,
                        BackpackItem.TR3_RocketLauncher => BackpackItem.TR3_RocketAmmo,
                        BackpackItem.TR3_HarpoonGun => BackpackItem.TR3_HarpoonAmmo,
                        BackpackItem.TR3_MP5 => BackpackItem.TR3_MP5Ammo,
                        _ => throw new Exception("Impossible state reached"),
                    };
                    ForceEquip(currentGame, 1); //Force equip pistols since weapon is removed from inventory. if this fails, its not that big of a deal
                    return AddressChain.Parse(Connector, $"{gameDll}+{backpackSlotOffsets[backpackSlot]:X}").
                        TrySetULong(dllBase + (uint)ammoItem);
                }
                else
                {
                    //otherwise decrease the slots and replace it with what's at the end
                    return RemoveFromBackpack(currentGame, itemToChange, backpackSize, backpackSlot);
                }
            }
        }

        private int GetAmmoCount(BackpackItem ammoToCheck)
        {
            bool success;
            ushort shotgunAmmo;
            ushort uziAmmo;
            ushort grenadeAmmo;
            ushort harpoonAmmo;
            switch (ammoToCheck)
            {
                case BackpackItem.TR1_ShotgunAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1ShotgunAmmoOffset:X}").TryGetUShort(out shotgunAmmo);
                    if (success)
                        return shotgunAmmo; //Shotgun ammo is stored in memory as multiples of 6
                    else
                        throw new Exception("Unable to read current shotgun ammo count");
                case BackpackItem.TR1_UziAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1UziAmmoOffset:X}").TryGetUShort(out uziAmmo);
                    if (success)
                        return uziAmmo;
                    else
                        throw new Exception("Unable to read current uzi ammo count");
                case BackpackItem.TR1_MagnumAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1MagnumAmmoOffset:X}").TryGetUShort(out ushort magnumAmmo);
                    if (success)
                        return magnumAmmo;
                    else
                        throw new Exception("Unable to read current shotgun ammo count");
                case BackpackItem.TR2_ShotgunAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2ShotgunAmmoOffset:X}").TryGetUShort(out shotgunAmmo);
                    if (success)
                        return shotgunAmmo; //Shotgun ammo is stored in memory as multiples of 6
                    else
                        throw new Exception("Unable to read current shotgun ammo count");
                case BackpackItem.TR2_UziAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2UziAmmoOffset:X}").TryGetUShort(out uziAmmo);
                    if (success)
                        return uziAmmo;
                    else
                        throw new Exception("Unable to read current uzi ammo count");
                case BackpackItem.TR2_APAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2AutomaticPistolsAmmoOffset:X}").TryGetUShort(out ushort apAmmo);
                    if (success)
                        return apAmmo;
                    else
                        throw new Exception("Unable to read current auto pistol ammo count");
                case BackpackItem.TR2_M16Ammo:
                    success = AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2M16AmmoOffset:X}").TryGetUShort(out ushort m16Ammo);
                    if (success)
                        return m16Ammo;
                    else
                        throw new Exception("Unable to read current M16 ammo count");
                case BackpackItem.TR2_GrenadeAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2GrenadeLauncherAmmoOffset:X}").TryGetUShort(out grenadeAmmo);
                    if (success)
                        return grenadeAmmo;
                    else
                        throw new Exception("Unable to read current grenade ammo count");
                case BackpackItem.TR2_HarpoonAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2HarpoonGunAmmoOffset:X}").TryGetUShort(out harpoonAmmo);
                    if (success)
                        return harpoonAmmo;
                    else
                        throw new Exception("Unable to read current harpoon ammo count");
                case BackpackItem.TR3_ShotgunAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3ShotgunAmmoOffset:X}").TryGetUShort(out shotgunAmmo);
                    if (success)
                        return shotgunAmmo; //Shotgun ammo is stored in memory as multiples of 6
                    else
                        throw new Exception("Unable to read current shotgun ammo count");
                case BackpackItem.TR3_UziAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3UziAmmoOffset:X}").TryGetUShort(out uziAmmo);
                    if (success)
                        return uziAmmo;
                    else
                        throw new Exception("Unable to read current uzi ammo count");
                case BackpackItem.TR3_DesertEagleAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3DeagleAmmoOffset:X}").TryGetUShort(out ushort deagleAmmo);
                    if (success)
                        return deagleAmmo;
                    else
                        throw new Exception("Unable to read current desert eagle ammo count");
                case BackpackItem.TR3_MP5Ammo:
                    success = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3MP5AmmoOffset:X}").TryGetUShort(out ushort mp5Ammo);
                    if (success)
                        return mp5Ammo;
                    else
                        throw new Exception("Unable to read current MP5 ammo count");
                case BackpackItem.TR3_RocketAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3RocketLauncherAmmoOffset:X}").TryGetUShort(out ushort rocketAmmo);
                    if (success)
                        return rocketAmmo;
                    else
                        throw new Exception("Unable to read current rocket ammo count");
                case BackpackItem.TR3_GrenadeAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3GrenadeLauncherAmmoOffset:X}").TryGetUShort(out grenadeAmmo);
                    if (success)
                        return grenadeAmmo;
                    else
                        throw new Exception("Unable to read current grenade ammo count");
                case BackpackItem.TR3_HarpoonAmmo:
                    success = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3HarpoonGunAmmoOffset:X}").TryGetUShort(out harpoonAmmo);
                    if (success)
                        return harpoonAmmo;
                    else
                        throw new Exception("Unable to read current harpoon ammo count");
                default:
                    throw new Exception("Impossible state reached");
            }
        }

        private bool AdjustAmmoCount(BackpackItem ammoToEdit, uint ammoCountToEdit, bool increase)
        {
            int currentAmmo = GetAmmoCount(ammoToEdit);

            if ((increase && currentAmmo == 0x7FFF) || (!increase && currentAmmo == 0))
            {
                //ammo for this weapon is already full and we're trying to increase
                //or its empty and we're trying to decrease: delay effect
                return false;
            }

            AddressChain<InjectConnector> ammoMemory = ammoToEdit switch
            {
                BackpackItem.TR1_ShotgunAmmo => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1ShotgunAmmoOffset:X}"),
                BackpackItem.TR1_UziAmmo => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1UziAmmoOffset:X}"),
                BackpackItem.TR1_MagnumAmmo => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1MagnumAmmoOffset:X}"),
                BackpackItem.TR2_ShotgunAmmo => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2ShotgunAmmoOffset:X}"),
                BackpackItem.TR2_UziAmmo => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2UziAmmoOffset:X}"),
                BackpackItem.TR2_APAmmo => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2AutomaticPistolsAmmoOffset:X}"),
                BackpackItem.TR2_GrenadeAmmo => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2GrenadeLauncherAmmoOffset:X}"),
                BackpackItem.TR2_HarpoonAmmo => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2HarpoonGunAmmoOffset:X}"),
                BackpackItem.TR2_M16Ammo => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2M16AmmoOffset:X}"),
                BackpackItem.TR3_ShotgunAmmo => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3ShotgunAmmoOffset:X}"),
                BackpackItem.TR3_UziAmmo => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3UziAmmoOffset:X}"),
                BackpackItem.TR3_DesertEagleAmmo => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3DeagleAmmoOffset:X}"),
                BackpackItem.TR3_HarpoonAmmo => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3HarpoonGunAmmoOffset:X}"),
                BackpackItem.TR3_GrenadeAmmo => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3GrenadeLauncherAmmoOffset:X}"),
                BackpackItem.TR3_RocketAmmo => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3RocketLauncherAmmoOffset:X}"),
                BackpackItem.TR3_MP5Ammo => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3MP5AmmoOffset:X}"),
                _ => throw new Exception("Selected BackpackItem does not use ammo"),
            };
            if (increase)
            {
                if (ammoCountToEdit + currentAmmo < 0x7FFF)
                {
                    return ammoMemory.TrySetShort((short)(ammoCountToEdit + currentAmmo));
                }
                else
                {
                    return ammoMemory.TrySetShort(0x7FFF);
                }
            }
            else
            {
                if (currentAmmo - ammoCountToEdit < 0)
                {
                    return ammoMemory.TrySetShort(0);
                }
                else
                {
                    return ammoMemory.TrySetShort((short)(currentAmmo - ammoCountToEdit));
                }
            }
        }

        private bool AddToBackpack(CurrentGame currentGame, BackpackItem itemToChange, int backpackSize)
        {
            bool successEditingBackpackSize;
            string currentDll;
            ulong dllBase;
            uint backpackSizeOffset;
            List<uint> backpackSlotOffsets;
            List<BackpackItem> backpackOrder;
            List<BackpackItem> backpackItemsWithAmmo;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    backpackOrder = _tr1BackpackEntityOrder;
                    currentDll = _tomb1Dll;
                    dllBase = _tomb1DllBase;
                    backpackSizeOffset = _tr1BackpackSizeOffset;
                    backpackSlotOffsets = _tr1BackpackSlotOffsets;
                    backpackItemsWithAmmo = _tr1BackpackItemsWithAmmo;
                    break;
                case CurrentGame.TR2:
                    backpackOrder = _tr2BackpackEntityOrder;
                    currentDll = _tomb2Dll;
                    dllBase = _tomb2DllBase;
                    backpackSizeOffset = _tr2BackpackSizeOffset;
                    backpackSlotOffsets = _tr2BackpackSlotOffsets;
                    backpackItemsWithAmmo = _tr2BackpackItemsWithAmmo;
                    break;
                case CurrentGame.TR3:
                    backpackOrder = _tr3BackpackEntityOrder;
                    currentDll = _tomb3Dll;
                    dllBase = _tomb3DllBase;
                    backpackSizeOffset = _tr3BackpackSizeOffset;
                    backpackSlotOffsets = _tr3BackpackSlotOffsets;
                    backpackItemsWithAmmo = _tr3BackpackItemsWithAmmo;
                    break;
                default:
                    throw new Exception("Invalid hooked game detected");
            }

            //get the current list of items
            List<BackpackItem> currentBackpackOrder = [];
            for (int i = 0; i < backpackSize; i++)
            {
                ulong backpackSlotId = AddressChain.Parse(Connector, $"{currentDll}+{backpackSlotOffsets[i]:X}").GetULong();
                BackpackItem backpackItem = (BackpackItem)(backpackSlotId - dllBase);
                currentBackpackOrder.Add(backpackItem);
            }

            //if we already have the ammo, its super simple - just replace the ammo and do not increase backpack size.
            if (backpackItemsWithAmmo.Contains(itemToChange))
            {
                int exactIndex = currentBackpackOrder.IndexOf(GetAmmoTypeOfGun(itemToChange));
                if (exactIndex != -1)
                {
                    return AddressChain.Parse(Connector, $"{currentDll}+{backpackSlotOffsets[exactIndex]:X}").TrySetULong(dllBase + (ulong)itemToChange);
                }
            }

            //otherwise, find the correct location, add it, and bump everything that comes after
            successEditingBackpackSize = AddressChain.Parse(Connector, $"{currentDll}+{backpackSizeOffset:X}").TrySetByte((byte)(backpackSize + 1));
            if (successEditingBackpackSize)
            {
                //find where the item we're trying to add *should* be in the array
                int desiredIndex = backpackOrder.IndexOf(itemToChange);
                bool indexFound = false;
                int placementIndex = desiredIndex - 1;
                do
                {
                    BackpackItem precedingItem = backpackOrder[placementIndex];
                    Log.Debug($"Checking to see if {precedingItem} is in the backpack...");
                    int precedingIndex = currentBackpackOrder.IndexOf(precedingItem);
                    //If the preceding item is not present in the current backpack...
                    if (precedingIndex == -1)
                    {
                        //check to see if it has ammo
                        if (backpackItemsWithAmmo.Contains(precedingItem))
                        {
                            //if it does, check and see if the ammo is in our current backpack.
                            precedingIndex = currentBackpackOrder.IndexOf(GetAmmoTypeOfGun(precedingItem));
                            if (precedingIndex != -1)
                            {
                                //if it is, then we've found the preceding index we need
                                placementIndex = precedingIndex;
                                indexFound = true;
                                Log.Debug($"Placement index found at: {placementIndex}");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        //if it is, then we've found the preceding index we need
                        placementIndex = precedingIndex;
                        indexFound = true;
                        Log.Debug($"Placement index found at: {placementIndex}");
                        continue;
                    }
                    placementIndex--;
                } while (!indexFound);

                ulong previousSlotId = dllBase + (ulong)itemToChange;
                short previousSlotCount = 1;
                for (int i = placementIndex + 1; i <= backpackSize; i++)
                {
                    //Set slot count
                    AddressChain<InjectConnector> slotCountAddressChain = GetBackpackSlotCountAddress(currentGame, i);
                    short currentSlotCount = slotCountAddressChain.GetShort();
                    GetBackpackSlotCountAddress(currentGame, i).SetShort(previousSlotCount);
                    previousSlotCount = currentSlotCount;

                    //Set slot ID
                    AddressChain<InjectConnector> slotIdAddressChain = AddressChain.Parse(Connector, $"{currentDll}+{backpackSlotOffsets[i]:X}");
                    ulong slotId = slotIdAddressChain.GetULong();
                    AddressChain.Parse(Connector, $"{currentDll}+{backpackSlotOffsets[i]:X}").SetULong(previousSlotId);
                    previousSlotId = slotId;
                }
                return true;
            }
            else return false;
        }

        private bool RemoveFromBackpack(CurrentGame currentGame, BackpackItem itemToChange, int backpackSize, int backpackSlot)
        {
            string gameDll;
            uint backpackSizeOffset;
            List<uint> backpackSlotOffsets;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    gameDll = _tomb1Dll;
                    backpackSizeOffset = _tr1BackpackSizeOffset;
                    backpackSlotOffsets = _tr1BackpackSlotOffsets;
                    break;
                case CurrentGame.TR2:
                    gameDll = _tomb2Dll;
                    backpackSizeOffset = _tr2BackpackSizeOffset;
                    backpackSlotOffsets = _tr2BackpackSlotOffsets;
                    break;
                case CurrentGame.TR3:
                    gameDll = _tomb3Dll;
                    backpackSizeOffset = _tr3BackpackSizeOffset;
                    backpackSlotOffsets = _tr3BackpackSlotOffsets;
                    break;
                default:
                    throw new Exception("Invalid hooked game detected");
            }
            bool successEditingBackpackSize = AddressChain.Parse(Connector, $"{gameDll}+{backpackSizeOffset:X}").TrySetByte((byte)(backpackSize - 1));
            if (successEditingBackpackSize)
            {
                //move everything up one in slot counts (i becomes i+1, i+1 becomes i+2, etc) and the slot IDs
                //this appears to be what the native functionality is, so we want to mimic that
                for (int i = backpackSlot; i <= backpackSize - 2; i++)
                {
                    AddressChain<InjectConnector> slotCountAddressChain = GetBackpackSlotCountAddress(currentGame, i + 1);
                    short slotCount = slotCountAddressChain.GetShort();
                    GetBackpackSlotCountAddress(currentGame, i).SetShort(slotCount);

                    AddressChain<InjectConnector> slotIdAddressChain = AddressChain.Parse(Connector, $"{gameDll}+{backpackSlotOffsets[i + 1]:X}");
                    ulong slotId = slotIdAddressChain.GetULong();
                    AddressChain.Parse(Connector, $"{gameDll}+{backpackSlotOffsets[i]:X}").SetULong(slotId);
                }
                return true;
            }
            return false;
        }

        private AddressChain<InjectConnector> GetCurrentO2(CurrentGame currentGame, out int currentO2)
        {
            AddressChain<InjectConnector> o2Memory;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    o2Memory = AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1OxygenOffset:X}");
                    currentO2 = o2Memory.GetShort();
                    return o2Memory;
                case CurrentGame.TR2:
                    o2Memory = AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2OxygenOffset:X}");
                    currentO2 = o2Memory.GetShort();
                    return o2Memory;
                case CurrentGame.TR3:
                    o2Memory = AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3OxygenOffset:X}");
                    currentO2 = o2Memory.GetShort();
                    return o2Memory;
            }
            throw new Exception("Invalid hooked game detected");
        }

        private AddressChain<InjectConnector> GetCurrentHP(CurrentGame currentGame, out int currentHP)
        {
            string gameDll;
            uint laraOffset;

            switch (currentGame)
            {
                case CurrentGame.TR1:
                    gameDll = _tomb1Dll;
                    laraOffset = _tr1LaraOffset;
                    break;
                case CurrentGame.TR2:
                    gameDll = _tomb2Dll;
                    laraOffset = _tr2LaraOffset;
                    break;
                case CurrentGame.TR3:
                    gameDll = _tomb3Dll;
                    laraOffset = _tr3LaraOffset;
                    break;
                default:
                    throw new Exception("Invalid hooked game detected");
            }

            AddressChain<InjectConnector> hpMemory = AddressChain.Parse(Connector, $"{gameDll}+{laraOffset:X}");
            PointerAddressChain<InjectConnector> realMemory = hpMemory.Follow();
            AddressChain<InjectConnector> realestHpMemory = AddressChain.Parse(Connector, $"{realMemory.Address:X}+{_laraHealthOffset:X}");
            currentHP = realestHpMemory.GetShort();
            return realestHpMemory;

        }

        private bool AdjustO2(CurrentGame currentGame, uint amountToChange, bool increase)
        {
            if (LaraStateByte(currentGame) == 0)
                return false;

            AddressChain<InjectConnector> currentO2Address = GetCurrentO2(currentGame, out int currentO2);

            if (increase)
            {
                if ((amountToChange + currentO2) < _maxO2)
                {
                    return currentO2Address.TrySetShort((short)(amountToChange + currentO2));
                }
                else
                {
                    return currentO2Address.TrySetShort((short)_maxO2);
                }
            }
            else
            {
                if ((currentO2 - amountToChange) < 0)
                {
                    return currentO2Address.TrySetShort(0);
                }
                else
                {
                    return currentO2Address.TrySetShort((short)(currentO2 - amountToChange));
                }
            }
        }

        private bool SetCurrentHP(CurrentGame currentGame, uint amountToSet)
        {
            AddressChain<InjectConnector> currentHPAddress = GetCurrentHP(currentGame, out int currentHP);

            return currentHPAddress.TrySetShort((short)(amountToSet));
        }

        private bool AdjustHP(CurrentGame currentGame, uint amountToChange, bool increase)
        {
            AddressChain<InjectConnector> currentHPAddress = GetCurrentHP(currentGame, out int currentHP);
            _maxHP = (uint)GetCurrentMaxHP(currentGame);

            if (increase)
            {
                if (currentHP >= _maxHP)
                    return false;
                if ((amountToChange + currentHP) < _maxHP)
                {
                    return currentHPAddress.TrySetShort((short)(amountToChange + currentHP));
                }
                else
                {
                    return currentHPAddress.TrySetShort((short)_maxHP);
                }
            }
            else
            {
                if (currentHP <= 1)
                    return false;
                if ((currentHP - amountToChange) < 1)
                {
                    return currentHPAddress.TrySetShort(1);
                }
                else
                {
                    return currentHPAddress.TrySetShort((short)(currentHP - amountToChange));
                }
            }
        }

        private bool SetGraphics(bool classic)
        {
            _modifyingGraphics = true;
            AddressChain<InjectConnector> currentState = AddressChain.Parse(Connector, $"{_mainGame}+{_tr123GraphicsAndControlOffset:X}");
            byte state = currentState.GetByte();
            if (_wasOGGraphics == null)
            {
                _wasOGGraphics = state % 2 == 0;
            }
            if (classic)
            {
                if (state % 2 == 1)
                    return currentState.TrySetByte((byte)(state - 1));
                else
                    return currentState.TrySetByte(state);
            }
            else
            {
                if (state % 2 == 0)
                    return currentState.TrySetByte((byte)(state + 1));
                else
                    return currentState.TrySetByte(state);
            }
        }

        private bool SetControls(bool tank)
        {
            _modifyingControls = true;
            AddressChain<InjectConnector> currentState = AddressChain.Parse(Connector, $"{_mainGame}+{_tr123GraphicsAndControlOffset:X}");

            byte state = currentState.GetByte();
            BitArray bitArray = new(BitConverter.GetBytes((char)state));
            Log.Debug($"Current control scheme bit: {bitArray[1]}");
            if (_wasTankControls == null)
            {
                _wasTankControls = !bitArray[1];
            }
            if (tank)
            {
                if (bitArray[1] == true) //if bit #1 of this byte is 1(true), then we are in Modern controls
                    return currentState.TrySetByte((byte)(state - 2));
                else //otherwise, we are already on tank controls, so just keep it set
                    return currentState.TrySetByte(state);
            }
            else
            {
                if (bitArray[1] == false) //if bit #1 of this byte is 0(false), then we are in Tank controls
                    return currentState.TrySetByte((byte)(state + 2));
                else //otherwise, we are already on modern controls, so just keep it set
                    return currentState.TrySetByte(state);
            }
        }

        private bool InvisibleLara(CurrentGame currentGame, bool invisible)
        {
            SetGraphics(true);
            string gameDll;
            uint laraOffset;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    gameDll = _tomb1Dll;
                    laraOffset = _tr1LaraOffset;
                    break;
                case CurrentGame.TR2:
                    gameDll = _tomb2Dll;
                    laraOffset = _tr2LaraOffset;
                    break;
                case CurrentGame.TR3:
                    gameDll = _tomb3Dll;
                    laraOffset = _tr3LaraOffset;
                    break;
                default:
                    throw new Exception("Unknown game hooked into");
            }
            PointerAddressChain<InjectConnector> invisibilityPointerChain = AddressChain.Parse(Connector, $"{gameDll}+{laraOffset:X}").Follow();

            AddressChain<InjectConnector> visibilityByte = AddressChain.Parse(Connector, $"{invisibilityPointerChain.Address:X}+{_laraVisibilityOffset:X}");

            ushort currentVisibility = visibilityByte.GetUShort();

            if (invisible)
            {
                if (currentVisibility == 0xFFFF)
                {
                    return visibilityByte.TrySetShort(0);
                }
                else if (currentVisibility == 0)
                    return true;
            }
            else
            {
                if (currentVisibility == 0x0)
                {
                    return visibilityByte.TrySetUShort(0xFFFF);
                }
                else if (currentVisibility == 0xFFFF)
                    return true;
            }

            return false;
        }

        private bool DarkLara(CurrentGame currentGame, bool darkLara)
        {
            SetGraphics(true);
            string gameDll;
            uint laraOffset;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    gameDll = _tomb1Dll;
                    laraOffset = _tr1LaraOffset;
                    break;
                case CurrentGame.TR2:
                    gameDll = _tomb2Dll;
                    laraOffset = _tr2LaraOffset;
                    break;
                case CurrentGame.TR3:
                    gameDll = _tomb3Dll;
                    laraOffset = _tr3LaraOffset;
                    break;
                default:
                    throw new Exception("Unknown game hooked into");
            }
            PointerAddressChain<InjectConnector> invisibilityPointerChain = AddressChain.Parse(Connector, $"{gameDll}+{laraOffset:X}").Follow();

            AddressChain<InjectConnector> visibilityByte = AddressChain.Parse(Connector, $"{invisibilityPointerChain.Address:X}+{_laraLightingOffset:X}");

            byte currentVisibility = visibilityByte.GetByte();

            if (darkLara)
            {
                if (currentVisibility == 0xFF)
                {
                    return visibilityByte.TrySetByte(0x7F);
                }
                else if (currentVisibility == 0x7F)
                    return true;
            }
            else
            {
                if (currentVisibility == 0x7F)
                {
                    return visibilityByte.TrySetByte(0xFF);
                }
                else if (currentVisibility == 0xFF)
                    return true;
            }

            return false;
        }

        private bool PoisonLara(CurrentGame currentGame)
        {
            GetCurrentHP(currentGame, out int currentHp);
            if (_hpBeforePoison == uint.MaxValue)
            {
                _hpBeforePoison = (uint)currentHp;
                _laraIsPoisoned = true;
                return AdjustHP(currentGame, 10, false);
            }

            if (_hpBeforePoison <= currentHp && _laraIsPoisoned)
            {
                //player has healed/died and reloaded, poison is gone now
                _laraIsPoisoned = false;
                return false;
            }

            return AdjustHP(currentGame, 10, false);
        }

        private byte GetRoomCount(CurrentGame currentGame)
        {
            return currentGame switch
            {
                CurrentGame.TR1 => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1RoomCountOffset:X}").GetByte(),
                CurrentGame.TR2 => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2RoomCountOffset:X}").GetByte(),
                CurrentGame.TR3 => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3RoomCountOffset:X}").GetByte(),
                _ => throw new Exception("Invalid hooked game detected"),
            };
        }

        private bool GetFloodState(PointerAddressChain<InjectConnector> roomPointer, int roomNumber, out byte floodState)
        {
            return AddressChain.Parse(Connector, $"{roomPointer:X}+{(roomNumber * _roomSize):X}+{_floodStateOffset:X}").TryGetByte(out floodState);
        }

        private bool SetFloodState(PointerAddressChain<InjectConnector> roomPointer, int roomNumber, byte flood)
        {
            return AddressChain.Parse(Connector, $"{roomPointer:X}+{(roomNumber * _roomSize):X}+{_floodStateOffset:X}").TrySetByte(flood);
        }

        private void FloodLevel(CurrentGame currentGame, bool causeFlood)
        {
            byte roomCount = GetRoomCount(currentGame);
            string currentDll;
            uint roomPtrOffset;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    currentDll = _tomb1Dll;
                    roomPtrOffset = _tr1RoomPtrOffset;
                    break;
                case CurrentGame.TR2:
                    currentDll = _tomb2Dll;
                    roomPtrOffset = _tr2RoomPtrOffset;
                    break;
                case CurrentGame.TR3:
                    currentDll = _tomb3Dll;
                    roomPtrOffset = _tr3RoomPtrOffset;
                    break;
                default:
                    throw new Exception("Invalid hooked game detected");
            }
            PointerAddressChain<InjectConnector> roomPointer = AddressChain.Parse(Connector, $"{currentDll}+{roomPtrOffset:X}").Follow();
            if (causeFlood)
            {
                if (_floodedRoomStateOriginal.Count == 0)
                {
                    //effect is started, or we're in a new level and flooding again
                    for (int i = 0; i < roomCount; i++)
                    {
                        bool success = GetFloodState(roomPointer, i, out byte floodState);
                        if (success)
                        {
                            _floodedRoomStateOriginal.Add(i, floodState);
                        }
                        else
                        {
                            return;
                        }
                    }

                    //if we have gotten all original states for all rooms successfully, then we can safely flood the rooms
                    for (int i = 0; i < _floodedRoomStateOriginal.Count; i++)
                    {
                        bool success = SetFloodState(roomPointer, i, 1);
                        if (!success)
                        {
                            //if ANY room fails, abort and reverse flooding process
                            FloodLevel(currentGame, false);
                            return;
                        }
                    }
                }
                else if (_floodedRoomStateOriginal.Count == roomCount)
                {
                    //we're still in the same level as before
                    for (int i = 0; i < _floodedRoomStateOriginal.Count; i++)
                    {
                        bool success = GetFloodState(roomPointer, i, out byte floodState);
                        if (success)
                        {
                            if (floodState == 0)
                            {
                                SetFloodState(roomPointer, i, 1);
                            }
                        }
                    }
                }
                else
                {
                    //we're in a new level, stop trying to flood the previous level
                    _floodedRoomStateOriginal.Clear();
                }
            }
            else
            {
                for (int i = 0; i < _floodedRoomStateOriginal.Count; i++)
                {
                    SetFloodState(roomPointer, i, _floodedRoomStateOriginal[i]);
                }
                //done flooding, and we're returned to the original now: clear the original state
                _floodedRoomStateOriginal.Clear();
            }
        }

        private bool ForceMovement(int movement)
        {
            _forcingMovement = true;
            return AddressChain.Parse(Connector, $"{_mainGame}+{_tr123MovementByte:X}").TrySetByte((byte)movement);
        }

        private bool ForceEquip(CurrentGame currentGame, int weaponToEquip)
        {
            return currentGame switch
            {
                CurrentGame.TR1 => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1EquippedWeapon:X}").TrySetByte((byte)weaponToEquip),
                CurrentGame.TR2 => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2EquippedWeapon:X}").TrySetByte((byte)weaponToEquip),
                CurrentGame.TR3 => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3EquippedWeapon:X}").TrySetByte((byte)weaponToEquip),
                _ => throw new Exception("Invalid hooked game detected"),
            };
        }

        private bool ChangeOutfit(int outfit)
        {
            AddressChain<InjectConnector> outfitByte = AddressChain.Parse(Connector, $"{_mainGame}+{_tr123OutfitOffset:X}");
            if (outfitByte.GetByte() == (byte)outfit)
                return false;
            return outfitByte.TrySetByte((byte)outfit);
        }

        private bool EquipSunglasses(bool putOn)
        {
            byte sunnies = putOn ? (byte)1 : (byte)0;
            AddressChain<InjectConnector> sunniesByte = AddressChain.Parse(Connector, $"{_mainGame}+{_tr123SunniesOffset:X}");
            if (sunniesByte.GetByte() == sunnies)
                return false;
            return sunniesByte.TrySetByte(sunnies);
        }

        private byte LaraStateByte(CurrentGame currentGame)
        {
            return currentGame switch
            {
                CurrentGame.TR1 => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1LaraStateByte:X}").GetByte(),
                CurrentGame.TR2 => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2LaraStateByte:X}").GetByte(),
                CurrentGame.TR3 => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3LaraStateByte:X}").GetByte(),
                _ => throw new Exception("Invalid hooked game detected")
            };
        }

        private PointerAddressChain<InjectConnector> GetLaraPointer(CurrentGame currentGame)
        {
            return currentGame switch
            {
                CurrentGame.TR1 => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1LaraOffset:X}").Follow(),
                CurrentGame.TR2 => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2LaraOffset:X}").Follow(),
                CurrentGame.TR3 => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3LaraOffset:X}").Follow(),
                _ => throw new Exception("Invalid hooked game detected"),
            };
        }

        private bool AdjustLaraYPosition(CurrentGame currentGame, int amountToChange)
        {
            PointerAddressChain<InjectConnector> laraPointer = GetLaraPointer(currentGame);
            OffsetAddressChain<InjectConnector> yPosChain = laraPointer.Offset(_laraYPosOffset);
            bool success = yPosChain.TryGetInt(out int yPos);
            if (success)
            {
                return yPosChain.TrySetInt(yPos + amountToChange);
            }
            return false;
        }

        private short GetCurrentMaxHP(CurrentGame currentGame)
        {
            return currentGame switch
            {
                CurrentGame.TR1 => tr1MaxHPDeclarations[0].Offset(1).GetShort(),
                CurrentGame.TR2 => tr2MaxHPDeclarations[0].Offset(1).GetShort(),
                CurrentGame.TR3 => tr3MaxHPDeclarations[0].Offset(1).GetShort(),
                _ => throw new Exception("Invalid hooked game detected"),
            };
        }

        private short GetCurrentMaxO2(CurrentGame currentGame)
        {
            return currentGame switch
            {
                CurrentGame.TR1 => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1MaxO2Offset:X}").GetShort(),
                CurrentGame.TR2 => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2InitialMaxO2Offset:X}").GetShort(),
                CurrentGame.TR3 => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3InitialMaxO2Offset:X}").GetShort(),
                _ => throw new Exception("Invalid hooked game detected"),
            };
        }

        private byte GetCurrentMaxStamina()
        {
            return AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3MaxStaminaOffset:X}").GetByte();
        }

        private bool SetMaxHP(CurrentGame currentGame, short maxHP, bool decreasing = false)
        {
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    foreach (var maxHPDeclaration in tr1MaxHPDeclarations)
                    {
                        bool success = maxHPDeclaration.Offset(1).TrySetShort(maxHP);
                        if (decreasing)
                            success = SetCurrentHP(currentGame, (uint)maxHP);
                        if (!success)
                            return false;
                    }
                    return true;
                case CurrentGame.TR2:
                    foreach (var maxHPDeclaration in tr2MaxHPDeclarations)
                    {
                        bool success = maxHPDeclaration.Offset(1).TrySetShort(maxHP);
                        if (decreasing)
                            success = SetCurrentHP(currentGame, (uint)maxHP);
                        if (!success)
                            return false;
                    }
                    return true;
                case CurrentGame.TR3:
                    foreach (var maxHPDeclaration in tr3MaxHPDeclarations)
                    {
                        bool success = maxHPDeclaration.Offset(1).TrySetShort(maxHP);
                        if (decreasing)
                            success = SetCurrentHP(currentGame, (uint)maxHP);
                        if (!success)
                            return false;
                    }
                    return true;
            }
            throw new Exception("Invalid hooked game detected");
        }

        private bool SetMaxO2(CurrentGame currentGame, short maxO2)
        {
            short currentMaxO2;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    return AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1MaxO2Offset:X}").TrySetShort(maxO2);
                case CurrentGame.TR2:
                    currentMaxO2 = GetCurrentMaxO2(currentGame);
                    if (AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2InitialMaxO2Offset:X}").TrySetShort(maxO2))
                        return AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2RegainedMaxO2Offset:X}").TrySetShort(maxO2);
                    return false;
                case CurrentGame.TR3:
                    currentMaxO2 = GetCurrentMaxO2(currentGame);
                    if (AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3InitialMaxO2Offset:X}").TrySetShort(maxO2))
                        return AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3RegainedMaxO2Offset:X}").TrySetShort(maxO2);
                    return false;
            }
            throw new Exception("Invalid hooked game detected");
        }

        private bool SetMaxStamina(byte maxStamina)
        {
            _affectingStamina = true;
            return AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3MaxStaminaOffset:X}").TrySetByte(maxStamina);
        }

        private bool AdjustMaxHP(CurrentGame currentGame, uint amountToChange, bool increase)
        {
            short currentMaxHP = GetCurrentMaxHP(currentGame);
            _maxHP = (uint)currentMaxHP;
            Log.Debug($"Current MaxHP: {currentMaxHP}");
            short desiredMaxHP = (short)(currentMaxHP + amountToChange * (increase ? 1 : -1));
            Log.Debug($"Desired MaxHP: {desiredMaxHP}");
            if (increase)
            {
                if (currentMaxHP == 0x7FFF)
                    return false;
                if (desiredMaxHP < 0x7FFF)
                {
                    return SetMaxHP(currentGame, desiredMaxHP);
                }
                else
                {
                    return SetMaxHP(currentGame, 0x7FFF);
                }
            }
            else
            {
                if (currentMaxHP == 1)
                    return false;
                if (desiredMaxHP > 1)
                {
                    return SetMaxHP(currentGame, desiredMaxHP, true);
                }
                else
                {
                    return SetMaxHP(currentGame, 1, true);
                }
            }
        }

        private bool AdjustMaxO2(CurrentGame currentGame, uint amountToChange, bool increase)
        {
            short currentMaxO2 = GetCurrentMaxO2(currentGame);
            short desiredMaxO2 = (short)(currentMaxO2 + amountToChange * (increase ? 1 : -1));

            if (increase)
            {
                if (currentMaxO2 == 0x7FFF)
                    return false;
                if (desiredMaxO2 < 0x7FFF)
                {
                    return SetMaxO2(currentGame, desiredMaxO2);
                }
                else
                {
                    return SetMaxO2(currentGame, 0x7FFF);
                }
            }
            else
            {
                if (currentMaxO2 == 1)
                    return false;
                if (desiredMaxO2 > 1)
                {
                    return SetMaxO2(currentGame, desiredMaxO2);
                }
                else
                {
                    return SetMaxO2(currentGame, 1);
                }
            }
        }

        private bool AdjustMaxStamina(byte amountToSet)
        {
            byte currentMaxStamina = GetCurrentMaxStamina();

            if (currentMaxStamina > amountToSet)
            {
                if (currentMaxStamina == 0x7F)
                    return false;
                if (amountToSet < 0x7F)
                {
                    return SetMaxStamina(amountToSet);
                }
                else
                {
                    return SetMaxStamina(0x7F);
                }
            }
            else
            {
                if (currentMaxStamina == 0)
                    return false;
                if (amountToSet >= 1)
                {
                    if (SetMaxStamina(amountToSet))
                    {
                        if (AdjustCurrentStamina(amountToSet))
                            return true;
                        else
                            SetMaxStamina(currentMaxStamina);
                    }
                }
                else
                {
                    if (SetMaxStamina(0))
                    {
                        if (AdjustCurrentStamina(0))
                            return true;
                        else
                            SetMaxStamina(currentMaxStamina);
                    }
                }
            }
            return false;
        }

        private bool AdjustCurrentStamina(uint amountToSet)
        {
            return AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3CurrentStaminaOffset:X}").TrySetShort((short)amountToSet);
        }

        private byte CurrentTR1WeaponDamage(BackpackItem weapon)
        {
            return weapon switch
            {
                BackpackItem.TR1_Pistols => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1PistolDamageOffset:X}").GetByte(),
                BackpackItem.TR1_Magnums => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1MagnumDamageOffset:X}").GetByte(),
                BackpackItem.TR1_Uzis => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1UziDamageOffset:X}").GetByte(),
                BackpackItem.TR1_Shotgun => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1ShotgunDamageOffset:X}").GetByte(),
                _ => throw new Exception("Invalid weapon"),
            };
        }

        private bool SetWeaponDamage(BackpackItem weapon, byte desiredDamage)
        {
            return weapon switch
            {
                BackpackItem.TR1_Pistols => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1PistolDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR1_Magnums => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1MagnumDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR1_Uzis => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1UziDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR1_Shotgun => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1ShotgunDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR2_Pistols => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2PistolDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR2_AutomaticPistols => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2AutomaticPistolsDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR2_Uzis => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2UziDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR2_Shotgun => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2ShotgunDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR2_M16 => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2M16DamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR2_HarpoonGun => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2HarpoonGunDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR2_GrenadeLauncher => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2GrenadeLauncherDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR3_Pistols => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3PistolDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR3_DesertEagle => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3DeagleDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR3_Uzis => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3UziDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR3_Shotgun => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3ShotgunDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR3_MP5 => AddressChain.Parse(Connector, $"{_tomb3Dll}+{+_tr3MP5DamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR3_HarpoonGun => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3HarpoonGunDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR3_GrenadeLauncher => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3GrenadeLauncherDamageOffset:X}").TrySetByte(desiredDamage),
                BackpackItem.TR3_RocketLauncher => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3RocketLauncherDamageOffset:X}").TrySetByte(desiredDamage),
                _ => throw new Exception("Invalid weapon selected, cannot change the damage it deals"),
            };
        }

        private bool AdjustPistolDamage(CurrentGame currentGame, bool? increase)
        {
            var pistols = currentGame switch
            {
                CurrentGame.TR1 => BackpackItem.TR1_Pistols,
                CurrentGame.TR2 => BackpackItem.TR2_Pistols,
                CurrentGame.TR3 => BackpackItem.TR3_Pistols,
                _ => throw new Exception("Unknown hooked game detected"),
            };
            if (increase == true)
            {
                _affectingPistolDamage = true;
                return SetWeaponDamage(pistols, 2);
            }
            else if (increase == false)
            {
                _affectingPistolDamage = true;
                return SetWeaponDamage(pistols, 0);
            }
            else
            {
                _affectingPistolDamage = false;
                return SetWeaponDamage(pistols, 1);
            }
        }

        private bool AdjustTR1MagnumDamage(bool? increase)
        {
            if (increase == true)
            {
                _affectingMagnumDamage = true;
                return SetWeaponDamage(BackpackItem.TR1_Magnums, 4);
            }
            else if (increase == false)
            {
                _affectingMagnumDamage = true;
                return SetWeaponDamage(BackpackItem.TR1_Magnums, 0);
            }
            else
            {
                _affectingMagnumDamage = false;
                return SetWeaponDamage(BackpackItem.TR1_Magnums, 2);
            }
        }

        private bool AdjustTR2AutoPistolDamage(bool? increase)
        {
            if (increase == true)
            {
                _affectingAutoPistolDamage = true;
                return SetWeaponDamage(BackpackItem.TR2_AutomaticPistols, 4);
            }
            else if (increase == false)
            {
                _affectingAutoPistolDamage = true;
                return SetWeaponDamage(BackpackItem.TR2_AutomaticPistols, 0);
            }
            else
            {
                _affectingAutoPistolDamage = false;
                return SetWeaponDamage(BackpackItem.TR2_AutomaticPistols, 2);
            }
        }

        private bool AdjustTR2M16Damage(bool? increase)
        {
            if (increase == true)
            {
                _affectingM16Damage = true;
                return SetWeaponDamage(BackpackItem.TR2_M16, 6);
            }
            else if (increase == false)
            {
                _affectingM16Damage = true;
                return SetWeaponDamage(BackpackItem.TR2_M16, 0);
            }
            else
            {
                _affectingM16Damage = false;
                return SetWeaponDamage(BackpackItem.TR2_M16, 3);
            }
        }

        private bool AdjustHarpoonDamage(CurrentGame currentGame, bool? increase)
        {
            BackpackItem harpoonGun;
            byte damage;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    throw new Exception("TR1 does not have a Harpoon Gun, cannot adjust damage");
                default:
                    throw new Exception("Unknown hooked game detected");
                case CurrentGame.TR2:
                    harpoonGun = BackpackItem.TR2_HarpoonGun;
                    damage = 4;
                    break;
                case CurrentGame.TR3:
                    harpoonGun = BackpackItem.TR3_HarpoonGun;
                    damage = 6;
                    break;

            }
            if (increase == true)
            {
                _affectingHarpoonDamage = true;
                return SetWeaponDamage(harpoonGun, (byte)(damage * 2));
            }
            else if (increase == false)
            {
                _affectingHarpoonDamage = true;
                return SetWeaponDamage(harpoonGun, 0);
            }
            else
            {
                _affectingHarpoonDamage = false;
                return SetWeaponDamage(harpoonGun, damage);
            }
        }

        private bool AdjustGrenadeLauncherDamage(CurrentGame currentGame, bool? increase)
        {
            BackpackItem grenadeLauncher;
            byte damage;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    throw new Exception("TR1 does not have a Grenade Launcher, cannot adjust damage");
                default:
                    throw new Exception("Unknown hooked game detected");
                case CurrentGame.TR2:
                    grenadeLauncher = BackpackItem.TR2_GrenadeLauncher;
                    damage = 0x1E;
                    break;
                case CurrentGame.TR3:
                    grenadeLauncher = BackpackItem.TR3_GrenadeLauncher;
                    damage = 0x14;
                    break;

            }
            if (increase == true)
            {
                _affectingHarpoonDamage = true;
                return SetWeaponDamage(grenadeLauncher, (byte)(damage * 2));
            }
            else if (increase == false)
            {
                _affectingHarpoonDamage = true;
                return SetWeaponDamage(grenadeLauncher, 0);
            }
            else
            {
                _affectingHarpoonDamage = false;
                return SetWeaponDamage(grenadeLauncher, damage);
            }
        }

        private bool AdjustTR3RocketLauncherDamage(bool? increase)
        {
            if (increase == true)
            {
                _affectingRocketDamage = true;
                return SetWeaponDamage(BackpackItem.TR3_RocketLauncher, 0x3C);
            }
            else if (increase == false)
            {
                _affectingRocketDamage = true;
                return SetWeaponDamage(BackpackItem.TR3_RocketLauncher, 0);
            }
            else
            {
                _affectingRocketDamage = false;
                return SetWeaponDamage(BackpackItem.TR3_RocketLauncher, 0x1E);
            }
        }

        private bool AdjustTR3MP5Damage(bool? increase)
        {
            if (increase == true)
            {
                _affectingMP5Damage = true;
                return SetWeaponDamage(BackpackItem.TR3_MP5, 0x8);
            }
            else if (increase == false)
            {
                _affectingMP5Damage = true;
                return SetWeaponDamage(BackpackItem.TR3_MP5, 0);
            }
            else
            {
                _affectingMP5Damage = false;
                return SetWeaponDamage(BackpackItem.TR3_MP5, 0x4);
            }
        }

        private bool AdjustTR3DesertEagleDamage(bool? increase)
        {
            if (increase == true)
            {
                _affectingDesertEagleDamage = true;
                return SetWeaponDamage(BackpackItem.TR3_DesertEagle, 0x30);
            }
            else if (increase == false)
            {
                _affectingDesertEagleDamage = true;
                return SetWeaponDamage(BackpackItem.TR3_DesertEagle, 0);
            }
            else
            {
                _affectingDesertEagleDamage = false;
                return SetWeaponDamage(BackpackItem.TR3_DesertEagle, 0x15);
            }
        }

        private bool AdjustUziDamage(CurrentGame currentGame, bool? increase)
        {
            var uzis = currentGame switch
            {
                CurrentGame.TR1 => BackpackItem.TR1_Uzis,
                CurrentGame.TR2 => BackpackItem.TR2_Uzis,
                CurrentGame.TR3 => BackpackItem.TR3_Uzis,
                _ => throw new Exception("Unknown hooked game detected"),
            };
            if (increase == true)
            {
                _affectingUziDamage = true;
                return SetWeaponDamage(uzis, 2);
            }
            else if (increase == false)
            {
                _affectingUziDamage = true;
                return SetWeaponDamage(uzis, 0);
            }
            else
            {
                _affectingUziDamage = false;
                return SetWeaponDamage(uzis, 1);
            }
        }

        private bool AdjustShotgunDamage(CurrentGame currentGame, bool? increase)
        {
            BackpackItem shotgun;
            byte damage = 3;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    shotgun = BackpackItem.TR1_Shotgun;
                    damage = 4;
                    break;
                case CurrentGame.TR2:
                    shotgun = BackpackItem.TR2_Shotgun;
                    break;
                case CurrentGame.TR3:
                    shotgun = BackpackItem.TR3_Shotgun;
                    break;
                default:
                    throw new Exception("Unknown hooked game detected");
            }
            if (increase == true)
            {
                _affectingShotgunDamage = true;
                return SetWeaponDamage(shotgun, (byte)(damage * 2));
            }
            else if (increase == false)
            {
                _affectingShotgunDamage = true;
                return SetWeaponDamage(shotgun, 0);
            }
            else
            {
                _affectingShotgunDamage = false;
                return SetWeaponDamage(shotgun, damage);
            }
        }

        private bool SetFallDamage(CurrentGame currentGame, byte damageAmount)
        {
            return currentGame switch
            {
                CurrentGame.TR1 => AddressChain.Parse(Connector, $"{_tomb1Dll}+{_tr1FallDamageOffset:X}").TrySetByte(damageAmount),
                CurrentGame.TR2 => AddressChain.Parse(Connector, $"{_tomb2Dll}+{_tr2FallDamageOffset:X}").TrySetByte(damageAmount),
                CurrentGame.TR3 => AddressChain.Parse(Connector, $"{_tomb3Dll}+{_tr3FallDamageOffset:X}").TrySetByte(damageAmount),
                _ => throw new Exception("Unknown hooked game detected"),
            };
        }

        private bool AdjustFallDamage(CurrentGame currentGame, bool? increase)
        {
            if (increase == true)
            {
                _affectingFallDamage = true;
                return SetFallDamage(currentGame, 12);
            }
            else if (increase == false)
            {
                _affectingFallDamage = true;
                return SetFallDamage(currentGame, 3);
            }
            else
            {
                _affectingFallDamage = false;
                return SetFallDamage(currentGame, 6);
            }
        }

        private bool RestartLevel(CurrentGame currentGame)
        {
            string gameDll;
            uint currentLevelOffset;
            uint completedFlagOffset;
            switch (currentGame)
            {
                case CurrentGame.TR1:
                    gameDll = _tomb1Dll;
                    currentLevelOffset = _tr1CurrentLevelOffset;
                    completedFlagOffset = _tr1LevelCompletedFlagOffset;
                    break;
                case CurrentGame.TR2:
                    gameDll = _tomb2Dll;
                    currentLevelOffset = _tr2CurrentLevelOffset;
                    completedFlagOffset = _tr2LevelCompletedFlagOffset;
                    break;
                case CurrentGame.TR3:
                    gameDll = _tomb3Dll;
                    currentLevelOffset = _tr3CurrentLevelOffset;
                    completedFlagOffset = _tr3LevelCompletedFlagOffset;
                    break;
                default:
                    throw new Exception("Invalid game detected when trying to restart level");
            }
            AddressChain<InjectConnector> currentLevelMemAddr = AddressChain.Parse(Connector, $"{gameDll}+{currentLevelOffset:X}");
            bool success = currentLevelMemAddr.TryGetByte(out byte currentLevel);
            if (success)
            {
                success = currentLevelMemAddr.TrySetByte((byte)(currentLevel - 1));
                if (success)
                {
                    AddressChain<InjectConnector> levelCompletedMemAddr = AddressChain.Parse(Connector, $"{gameDll}+{completedFlagOffset:X}");
                    return levelCompletedMemAddr.TrySetByte(1);
                }
            }
            return false;
        }
        //private CurrentGame currentGame = CurrentGame.TR1;
        protected override void StartEffect(EffectRequest request)
        {
            string[] codeParams = FinalCode(request).Split("_");
            CurrentGame currentGame = DetermineCurrentGame();
            UpdateEffectMenuStatus(currentGame);
            BackpackItem backpackItem;

            switch (Enum.Parse(typeof(GameEffect), codeParams[0]))
            {
                #region All games
                #region Graphics/Controls
                case GameEffect.forceClassicGraphics:
                    if (GetGameState() == GameState.WrongMode || _modifyingGraphics == true)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan classicGraphicsDuration = request.Duration;
                    EffectState classicState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced classic graphics for {classicGraphicsDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            SetGraphics(true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    classicState.WhenCompleted.Then(() =>
                    {
                        if (_wasOGGraphics != null)
                            SetGraphics((bool)_wasOGGraphics);
                        _wasOGGraphics = null;
                        _modifyingGraphics = null;
                    });

                    break;
                case GameEffect.forceRemasterGraphics:
                    if (GetGameState() == GameState.WrongMode || _modifyingGraphics == true)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan remasterGraphicsDuration = request.Duration;
                    EffectState remasterState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced remastered graphics for {remasterGraphicsDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            SetGraphics(false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    remasterState.WhenCompleted.Then(() =>
                    {
                        if (_wasOGGraphics != null)
                            SetGraphics((bool)_wasOGGraphics);
                        _wasOGGraphics = null;
                        _modifyingGraphics = null;
                    });
                    break;
                case GameEffect.forceTankControls:
                    if (GetGameState() == GameState.WrongMode || _modifyingControls)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan tankControlsDuration = request.Duration;
                    EffectState tankState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced tank controls for {tankControlsDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            SetControls(true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    tankState.WhenCompleted.Then(() =>
                    {
                        if (_wasTankControls != null)
                            SetControls((bool)_wasTankControls);
                        _wasTankControls = null;
                        _modifyingControls = false;
                    });
                    break;
                case GameEffect.forceModernControls:
                    if (GetGameState() == GameState.WrongMode || _modifyingControls)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan modernControlsDuration = request.Duration;
                    EffectState modernState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced modern controls for {modernControlsDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(500),
                        () =>
                        {
                            SetControls(false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(500),
                        false);
                    modernState.WhenCompleted.Then(() =>
                    {
                        if (_wasTankControls != null)
                            SetControls((bool)_wasTankControls);
                        _wasTankControls = null;
                        _modifyingControls = false;
                    });
                    break;
                case GameEffect.forceMoveForward:
                    if (GetGameState() == GameState.WrongMode || _forcingMovement || LaraStateByte((CurrentGame)_lastReportedGame) != 0)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan moveForwardDuration = request.Duration;
                    EffectState moveForwardState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced forward movement for {moveForwardDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            ForceMovement(01);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    moveForwardState.WhenCompleted.Then(() =>
                    {
                        ForceMovement(0);
                        _forcingMovement = false;
                    });
                    break;
                case GameEffect.forceMoveBackward:
                    if (GetGameState() == GameState.WrongMode || _forcingMovement || LaraStateByte((CurrentGame)_lastReportedGame) != 0)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan moveBackwardDuration = request.Duration;
                    EffectState moveBackwardState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced backward movement for {moveBackwardDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            ForceMovement(02);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    moveBackwardState.WhenCompleted.Then(() =>
                    {
                        ForceMovement(0);
                        _forcingMovement = false;
                    });
                    break;
                case GameEffect.forceJump:
                    if (GetGameState() == GameState.WrongMode || _forcingMovement || LaraStateByte((CurrentGame)_lastReportedGame) != 0)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan jumpDuration = request.Duration;
                    EffectState jumpState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced jump for {jumpDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            ForceMovement(16);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    jumpState.WhenCompleted.Then(() =>
                    {
                        ForceMovement(0);
                        _forcingMovement = false;
                    });
                    break;
                case GameEffect.forceWalk:
                    if (GetGameState() == GameState.WrongMode || _forcingMovement || LaraStateByte((CurrentGame)_lastReportedGame) != 0)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan walkDuration = request.Duration;
                    EffectState forceWalkState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced jump for {walkDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            ForceMovement(128);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    forceWalkState.WhenCompleted.Then(() =>
                    {
                        ForceMovement(0);
                        _forcingMovement = false;
                    });
                    break;
                case GameEffect.forceSwanDive:
                    if (GetGameState() == GameState.WrongMode || _forcingMovement || LaraStateByte((CurrentGame)_lastReportedGame) != 0)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.WrongMode);
                        return;
                    }
                    SITimeSpan diveDuration = request.Duration;
                    EffectState forceDiveState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced swan dive for {diveDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            ForceMovement(145);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    forceDiveState.WhenCompleted.Then(() =>
                    {
                        ForceMovement(0);
                        _forcingMovement = false;
                    });
                    break;
                #endregion
                #region Cosmetics
                case GameEffect.putOnSunglasses:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => EquipSunglasses(true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her sunglasses"));
                    break;
                case GameEffect.takeOffSunglasses:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => EquipSunglasses(false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took Lara's sunglasses D:"));
                    break;
                case GameEffect.classic1Outfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(1),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR1 classic outfit"));
                    break;
                case GameEffect.training1Outfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(2),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR1 training outfit"));
                    break;
                case GameEffect.classic2Outfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(3),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR2 classic outfit"));
                    break;
                case GameEffect.training2Outfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(4),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR2 training outfit"));
                    break;
                case GameEffect.wetsuitOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(5),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR2 wetsuit outfit"));
                    break;
                case GameEffect.bomberOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(6),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR2 bomber outfit"));
                    break;
                case GameEffect.bathrobeOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(7),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR2 bathrobe outfit"));
                    break;
                case GameEffect.training3Outfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(8),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her training TR3 outfit"));
                    break;
                case GameEffect.nevadaOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(9),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR3 Nevada outfit"));
                    break;
                case GameEffect.pacificOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(10),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR3 Pacific outfit"));
                    break;
                case GameEffect.catsuitOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(11),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR3 catsuit outfit"));
                    break;
                case GameEffect.antarcticaOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(12),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her TR3 Antarctica outfit"));
                    break;
                case GameEffect.bloodyClassicOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(13),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her bloody classic outfit"));
                    break;
                case GameEffect.vegasOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(14),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her Vegas outfit"));
                    break;
                case GameEffect.ogLaraOutfit:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => ChangeOutfit(15),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her OG outfit"));
                    break;
                #endregion
                #endregion

                #region TR1 specific
                case GameEffect.giveShotgun:
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR1 => BackpackItem.TR1_Shotgun,
                        CurrentGame.TR2 => BackpackItem.TR2_Shotgun,
                        CurrentGame.TR3 => BackpackItem.TR3_Shotgun,
                        _ => throw new Exception("Invalid hooked game detected"),
                    };
                    if (GetGameState() == GameState.WrongMode)
                    {
                        Log.Error("Bad game state, delaying effect");
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, backpackItem, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her shotgun"));
                    break;

                case GameEffect.takeShotgun:
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR1 => BackpackItem.TR1_Shotgun,
                        CurrentGame.TR2 => BackpackItem.TR2_Shotgun,
                        CurrentGame.TR3 => BackpackItem.TR3_Shotgun,
                        _ => throw new Exception("Invalid hooked game detected"),
                    };
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, backpackItem, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's shotgun"));
                    break;

                case GameEffect.giveUzis:
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR1 => BackpackItem.TR1_Uzis,
                        CurrentGame.TR2 => BackpackItem.TR2_Uzis,
                        CurrentGame.TR3 => BackpackItem.TR3_Uzis,
                        _ => throw new Exception("Invalid hooked game detected"),
                    };
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, backpackItem, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her Uzis"));
                    break;

                case GameEffect.takeUzis:
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR1 => BackpackItem.TR1_Uzis,
                        CurrentGame.TR2 => BackpackItem.TR2_Uzis,
                        CurrentGame.TR3 => BackpackItem.TR3_Uzis,
                        _ => throw new Exception("Invalid hooked game detected"),
                    };
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, backpackItem, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's Uzis"));
                    break;

                case GameEffect.tr1GiveMagnums:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR1)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR1_Magnums, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her Magnums"));
                    break;

                case GameEffect.tr1TakeMagnums:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR1)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR1_Magnums, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's Magnums"));
                    break;

                case GameEffect.giveAutoPistols:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR2_AutomaticPistols, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her Automatic Pistols"));
                    break;

                case GameEffect.takeAutoPistols:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR2_AutomaticPistols, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's Automatic Pistols"));
                    break;

                case GameEffect.giveM16:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR2_M16, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her M16"));
                    break;

                case GameEffect.takeM16:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR2_M16, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's M16"));
                    break;

                case GameEffect.giveDesertEagle:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR3_DesertEagle, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her Desert Eagle"));
                    break;

                case GameEffect.takeDesertEagle:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR3_DesertEagle, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's Desert Eagle"));
                    break;

                case GameEffect.giveMP5:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR3_MP5, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her MP5"));
                    break;

                case GameEffect.takeMP5:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR3_MP5, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's MP5"));
                    break;

                case GameEffect.giveRocketLauncher:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR3_RocketLauncher, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her Rocket Launcher"));
                    break;

                case GameEffect.takeRocketLauncher:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, BackpackItem.TR3_RocketLauncher, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's Rocket Launcher"));
                    break;

                case GameEffect.giveGrenadeLauncher:
                    switch (currentGame)
                    {
                        case CurrentGame.TR2:
                            backpackItem = BackpackItem.TR2_GrenadeLauncher;
                            break;
                        case CurrentGame.TR3:
                            backpackItem = BackpackItem.TR3_GrenadeLauncher;
                            break;
                        default:
                            DelayEffect(request, StandardErrors.BadGameState, GameState.BadGameSettings);
                            return;
                    }
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, backpackItem, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her Grenade Launcher"));
                    break;

                case GameEffect.takeGrenadeLauncher:
                    switch (currentGame)
                    {
                        case CurrentGame.TR2:
                            backpackItem = BackpackItem.TR2_GrenadeLauncher;
                            break;
                        case CurrentGame.TR3:
                            backpackItem = BackpackItem.TR3_GrenadeLauncher;
                            break;
                        default:
                            DelayEffect(request, StandardErrors.BadGameState, GameState.BadGameSettings);
                            return;
                    }
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, backpackItem, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's Grenade Launcher"));
                    break;

                case GameEffect.giveHarpoonGun:
                    switch (currentGame)
                    {
                        case CurrentGame.TR2:
                            backpackItem = BackpackItem.TR2_HarpoonGun;
                            break;
                        case CurrentGame.TR3:
                            backpackItem = BackpackItem.TR3_HarpoonGun;
                            break;
                        default:
                            DelayEffect(request, StandardErrors.BadGameState, GameState.BadGameSettings);
                            return;
                    }
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, backpackItem, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara her Harpoon Gun"));
                    break;

                case GameEffect.takeHarpoonGun:
                    switch (currentGame)
                    {
                        case CurrentGame.TR2:
                            backpackItem = BackpackItem.TR2_HarpoonGun;
                            break;
                        case CurrentGame.TR3:
                            backpackItem = BackpackItem.TR3_HarpoonGun;
                            break;
                        default:
                            DelayEffect(request, StandardErrors.BadGameState, GameState.BadGameSettings);
                            return;
                    }
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustBackpackStatus(currentGame, backpackItem, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took away Lara's Harpoon Gun"));
                    break;

                case GameEffect.forceUnequip:
                    if (GetGameState() == GameState.WrongMode || _forcingUnequip)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan unequipDuration = request.Duration;
                    EffectState forceUnequipState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} forced Lara to unequip all weapons for {unequipDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            ForceEquip(currentGame, 0);
                            _forcingUnequip = true;
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    forceUnequipState.WhenCompleted.Then(() =>
                    {
                        ForceEquip(currentGame, 1);
                        _forcingUnequip = false;
                    });
                    break;

                case GameEffect.giveLargeMedi:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveLargeMediCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustLargeMedipackCount(currentGame, giveLargeMediCount, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveLargeMediCount} Large Medi Packs to Lara's backpack"));
                    break;

                case GameEffect.giveSmallMedi:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveSmallMediCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustSmallMedipackCount(currentGame, giveSmallMediCount, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveSmallMediCount} Small Medi Packs to Lara's backpack"));
                    break;

                case GameEffect.giveFlare:
                    if (GetGameState() == GameState.WrongMode || currentGame == CurrentGame.TR1)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveFlareCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustFlareCount(currentGame, giveFlareCount, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveFlareCount} flares to Lara's backpack"));
                    break;

                case GameEffect.takeFlare:
                    if (GetGameState() == GameState.WrongMode || currentGame == CurrentGame.TR1)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeFlareCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustFlareCount(currentGame, takeFlareCount, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took {takeFlareCount} flares from Lara's backpack"));
                    break;


                case GameEffect.takeLargeMedi:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeLargeMediCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustLargeMedipackCount(currentGame, takeLargeMediCount, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeLargeMediCount} Large Medi Packs from Lara's backpack"));
                    break;

                case GameEffect.takeSmallMedi:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeSmallMediCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustSmallMedipackCount(currentGame, takeSmallMediCount, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeSmallMediCount} Small Medi Packs from Lara's backpack"));
                    break;

                case GameEffect.giveShotgunAmmo:
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR1 => BackpackItem.TR1_ShotgunAmmo,
                        CurrentGame.TR2 => BackpackItem.TR2_ShotgunAmmo,
                        CurrentGame.TR3 => BackpackItem.TR3_ShotgunAmmo,
                        _ => throw new Exception("Invalid hooked game detected"),
                    };
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveShotgunAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(backpackItem, giveShotgunAmmoCount * 12, true), //+6 in memory == +1 shell
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveShotgunAmmoCount * 2} shotgun shells to Lara's backpack"));
                    break;

                case GameEffect.takeShotgunAmmo:
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR1 => BackpackItem.TR1_ShotgunAmmo,
                        CurrentGame.TR2 => BackpackItem.TR2_ShotgunAmmo,
                        CurrentGame.TR3 => BackpackItem.TR3_ShotgunAmmo,
                        _ => throw new Exception("Invalid hooked game detected"),
                    };
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeShotgunAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(backpackItem, takeShotgunAmmoCount * 12, false), //-6 in memory == -1 shell
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeShotgunAmmoCount * 2} shotgun shells from Lara's backpack"));
                    break;

                case GameEffect.giveUziAmmo:
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR1 => BackpackItem.TR1_UziAmmo,
                        CurrentGame.TR2 => BackpackItem.TR2_UziAmmo,
                        CurrentGame.TR3 => BackpackItem.TR3_UziAmmo,
                        _ => throw new Exception("Invalid hooked game detected"),
                    };
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveUziAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(backpackItem, giveUziAmmoCount * 50, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveUziAmmoCount * 50} Uzi bullets to Lara's backpack"));
                    break;

                case GameEffect.takeUziAmmo:
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR1 => BackpackItem.TR1_UziAmmo,
                        CurrentGame.TR2 => BackpackItem.TR2_UziAmmo,
                        CurrentGame.TR3 => BackpackItem.TR3_UziAmmo,
                        _ => throw new Exception("Invalid hooked game detected"),
                    };
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeUziAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(backpackItem, takeUziAmmoCount * 50, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeUziAmmoCount * 50} Uzi bullets from Lara's backpack"));
                    break;

                case GameEffect.tr1GiveMagnumAmmo:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveMagnumAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR1_MagnumAmmo, giveMagnumAmmoCount * 25, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveMagnumAmmoCount * 25} Magnum bullets to Lara's backpack"));
                    break;

                case GameEffect.tr1TakeMagnumAmmo:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeMagnumAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR1_MagnumAmmo, takeMagnumAmmoCount * 25, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeMagnumAmmoCount * 25} Magnum bullets from Lara's backpack"));
                    break;

                case GameEffect.giveAutoPistolAmmo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveAutoPistolAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR2_APAmmo, giveAutoPistolAmmoCount * 25, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveAutoPistolAmmoCount * 25} Automatic Pistol bullets to Lara's backpack"));
                    break;

                case GameEffect.takeAutoPistolAmmo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeAutoPistolAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR2_APAmmo, takeAutoPistolAmmoCount * 25, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeAutoPistolAmmoCount * 25} Automatic Pistol bullets from Lara's backpack"));
                    break;

                case GameEffect.giveM16Ammo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveM16AmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR2_M16Ammo, giveM16AmmoCount * 30, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveM16AmmoCount * 30} M16 bullets to Lara's backpack"));
                    break;

                case GameEffect.takeM16Ammo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeM16AmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR2_M16Ammo, takeM16AmmoCount * 30, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeM16AmmoCount * 30} M16 bullets from Lara's backpack"));
                    break;

                case GameEffect.giveMP5Ammo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveMP5AmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR3_MP5Ammo, giveMP5AmmoCount * 30, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveMP5AmmoCount * 30} MP5 bullets to Lara's backpack"));
                    break;

                case GameEffect.takeMP5Ammo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeMP5AmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR3_MP5Ammo, takeMP5AmmoCount * 30, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeMP5AmmoCount * 30} MP5 bullets from Lara's backpack"));
                    break;

                case GameEffect.giveDeagleAmmo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveDeagleAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR3_DesertEagleAmmo, giveDeagleAmmoCount * 7, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveDeagleAmmoCount * 7} Desert Eagle bullets to Lara's backpack"));
                    break;

                case GameEffect.takeDeagleAmmo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeDeagleAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR3_DesertEagleAmmo, takeDeagleAmmoCount * 7, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeDeagleAmmoCount * 7} Desert Eagle bullets from Lara's backpack"));
                    break;

                case GameEffect.giveRocketAmmo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveRocketAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR3_RocketAmmo, giveRocketAmmoCount * 2, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveRocketAmmoCount * 2} Rockets to Lara's backpack"));
                    break;

                case GameEffect.takeRocketAmmo:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeRocketAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(BackpackItem.TR3_RocketAmmo, takeRocketAmmoCount * 2, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeRocketAmmoCount * 2} Rockets from Lara's backpack"));
                    break;

                case GameEffect.giveHarpoonAmmo:
                    if (GetGameState() == GameState.WrongMode || (currentGame != CurrentGame.TR2 && currentGame != CurrentGame.TR3))
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveHarpoonAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR2 => BackpackItem.TR2_HarpoonAmmo,
                        CurrentGame.TR3 => BackpackItem.TR3_HarpoonAmmo,
                        _ => throw new Exception("Invalid game detected, cannot edit Harpoon ammo for this game"),
                    };
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(backpackItem, giveHarpoonAmmoCount * 4, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveHarpoonAmmoCount * 4} Harpoons to Lara's backpack"));
                    break;

                case GameEffect.takeHarpoonAmmo:
                    if (GetGameState() == GameState.WrongMode || (currentGame != CurrentGame.TR2 && currentGame != CurrentGame.TR3))
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeHarpoonAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR2 => BackpackItem.TR2_HarpoonAmmo,
                        CurrentGame.TR3 => BackpackItem.TR3_HarpoonAmmo,
                        _ => throw new Exception("Invalid game detected, cannot edit Harpoon ammo for this game"),
                    };
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(backpackItem, takeHarpoonAmmoCount * 4, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took {takeHarpoonAmmoCount * 4} Harpoons from Lara's backpack"));
                    break;

                case GameEffect.giveGrenadeAmmo:
                    if (GetGameState() == GameState.WrongMode || (currentGame != CurrentGame.TR2 && currentGame != CurrentGame.TR3))
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveGrenadeAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR2 => BackpackItem.TR2_GrenadeAmmo,
                        CurrentGame.TR3 => BackpackItem.TR3_GrenadeAmmo,
                        _ => throw new Exception("Invalid game detected, cannot edit Grenade ammo for this game"),
                    };
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(backpackItem, giveGrenadeAmmoCount * 2, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} added {giveGrenadeAmmoCount * 2} Grenades to Lara's backpack"));
                    break;

                case GameEffect.takeGrenadeAmmo:
                    if (GetGameState() == GameState.WrongMode || (currentGame != CurrentGame.TR2 && currentGame != CurrentGame.TR3))
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint takeGrenadeAmmoCount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    backpackItem = currentGame switch
                    {
                        CurrentGame.TR2 => BackpackItem.TR2_GrenadeAmmo,
                        CurrentGame.TR3 => BackpackItem.TR3_GrenadeAmmo,
                        _ => throw new Exception("Invalid game detected, cannot edit Grenade ammo for this game"),
                    };
                    TryEffect(request,
                        () => true,
                        () => AdjustAmmoCount(backpackItem, takeGrenadeAmmoCount * 2, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} took {takeGrenadeAmmoCount * 2} Grenades from Lara's backpack"));
                    break;

                case GameEffect.giveO2:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint giveO2Count))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustO2(currentGame, giveO2Count * 180, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} put {giveO2Count * 180} oxygen into Lara's lungs"));
                    break;

                case GameEffect.takeO2:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }

                    if (!uint.TryParse(codeParams[1], out uint takeO2Count))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustO2(currentGame, takeO2Count * 180, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} removed {takeO2Count * 180} oxygen from Lara's lungs D:"));
                    break;

                case GameEffect.healLara:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint healAmount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustHP(currentGame, healAmount * 100, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara {healAmount * 100} HP"));
                    break;

                case GameEffect.hurtLara:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint hurtAmount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustHP(currentGame, hurtAmount * 100, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} hurt Lara for {hurtAmount * 100} HP"));
                    break;

                case GameEffect.invisibleLara:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan invisibleDuration = request.Duration;
                    EffectState invisibleState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} made Lara invisible for {invisibleDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            InvisibleLara(currentGame, true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    invisibleState.WhenCompleted.Then(() =>
                    {
                        InvisibleLara(currentGame, false);
                        if (_wasOGGraphics != null)
                            SetGraphics((bool)_wasOGGraphics);
                        _wasOGGraphics = null;
                    });
                    break;

                case GameEffect.darkLara:
                    if (GetGameState() == GameState.WrongMode || currentGame == CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan darkDuration = request.Duration;
                    EffectState darkState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} brought about Dark Lara for {darkDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            DarkLara(currentGame, true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    darkState.WhenCompleted.Then(() =>
                    {
                        DarkLara(currentGame, false);
                        if (_wasOGGraphics != null)
                            SetGraphics((bool)_wasOGGraphics);
                        _wasOGGraphics = null;
                    });
                    break;

                case GameEffect.poisonLara:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan poisonDuration = request.Duration;
                    EffectState poisonState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} poisoned Lara for {poisonDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            if (!PoisonLara(currentGame))
                                this.CancelTimed(request);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(900),
                        false);
                    break;

                case GameEffect.floodLevel:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan floodDuration = request.Duration;
                    EffectState floodState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} flooded the level for {floodDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(100),
                        () =>
                        {
                            FloodLevel(currentGame, true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(900),
                        false);
                    floodState.WhenCompleted.Then(() =>
                    {
                        FloodLevel(currentGame, false);
                    });
                    break;

                case GameEffect.superJump:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => AdjustLaraYPosition(currentGame, -0xA00),
                        () => Connector.SendMessage($"{request.DisplayViewer} \"super jumped\" Lara"));
                    break;

                case GameEffect.increaseMaxHP:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint increaseMaxHPAmount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    increaseMaxHPAmount *= 100;
                    TryEffect(request,
                        () => true,
                        () => AdjustMaxHP(currentGame, increaseMaxHPAmount, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} increased Lara's Max HP by {increaseMaxHPAmount}"));
                    break;

                case GameEffect.decreaseMaxHP:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint decreaseMaxHPAmount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    decreaseMaxHPAmount *= 100;
                    TryEffect(request,
                        () => true,
                        () => AdjustMaxHP(currentGame, decreaseMaxHPAmount, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} decreased Lara's Max HP by {decreaseMaxHPAmount}"));
                    break;

                case GameEffect.increaseMaxO2:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint increaseMaxO2Amount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    increaseMaxO2Amount *= 180;
                    TryEffect(request,
                        () => true,
                        () => AdjustMaxO2(currentGame, increaseMaxO2Amount, true),
                        () => Connector.SendMessage($"{request.DisplayViewer} increased Lara's Max O2 by {increaseMaxO2Amount}"));
                    break;

                case GameEffect.decreaseMaxO2:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    if (!uint.TryParse(codeParams[1], out uint decreaseMaxO2Amount))
                    {
                        Respond(request, EffectStatus.FailTemporary, StandardErrors.CannotParseNumber, codeParams[1]);
                        break;
                    }
                    decreaseMaxO2Amount *= 180;
                    TryEffect(request,
                        () => true,
                        () => AdjustMaxO2(currentGame, decreaseMaxO2Amount, false),
                        () => Connector.SendMessage($"{request.DisplayViewer} decreased Lara's Max O2 by {decreaseMaxO2Amount}"));
                    break;

                case GameEffect.disableStamina:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3 || _affectingStamina)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableStaminaDuration = request.Duration;
                    EffectState disableStaminaState = RepeatAction(request,
                        () => true,
                        () =>
                        {
                            Connector.SendMessage($"{request.DisplayViewer} disabled Lara from sprinting for {disableStaminaDuration} seconds");
                            return AdjustCurrentStamina(0);
                        },
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {

                            return SetMaxStamina(0);
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableStaminaState.WhenCompleted.Then(() =>
                    {
                        SetMaxStamina(_defaultMaxStamina);
                        _affectingStamina = false;
                    });
                    break;

                case GameEffect.halfStamina:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3 || _affectingStamina)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan halfStaminaDuration = request.Duration;
                    EffectState halfStaminaState = RepeatAction(request,
                        () => true,
                        () =>
                        {
                            Connector.SendMessage($"{request.DisplayViewer} halved Lara's max stamina for {halfStaminaDuration} seconds");
                            return AdjustCurrentStamina(0x3F);
                        },
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            return SetMaxStamina(0x3F);
                        },
                        TimeSpan.FromMilliseconds(100),
                        false);
                    halfStaminaState.WhenCompleted.Then(() =>
                    {
                        SetMaxStamina(_defaultMaxStamina);
                        _affectingStamina = false;
                    });
                    break;

                case GameEffect.infiniteStamina:
                    if (GetGameState() == GameState.WrongMode || currentGame != CurrentGame.TR3 || _affectingStamina)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan infiniteStaminaDuration = request.Duration;
                    EffectState infiniteStaminaState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} gave Lara infinite stamina for {infiniteStaminaDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(10),
                        () =>
                        {
                            _affectingStamina = true;
                            return AdjustCurrentStamina(0x7F);
                        },
                        TimeSpan.FromMilliseconds(10),
                        false);
                    infiniteStaminaState.WhenCompleted.Then(() =>
                    {
                        _affectingStamina = false;
                    });
                    break;

                case GameEffect.doublePistolDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingPistolDamage)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doublePistolDmgDuration = request.Duration;
                    EffectState doublePistolDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Pistols do for {doublePistolDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustPistolDamage(currentGame, true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doublePistolDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustPistolDamage(currentGame, null);
                    });
                    break;

                case GameEffect.disablePistolDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingPistolDamage)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disablePistolDmgDuration = request.Duration;
                    EffectState disablePistolDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Pistols do for {disablePistolDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustPistolDamage(currentGame, false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disablePistolDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustPistolDamage(currentGame, null);
                    });
                    break;

                case GameEffect.tr1DoubleMagnumDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingMagnumDamage || currentGame != CurrentGame.TR1)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleMagnumDmgDuration = request.Duration;
                    EffectState doubleMagnumDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} disabled the damage Lara's Magnums do for {doubleMagnumDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR1MagnumDamage(true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleMagnumDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR1MagnumDamage(null);
                    });
                    break;

                case GameEffect.tr1DisableMagnumDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingMagnumDamage || currentGame != CurrentGame.TR1)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableMagnumDmgDuration = request.Duration;
                    EffectState disableMagnumDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} disabled the damage Lara's Magnums do for {disableMagnumDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR1MagnumDamage(false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableMagnumDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR1MagnumDamage(null);
                    });
                    break;

                case GameEffect.doubleM16Damage:
                    if (GetGameState() == GameState.WrongMode || _affectingM16Damage || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleM16DmgDuration = request.Duration;
                    EffectState doubleM16DmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's M16 does for {doubleM16DmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR2M16Damage(true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleM16DmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR2M16Damage(null);
                    });
                    break;

                case GameEffect.disableM16Damage:
                    if (GetGameState() == GameState.WrongMode || _affectingM16Damage || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableM16DmgDuration = request.Duration;
                    EffectState disableM16DmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} disabled the damage Lara's M16 does for {disableM16DmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR2M16Damage(false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableM16DmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR2M16Damage(null);
                    });
                    break;

                case GameEffect.doubleAutoPistolsDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingAutoPistolDamage || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleAutoPistolsDmgDuration = request.Duration;
                    EffectState doubleAutoPistolsDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Automatic Pistols do for {doubleAutoPistolsDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR2AutoPistolDamage(true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleAutoPistolsDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR2AutoPistolDamage(null);
                    });
                    break;

                case GameEffect.disableAutoPistolsDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingAutoPistolDamage || currentGame != CurrentGame.TR2)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableAutoPistolsDmgDuration = request.Duration;
                    EffectState disableAutoPistolsDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} disabled the damage Lara's Automatic Pistols do for {disableAutoPistolsDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR2AutoPistolDamage(false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableAutoPistolsDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR2AutoPistolDamage(null);
                    });
                    break;

                case GameEffect.doubleMP5Damage:
                    if (GetGameState() == GameState.WrongMode || _affectingMP5Damage || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleMP5DmgDuration = request.Duration;
                    EffectState doubleMP5DmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's MP5 does for {doubleMP5DmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR3MP5Damage(true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleMP5DmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR3MP5Damage(null);
                    });
                    break;

                case GameEffect.disableMP5Damage:
                    if (GetGameState() == GameState.WrongMode || _affectingMP5Damage || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableMP5DmgDuration = request.Duration;
                    EffectState disableMP5DmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} disabled the damage Lara's MP5 does for {disableMP5DmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR3MP5Damage(false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableMP5DmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR3MP5Damage(null);
                    });
                    break;

                case GameEffect.doubleDeagleDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingDesertEagleDamage || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleDeagleDmgDuration = request.Duration;
                    EffectState doubleDeagleDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Desert Eagle does for {doubleDeagleDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR3DesertEagleDamage(true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleDeagleDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR3DesertEagleDamage(null);
                    });
                    break;

                case GameEffect.disableDeagleDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingDesertEagleDamage || currentGame != CurrentGame.TR3)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableDeagleDmgDuration = request.Duration;
                    EffectState disableDeagleDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} disabled the damage Lara's Desert Eagle does for {disableDeagleDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustTR3DesertEagleDamage(false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableDeagleDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustTR3DesertEagleDamage(null);
                    });
                    break;

                case GameEffect.doubleHarpoonDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingHarpoonDamage || currentGame == CurrentGame.TR1)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleHarpoonDmgDuration = request.Duration;
                    EffectState doubleHarpoonDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Harpoon does for {doubleHarpoonDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustHarpoonDamage(currentGame, true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleHarpoonDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustHarpoonDamage(currentGame, null);
                    });
                    break;

                case GameEffect.disableHarpoonDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingHarpoonDamage || currentGame == CurrentGame.TR1)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableHarpoonDmgDuration = request.Duration;
                    EffectState disableHarpoonDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} disabled the damage Lara's Harpoon does for {disableHarpoonDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustHarpoonDamage(currentGame, false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableHarpoonDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustHarpoonDamage(currentGame, null);
                    });
                    break;

                case GameEffect.doubleUziDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingUziDamage)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleUziDmgDuration = request.Duration;
                    EffectState doubleUziDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Uzis do for {doubleUziDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustUziDamage(currentGame, true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleUziDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustUziDamage(currentGame, null);
                    });
                    break;

                case GameEffect.disableUziDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingUziDamage)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableUziDmgDuration = request.Duration;
                    EffectState disableUziDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Uzis do for {disableUziDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustUziDamage(currentGame, false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableUziDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustUziDamage(currentGame, null);
                    });
                    break;

                case GameEffect.doubleShotgunDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingShotgunDamage)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleShotgunDmgDuration = request.Duration;
                    EffectState doubleShotgunDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Shotgun does for {doubleShotgunDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustShotgunDamage(currentGame, true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleShotgunDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustShotgunDamage(currentGame, null);
                    });
                    break;

                case GameEffect.disableShotgunDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingShotgunDamage)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan disableShotgunDmgDuration = request.Duration;
                    EffectState disableShotgunDmgState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara's Shotgun does for {disableShotgunDmgDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustShotgunDamage(currentGame, false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    disableShotgunDmgState.WhenCompleted.Then(() =>
                    {
                        AdjustShotgunDamage(currentGame, null);
                    });
                    break;

                case GameEffect.halfFallDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingFallDamage)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan halfFallDamageDuration = request.Duration;
                    EffectState halfFallDamageState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} halved the damage Lara takes from falling for {halfFallDamageDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustFallDamage(currentGame, false);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    halfFallDamageState.WhenCompleted.Then(() =>
                    {
                        AdjustFallDamage(currentGame, null);
                    });
                    break;

                case GameEffect.doubleFallDamage:
                    if (GetGameState() == GameState.WrongMode || _affectingFallDamage)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    SITimeSpan doubleFallDamageDuration = request.Duration;
                    EffectState doubleFallDamageState = RepeatAction(request,
                        () => true,
                        () => Connector.SendMessage($"{request.DisplayViewer} doubled the damage Lara takes from falling for {doubleFallDamageDuration} seconds"),
                        TimeSpan.Zero,
                        () => IsReady(request),
                        TimeSpan.FromMilliseconds(1000),
                        () =>
                        {
                            AdjustFallDamage(currentGame, true);
                            return true;
                        },
                        TimeSpan.FromMilliseconds(1000),
                        false);
                    doubleFallDamageState.WhenCompleted.Then(() =>
                    {
                        AdjustFallDamage(currentGame, null);
                    });
                    break;

                case GameEffect.restartLevel:
                    if (GetGameState() == GameState.WrongMode)
                    {
                        DelayEffect(request, StandardErrors.BadGameState, GameState.BadPlayerState);
                        return;
                    }
                    TryEffect(request,
                        () => true,
                        () => RestartLevel(currentGame),
                        () => Connector.SendMessage($"{request.DisplayViewer} restarted the current level"));
                    break;
                #endregion
                default:
                    Log.Message($"Unsupported effect: {codeParams[0]}");
                    Respond(request, EffectStatus.FailPermanent, StandardErrors.EffectUnknown, request);
                    break;
            }
        }

        private bool IsPausedOrMenu()
        {
            bool success = AddressChain.Parse(Connector, $"{_mainGame}+{_tr123InMenuBoolean:X}").TryGetByte(out byte menuState);
            if (!success)
                return false;
            Log.Debug($"Menu state is: {menuState}");
            if (menuState == 0)
                return false;
            return true;
        }

        private bool IsLevelCompleteScreen()
        {
            string currentGame;
            uint levelCompletedFlag;
            switch (_lastReportedGame)
            {
                case CurrentGame.TR1:
                    currentGame = _tomb1Dll;
                    levelCompletedFlag = _tr1LevelCompletedFlagOffset;
                    break;
                case CurrentGame.TR2:
                    currentGame = _tomb2Dll;
                    levelCompletedFlag = _tr2LevelCompletedFlagOffset;
                    break;
                case CurrentGame.TR3:
                    currentGame = _tomb3Dll;
                    levelCompletedFlag = _tr3LevelCompletedFlagOffset;
                    break;
                default:
                    return true;
            }
            bool success = AddressChain.Parse(Connector, $"{currentGame}+{levelCompletedFlag:X}").TryGetByte(out byte menuState);
            if (!success)
                return false;
            Log.Debug($"Menu state is: {menuState}");
            if (menuState == 0)
                return false;
            return true;
        }

    }
}
