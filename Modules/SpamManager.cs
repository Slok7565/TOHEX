﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static TOHEXI.Translator;

namespace TOHEXI;

public static class SpamManager
{
    private static readonly string BANEDWORDS_FILE_PATH = "./TOHEXI_Data/BanWords.txt";
    public static List<string> BanWords = new();
    public static void Init()
    {
        CreateIfNotExists();
        BanWords = ReturnAllNewLinesInFile(BANEDWORDS_FILE_PATH);
    }
    public static void CreateIfNotExists()
    {
        if (!File.Exists(BANEDWORDS_FILE_PATH))
        {
            try
            {
                if (!Directory.Exists(@"TOHEXI_Data")) Directory.CreateDirectory(@"TOHEXI_Data");
                if (File.Exists(@"./BanWords.txt")) File.Move(@"./BanWords.txt", BANEDWORDS_FILE_PATH);
                else
                {
                    string fileName;
                    string[] name = CultureInfo.CurrentCulture.Name.Split("-");
                    if (name.Count() >= 2)
                        fileName = name[0] switch
                        {
                            "zh" => "SChinese",
                            "ru" => "Russian",
                            _ => "English"
                        };
                    else fileName = "English";
                    Logger.Warn($"创建新的 BanWords 文件：{fileName}", "SpamManager");
                    File.WriteAllText(BANEDWORDS_FILE_PATH, GetResourcesTxt($"TOHEXI.Resources.Config.BanWords.{fileName}.txt"));
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "SpamManager");
            }
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    public static List<string> ReturnAllNewLinesInFile(string filename)
    {
        if (!File.Exists(filename)) return new List<string>();
        using StreamReader sr = new(filename, Encoding.GetEncoding("UTF-8"));
        string text;
        List<string> sendList = new();
        while ((text = sr.ReadLine()) != null)
            if (text.Length > 1 && text != "") sendList.Add(text.Replace("\\n", "\n").ToLower());
        return sendList;
    }
    public static bool CheckSpam(PlayerControl player, string text)
    {
        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) return false;
        string name = player.GetRealName();
        bool kick = false;
        string msg = "";

        if (Options.AutoKickStart.GetBool())
        {
            if (ContainsStart(text) && GameStates.IsLobby)
            {
                msg = string.Format(GetString("Message.KickWhoSayStart"), name);
                if (Options.AutoKickStart.GetBool())
                {
                    if (!Main.SayStartTimes.ContainsKey(player.GetClientId())) Main.SayStartTimes.Add(player.GetClientId(), 0);
                    Main.SayStartTimes[player.GetClientId()]++;
                    msg = string.Format(GetString("Message.WarnWhoSayStart"), name, Main.SayStartTimes[player.GetClientId()]);
                    if (Main.SayStartTimes[player.GetClientId()] > Options.AutoKickStartTimes.GetInt())
                    {
                        msg = string.Format(GetString("Message.KickStartAfterWarn"), name, Main.SayStartTimes[player.GetClientId()]);
                        kick = true;
                    }
                }
                if (msg != "") Utils.SendMessage(msg);
                if (kick) AmongUsClient.Instance.KickPlayer(player.GetClientId(), Options.AutoKickStartAsBan.GetBool());
                return true;
            }
        }

        bool banned = BanWords.Any(text.Contains);

        if (!banned) return false;

        if (Options.AutoWarnStopWords.GetBool()) msg = string.Format(GetString("Message.WarnWhoSayBanWord"), name);
        if (Options.AutoKickStopWords.GetBool())
        {
            if (!Main.SayBanwordsTimes.ContainsKey(player.GetClientId())) Main.SayBanwordsTimes.Add(player.GetClientId(), 0);
            Main.SayBanwordsTimes[player.GetClientId()]++;
            msg = string.Format(GetString("Message.WarnWhoSayBanWordTimes"), name, Main.SayBanwordsTimes[player.GetClientId()]);
            if (Main.SayBanwordsTimes[player.GetClientId()] > Options.AutoKickStopWordsTimes.GetInt())
            {
                msg = string.Format(GetString("Message.KickWhoSayBanWordAfterWarn"), name, Main.SayBanwordsTimes[player.GetClientId()]);
                kick = true;
            }
        }

        if (msg != "")
        {
            if (kick || !GameStates.IsInGame) Utils.SendMessage(msg);
            else
            {
                foreach (var pc in Main.AllPlayerControls.Where(x => x.IsAlive() == player.IsAlive()))
                    Utils.SendMessage(msg, pc.PlayerId);
            }
        }
        if (kick) AmongUsClient.Instance.KickPlayer(player.GetClientId(), Options.AutoKickStopWordsAsBan.GetBool());
        return true;
    }
    private static bool ContainsStart(string text)
    {
        text = text.Trim().ToLower();

        int stNum = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i..].Equals("k")) stNum++;
            if (text[i..].Equals("开")) stNum++;
        }
        if (stNum >= 3) return true;

        if (text == "Start") return true;
        if (text == "start") return true;
        if (text == "开") return true;
        if (text == "快开") return true;
        if (text == "开始") return true;
        if (text == "开啊") return true;
        if (text == "开阿") return true;
        if (text == "kai") return true;
        if (text == "kaishi") return true;
        if (text.Contains("started")) return false;
        if (text.Contains("starter")) return false;
        if (text.Contains("Starting")) return false;
        if (text.Contains("starting")) return false;
        if (text.Contains("beginner")) return false;
        if (text.Contains("beginned")) return false;
        if (text.Contains("了")) return false;
        if (text.Contains("没")) return false;
        if (text.Contains("吗")) return false;
        if (text.Contains("哈")) return false;
        if (text.Contains("还")) return false;
        if (text.Contains("现")) return false;
        if (text.Contains("不")) return false;
        if (text.Contains("可")) return false;
        if (text.Contains("刚")) return false;
        if (text.Contains("的")) return false;
        if (text.Contains("打")) return false;
        if (text.Contains("门")) return false;
        if (text.Contains("关")) return false;
        if (text.Contains("怎")) return false;
        if (text.Contains("要")) return false;
        if (text.Contains("摆")) return false;
        if (text.Contains("啦")) return false;
        if (text.Contains("咯")) return false;
        if (text.Contains("嘞")) return false;
        if (text.Contains("勒")) return false;
        if (text.Contains("心")) return false;
        if (text.Contains("呢")) return false;
        if (text.Contains("门")) return false;
        if (text.Contains("总")) return false;
        if (text.Contains("哥")) return false;
        if (text.Contains("姐")) return false;
        if (text.Contains("《")) return false;
        if (text.Contains("?")) return false;
        if (text.Contains("？")) return false;
        if (text.Length >= 3) return false;
        if (text.Contains("start")) return true;
        if (text.Contains("s t a r t")) return true;
        if (text.Contains("begin")) return true;
        return text.Contains("开") || text.Contains("kai");
    }
}
