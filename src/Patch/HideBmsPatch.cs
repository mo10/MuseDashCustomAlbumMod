﻿using HarmonyLib;
using Newtonsoft.Json.Linq;
using IL2CppJson = Il2CppNewtonsoft.Json.Linq;
using Il2CppSystem.Collections.Generic;
using Assets.Scripts.Database;
using Assets.Scripts.Database.DataClass;
using static Assets.Scripts.Database.DBConfigCustomTags;
using CustomAlbums.Data;
using System;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.UI.Controls;
using UnhollowerBaseLib;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.UI.Tips;
using Assets.Scripts.UI.Panels;
using PeroPeroGames.GlobalDefines;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Adds custom albums with hidden charts to the list
    /// </summary>
    [HarmonyPatch(typeof(SpecialSongManager), "InitHideBmsInfoDic")]
    internal static class HideBmsInfoDicPatch {
        private static bool runOnce;

        private static void Postfix(SpecialSongManager __instance) {
            if(!runOnce) {
                var albumList = new List<string>();

                foreach(var album in AlbumManager.LoadedAlbums) {
                    if(album.Value.availableMaps.ContainsKey(4)) {
                        __instance.m_HideBmsInfos.Add($"{AlbumManager.Uid}-{album.Value.Index}",
                        new SpecialSongManager.HideBmsInfo(
                            $"{AlbumManager.Uid}-{album.Value.Index}",
                            album.Value.Info.hideBmsDifficulty == 0 ? (album.Value.availableMaps.ContainsKey(3) ? 3 : 2) : album.Value.Info.hideBmsDifficulty,
                            4,
                            $"{album.Value.Name}_map4",
                            (Il2CppSystem.Func<bool>)delegate { return __instance.IsInvokeHideBms($"{AlbumManager.Uid}-{album.Value.Index}"); }
                        ));

                        // Add chart to the appropriate list for their hidden type
                        switch(album.Value.Info.GetHideBmsMode()) {
                            case AlbumInfo.HideBmsMode.CLICK:
                                var newClickArr = new Il2CppStringArray(__instance.m_ClickHideUids.Length + 1);
                                for(int i = 0; i < __instance.m_ClickHideUids.Length; i++) newClickArr[i] = __instance.m_ClickHideUids[i];
                                newClickArr[newClickArr.Length - 1] = ($"{AlbumManager.Uid}-{album.Value.Index}");
                                __instance.m_ClickHideUids = newClickArr;
                                break;
                            case AlbumInfo.HideBmsMode.PRESS:
                                var newPressArr = new Il2CppStringArray(__instance.m_LongPressHideUids.Length + 1);
                                for(int i = 0; i < __instance.m_LongPressHideUids.Length; i++) newPressArr[i] = __instance.m_LongPressHideUids[i];
                                newPressArr[newPressArr.Length - 1] = ($"{AlbumManager.Uid}-{album.Value.Index}");
                                __instance.m_LongPressHideUids = newPressArr;
                                break;
                            case AlbumInfo.HideBmsMode.TOGGLE:
                                var newToggleArr = new Il2CppStringArray(__instance.m_ToggleChangedHideUids.Length + 1);
                                for(int i = 0; i < __instance.m_ToggleChangedHideUids.Length; i++) newToggleArr[i] = __instance.m_ToggleChangedHideUids[i];
                                newToggleArr[newToggleArr.Length - 1] = ($"{AlbumManager.Uid}-{album.Value.Index}");
                                __instance.m_ToggleChangedHideUids = newToggleArr;
                                break;
                            default:
                                break;
                        }

                        // Add chart to the "With Hidden Chart" tag
                        /*albumList.Add($"{AlbumManager.Uid}-{album.Value.Index}");

                        var newArr = new Il2CppStringArray(DBMusicTagDefine.s_HiddenLocal.Length + 1);
                        for(int i = 0; i < DBMusicTagDefine.s_HiddenLocal.Count; i++) {
                            newArr[i] = DBMusicTagDefine.s_HiddenLocal[i];
                        }
                        newArr[newArr.Length - 1] = $"{AlbumManager.Uid}-{album.Value.Index}";
                        DBMusicTagDefine.s_HiddenLocal = newArr;*/
                    }
                }

                //var tagInfo = GlobalDataBase.dbMusicTag.GetAlbumTagInfo(32776);
                //tagInfo.AddTagUids(albumList);

                // This may run multiple times, but creates data which can only be generated once
                runOnce = true;
            }
        }
    }

    /// <summary>
    /// Activates hidden charts when the conditions are met
    /// </summary>
    [HarmonyPatch(typeof(SpecialSongManager), "InvokeHideBms")]
    internal static class InvokeHideBmsPatch {
        private static bool Prefix(DBConfigALBUM.MusicInfo musicInfo, SpecialSongManager __instance) {
            if(musicInfo.uid.StartsWith(AlbumManager.Uid.ToString()) && __instance.m_HideBmsInfos.ContainsKey(musicInfo.uid)) {
                var hideBms = __instance.m_HideBmsInfos[musicInfo.uid];
                __instance.m_IsInvokeHideDic[hideBms.uid] = true;

                if(hideBms.extraCondition.Invoke()) {
                    var album = AlbumManager.LoadedAlbums[AlbumManager.GetAlbumKeyByIndex(musicInfo.musicIndex)];

                    ActivateHidden(hideBms);

                    var msgBox = PnlTipsManager.instance.GetMessageBox("PnlSpecialsBmsAsk");
                    msgBox.Show("TIPS", album.Info.hideBmsMessage);
                    SpecialSongManager.onTriggerHideBmsEvent?.Invoke();
                    if(album.Info.GetHideBmsMode() == AlbumInfo.HideBmsMode.PRESS) Singleton<EventManager>.instance.Invoke("UI/OnSpecialsMusic", null);
                }
                return false;
            }
            return true;
        }

        private static bool ActivateHidden(SpecialSongManager.HideBmsInfo hideBms) {
            if(hideBms == null) return false;

            var info = GlobalDataBase.dbMusicTag.GetMusicInfoFromAll(hideBms.uid);
            var success = false;
            if(hideBms.triggerDiff != 0) {
                var targetDiff = hideBms.triggerDiff;
                if(targetDiff == -1) {
                    targetDiff = 2;

                    // Disable the other difficulty options
                    info.AddMaskValue("difficulty1", "0");
                    info.AddMaskValue("difficulty3", "0");
                }
                var diffToHide = "difficulty" + targetDiff;
                var levelDesignToHide = "levelDesigner" + targetDiff;
                var diffStr = "?";
                var levelDesignStr = info.levelDesigner;
                switch(hideBms.m_HideDiff) {
                    case 1:
                        diffStr = info.difficulty1;
                        levelDesignStr = info.levelDesigner1 ?? info.levelDesigner;
                        break;
                    case 2:
                        diffStr = info.difficulty2;
                        levelDesignStr = info.levelDesigner2 ?? info.levelDesigner;
                        break;
                    case 3:
                        diffStr = info.difficulty3;
                        levelDesignStr = info.levelDesigner3 ?? info.levelDesigner;
                        break;
                    case 4:
                        diffStr = info.difficulty4;
                        levelDesignStr = info.levelDesigner4 ?? info.levelDesigner;
                        break;
                    case 5:
                        diffStr = info.difficulty5;
                        levelDesignStr = info.levelDesigner5 ?? info.levelDesigner;
                        break;
                }
                info.AddMaskValue(diffToHide, diffStr);
                info.AddMaskValue(levelDesignToHide, levelDesignStr);
                info.SetDifficulty(targetDiff, hideBms.m_HideDiff);

                MelonLoader.MelonLogger.Log($"Activated hidden chart {hideBms.uid}");
                success = true;
            }

            return success;
        }
    }

    /// <summary>
    /// Turns off custom hiddens when leaving a chart
    /// </summary>
    [HarmonyPatch(typeof(PnlStage), "PreWarm")]
    internal static class FixInvokeHideBms
    {
        private static void Postfix() {
            var invokeHideKeys = Singleton<SpecialSongManager>.instance.m_IsInvokeHideDic.Keys;
            var newKeys = new List<string>();
            foreach(var key in invokeHideKeys) {
                if(key.StartsWith(AlbumManager.Uid.ToString())) {
                    newKeys.Add(key);
                }
            }
            foreach(var key in newKeys) {
                Singleton<SpecialSongManager>.instance.m_IsInvokeHideDic[key] = false;
            }
        }
    }

    /// <summary>
    /// Adds charts to the "With Hidden Sheet" tag
    /// </summary>
    [HarmonyPatch(typeof(MusicTagManager), "InitDefaultInfo")]
    internal static class AddHiddenSheetTagPatch
    {
        private static bool runOnce;

        private static void Postfix() {
            if(!runOnce) {
                var albumList = new List<string>();
                foreach(var album in AlbumManager.LoadedAlbums) {
                    if(album.Value.availableMaps.ContainsKey(4)) {
                        albumList.Add($"{AlbumManager.Uid}-{album.Value.Index}");

                        var newArr = new Il2CppStringArray(DBMusicTagDefine.s_HiddenLocal.Length + 1);
                        for(int i = 0; i < DBMusicTagDefine.s_HiddenLocal.Count; i++) {
                            newArr[i] = DBMusicTagDefine.s_HiddenLocal[i];
                        }
                        newArr[newArr.Length - 1] = $"{AlbumManager.Uid}-{album.Value.Index}";
                        DBMusicTagDefine.s_HiddenLocal = newArr;
                    }
                }
                var tagInfo = GlobalDataBase.dbMusicTag.GetAlbumTagInfo(32776);
                tagInfo.AddTagUids(albumList);
                runOnce = true;
            }
        }
    }
}