using System.IO;
using UnityEngine;

namespace Roguelike.Run
{
    /// <summary>
    /// 局外货币（永久晶核）持久化。
    /// 为什么不是 partial SaveManagerExtensions：当前代码库不存在 SaveManager 类（已核对全目录），
    /// partial 扩展无法编译。此处改为无状态 IO 工具：版本号 v0.1.5 + 原子写入
    /// （先写 .tmp 再替换/改名），损坏或版本不符时回退 0 并告警，不抛出。
    /// 无字段即无静态全局状态，符合"严禁单例/静态状态"约束。
    /// </summary>
    public static class RunCurrencyStore
    {
        private const string Version = "v0.1.5";
        private const string FileName = "run_currency.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        [System.Serializable]
        private sealed class CurrencyData
        {
            public string version = Version;
            public int amount;
        }

        /// <summary>原子写入：先写临时文件，再替换原文件（首次写入用改名，同为原子操作）。</summary>
        public static void SaveCurrency(int amount)
        {
            try
            {
                string json = JsonUtility.ToJson(new CurrencyData { version = Version, amount = amount });
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json);

                if (File.Exists(FilePath))
                {
                    File.Replace(tmp, FilePath, null);
                }
                else
                {
                    File.Move(tmp, FilePath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Diagnostics] 晶核保存失败：{e.Message}");
            }
        }

        /// <summary>读取局外货币；文件缺失/版本不符/损坏时返回 false 且 amount=0，不抛出。</summary>
        public static bool LoadCurrency(out int amount)
        {
            amount = 0;
            try
            {
                if (!File.Exists(FilePath))
                {
                    return false;
                }

                CurrencyData data = JsonUtility.FromJson<CurrencyData>(File.ReadAllText(FilePath));
                if (data == null || data.version != Version)
                {
                    Debug.LogWarning($"[Diagnostics] 晶核存档版本不符（期望 {Version}），按 0 处理。");
                    return false;
                }

                amount = Mathf.Max(0, data.amount);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Diagnostics] 晶核读取失败：{e.Message}");
                return false;
            }
        }
    }
}