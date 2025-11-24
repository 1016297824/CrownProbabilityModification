using System;
using UnityEngine;
using System.IO;
using Duckov.Modding;

namespace CrownProbabilityModification
{
    /// <summary>
    /// 皇冠爆率配置管理类
    /// 管理两个按钮配置：是否启用皇冠功能、皇冠爆率
    /// </summary>
    public class CrownConfig : MonoBehaviour
    {
        private static CrownConfig? _instance;
        /// <summary>
        /// 获取CrownConfig单例实例
        /// </summary>
        public static CrownConfig? Instance => _instance;

        /// <summary>
        /// Mod名称，用于在ModConfig中标识此Mod
        /// </summary>
        private const string MOD_NAME = "CrownProbabilityModification";

        // 配置项键名常量
        private const string ENABLE_CROWN_KEY = "EnableCrownDropRate";
        private const string CROWN_DROP_RATE_KEY = "CrownDropRate";

        // 预拼接的配置键名
        private static readonly string FULL_ENABLE_CROWN_KEY = $"{MOD_NAME}_{ENABLE_CROWN_KEY}";
        private static readonly string FULL_CROWN_DROP_RATE_KEY = $"{MOD_NAME}_{CROWN_DROP_RATE_KEY}";

        // 配置项描述信息
        private const string ENABLE_CROWN_DESC = "是否启用独立皇冠爆率功能";
        private const string CROWN_DROP_RATE_DESC = "皇冠爆率，范围 0.01% - 100%";

        /// <summary>
        /// 本地配置文件路径
        /// </summary>
        private static string persistentConfigPath => Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "CrownConfig.json");

        // 配置数据
        public static bool EnableCrownDropRate { get; private set; } = false;
        public static float CrownDropRate { get; private set; } = 0.03f;

        // 保存上一次的值用于比较
        private static bool previousEnableCrown = false;
        private static float previousCrownDropRate = 0.03f;

        // 标记是否已经初始化过配置项
        private static bool isConfigInitialized = false;

        /// <summary>
        /// 当组件被唤醒时调用
        /// 确保此类为单例模式并初始化配置
        /// </summary>
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeConfig();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 当组件启用时调用
        /// </summary>
        private void OnEnable()
        {
            // 添加Mod激活事件监听
            ModManager.OnModActivated += OnModActivated;
        }

        /// <summary>
        /// 当组件被禁用时调用
        /// </summary>
        private void OnDisable()
        {
            // 移除Mod激活事件监听
            ModManager.OnModActivated -= OnModActivated;

            // 移除配置变更事件监听
            ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnConfigChanged);
        }

        /// <summary>
        /// Mod激活事件处理
        /// </summary>
        private void OnModActivated(Duckov.Modding.ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if (info.name == ModConfigAPI.ModConfigName)
            {
                InitializeConfig();
            }
        }

        /// <summary>
        /// 初始化ModConfig配置
        /// 注册配置项并加载当前设置
        /// </summary>
        private void InitializeConfig()
        {
            // 检查ModConfig是否可用
            if (!ModConfigAPI.IsAvailable())
            {
                // 如果ModConfig不可用，从本地配置加载
                LoadLocalConfig();
                return;
            }

            // 避免重复初始化配置项
            if (isConfigInitialized)
            {
                return;
            }

            // 先从本地配置加载，获取当前设置
            LoadLocalConfig();

            // 保存加载后的本地配置值
            bool localEnableCrown = EnableCrownDropRate;
            float localCrownDropRate = CrownDropRate;

            // 注册配置项变更事件
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnConfigChanged);

            // 添加皇冠爆率滑条输入框配置项
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                CROWN_DROP_RATE_KEY,
                CROWN_DROP_RATE_DESC,
                typeof(float),
                CrownDropRate, // 使用当前值而不是硬编码默认值
                new Vector2(0.01f, 100f) // 滑条范围 0.01f~100f
            );

            // 添加启用皇冠功能的布尔下拉列表配置项
            ModConfigAPI.SafeAddBoolDropdownList(
                MOD_NAME,
                ENABLE_CROWN_KEY,
                ENABLE_CROWN_DESC,
                EnableCrownDropRate // 使用当前值而不是硬编码默认值
            );

            isConfigInitialized = true;

            // 从ModConfig加载最新配置
            LoadConfig();

            // 检查ModConfig中的配置是否与本地配置不同
            bool configChanged = EnableCrownDropRate != localEnableCrown || 
                               Math.Abs(CrownDropRate - localCrownDropRate) > 0.001f;

            // 如果配置不同，将ModConfig的配置同步到本地配置文件
            if (configChanged)
            {
                SaveLocalConfig();
            }

            // 设置初始值用于比较
            previousEnableCrown = EnableCrownDropRate;
            previousCrownDropRate = CrownDropRate;
        }

        /// <summary>
        /// 配置变更时的回调函数
        /// 当用户在ModConfig界面中更改设置时调用
        /// </summary>
        private void OnConfigChanged(string key)
        {
            if (key == FULL_ENABLE_CROWN_KEY || key == FULL_CROWN_DROP_RATE_KEY)
            {
                LoadConfig();

                // 只有在值真正发生变化时才保存到本地文件
                if (EnableCrownDropRate != previousEnableCrown || 
                    Math.Abs(CrownDropRate - previousCrownDropRate) > 0.001f)
                {
                    SaveLocalConfig();
                    previousEnableCrown = EnableCrownDropRate;
                    previousCrownDropRate = CrownDropRate;
                }
            }
        }

        /// <summary>
        /// 从ModConfig加载配置
        /// </summary>
        private void LoadConfig()
        {
            if (ModConfigAPI.IsAvailable())
            {
                // 从ModConfig加载新值
                EnableCrownDropRate = ModConfigAPI.SafeLoad<bool>(MOD_NAME, ENABLE_CROWN_KEY, EnableCrownDropRate);
                CrownDropRate = ModConfigAPI.SafeLoad<float>(MOD_NAME, CROWN_DROP_RATE_KEY, CrownDropRate);
                
                // 确保爆率在有效范围内
                CrownDropRate = Mathf.Clamp(CrownDropRate, 0.01f, 100f);
            }
        }

        /// <summary>
        /// 保存配置到本地文件（JSON格式）
        /// </summary>
        private void SaveLocalConfig()
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(persistentConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 使用JSON格式保存
                string configContent = "{\n" +
                    $"  \"EnableCrownDropRate\": {EnableCrownDropRate.ToString().ToLower()},\n" +
                    $"  \"CrownDropRate\": {CrownDropRate:F2}\n" +
                    "}";

                File.WriteAllText(persistentConfigPath, configContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[皇冠配置] 保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从本地文件加载配置（JSON格式）
        /// </summary>
        private void LoadLocalConfig()
        {
            try
            {
                if (File.Exists(persistentConfigPath))
                {
                    string jsonContent = File.ReadAllText(persistentConfigPath);
                    
                    // 简单的JSON解析
                    if (jsonContent.Contains("\"EnableCrownDropRate\""))
                    {
                        // 提取EnableCrownDropRate值
                        int startIdx = jsonContent.IndexOf("\"EnableCrownDropRate\"") + "\"EnableCrownDropRate\"".Length;
                        int colonIdx = jsonContent.IndexOf(":", startIdx);
                        int commaIdx = jsonContent.IndexOf(",", colonIdx);
                        if (commaIdx == -1)
                            commaIdx = jsonContent.IndexOf("}", colonIdx);
                        
                        string enableValue = jsonContent.Substring(colonIdx + 1, commaIdx - colonIdx - 1).Trim();
                        if (bool.TryParse(enableValue, out bool boolValue))
                            EnableCrownDropRate = boolValue;
                    }
                    
                    if (jsonContent.Contains("\"CrownDropRate\""))
                    {
                        // 提取CrownDropRate值
                        int startIdx = jsonContent.IndexOf("\"CrownDropRate\"") + "\"CrownDropRate\"".Length;
                        int colonIdx = jsonContent.IndexOf(":", startIdx);
                        int commaIdx = jsonContent.IndexOf(",", colonIdx);
                        if (commaIdx == -1)
                            commaIdx = jsonContent.IndexOf("}", colonIdx);
                        
                        string rateValue = jsonContent.Substring(colonIdx + 1, commaIdx - colonIdx - 1).Trim();
                        if (float.TryParse(rateValue, out float floatValue))
                            CrownDropRate = Mathf.Clamp(floatValue, 0.01f, 100f);
                    }
                }
                else
                {
                    // 如果配置文件不存在，创建默认配置
                    SaveLocalConfig();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[皇冠配置] 加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置是否启用皇冠功能
        /// </summary>
        public static void SetEnableCrownDropRate(bool enable)
        {
            EnableCrownDropRate = enable;
            if (Instance != null)
            {
                Instance.SaveLocalConfig();
            }
        }

        /// <summary>
        /// 设置皇冠爆率（0.01 - 100）
        /// </summary>
        public static void SetCrownDropRate(float rate)
        {
            CrownDropRate = Mathf.Clamp(rate, 0.01f, 100f);
            if (Instance != null)
            {
                Instance.SaveLocalConfig();
            }
        }
    }
}
