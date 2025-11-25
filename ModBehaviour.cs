using System;
using Duckov.Scenes;
using Duckov.Utilities;
using HarmonyLib;
using UnityEngine;
using ItemStatsSystem;

namespace CrownProbabilityModification
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        /// <summary>
        /// Harmony实例，用于应用代码补丁
        /// </summary>
        public Harmony harmony = new Harmony("CrownProbabilityModification");
        
        // 场景加载标记，确保什么时候触发生成
        private static bool sceneLevelLoaded = false;
        
        // 场景加载事件处理器，用于后续注销
        private UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode>? sceneLoadedHandler;
        
        /// <summary>
        /// 当组件启用时调用
        /// 加载Harmony库
        /// </summary>
        private void OnEnable()
        {
            HarmonyLoad.HarmonyLoad.Load0Harmony();
        }

        /// <summary>
        /// 当组件被离活时调用
        /// </summary>
        private void OnDisable()
        {
            // 注销场景加载事件
            if (sceneLoadedHandler != null)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= sceneLoadedHandler;
            }
            
            // 移除所有Harmony补丁
            try
            {
                harmony?.UnpatchAll("CrownProbabilityModification");
            }
            catch
            {
                // 静默处理错误
            }
        }
                
        /// <summary>
        /// 当游戲对象第一次被激活时调用
        /// 应用所有Harmony补丁并初始化配置系統
        /// </summary>
        private void Start()
        {
            // 应用所有Harmony补丁
            try
            {
                harmony.PatchAll();
            }
            catch
            {
                // 静默处理错误
            }

            // 从 GameObject 获取 CrownConfig 组件
            var crownConfig = GetComponent<CrownConfig>();
            if (crownConfig == null)
            {
                // 如果GameObject上没有CrownConfig，为其添加
                gameObject.AddComponent<CrownConfig>();
            }
            
            // 注册场景加载事件，每次进入新场景时重置标志
            sceneLoadedHandler = (scene, mode) => 
            {
                sceneLevelLoaded = false;
            };
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += sceneLoadedHandler;
        }

        /// <summary>
        /// 每帧更新时调用
        /// 可用于处理实时逻辑
        /// </summary>
        private void Update()
        {
            // 检查是否启用皇冠爆率功能
            if (!CrownConfig.EnableCrownDropRate)
            {
                return;
            }

            // 检测是否进入了农场镇（只检测一次）
            if (!sceneLevelLoaded)
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                
                // 农场镇场景 ID: Level_Farm_Main
                if (activeScene.name == "Level_Farm_Main")
                {
                    sceneLevelLoaded = true;
                    OnFarmLevelLoaded();
                }
            }
        }

        /// <summary>
        /// 为Four 个常驻刷新点添加1254号物品（概率）
        /// 位置A：(389.75, 0.00, 657.61) 和 (388.74, 0.00, 656.71)
        /// 位置B：(528.59, 0.00, 326.56) 和 (528.59, 0.00, 326.72)
        /// </summary>
        private void OnFarmLevelLoaded()
        {
            // 获取爆率
            float crownDropRate = CrownConfig.CrownDropRate;
        
            // 当爆率为100%时，确保必定触发
            if (crownDropRate >= 100f)
            {
                GenerateRandomCrown();
                return;
            }

            // 计算随机概率（0-100%）
            float randomChance = UnityEngine.Random.Range(0f, 100f);
                    
            if (randomChance < crownDropRate)
            {
                GenerateRandomCrown();
            }
        }

        private void GenerateRandomCrown()
        {
            Vector3[] targetPositions = new Vector3[]
            {
                new Vector3(389.75f, 0.50f, 657.61f),
                new Vector3(388.74f, 0.50f, 656.71f),
                new Vector3(528.59f, 0.50f, 326.56f),
                new Vector3(528.59f, 0.50f, 326.72f)
            };
        
            // 随机选择一个位置
            int randomIndex = UnityEngine.Random.Range(0, targetPositions.Length);
            Vector3 selectedPos = targetPositions[randomIndex];
        
            // 创建1254号物品（皇冠）
            int itemId = 1254;
            var crownItem = ItemStatsSystem.ItemAssetsCollection.InstantiateSync(itemId);
                    
            if (crownItem != null)
            {
                crownItem.transform.position = selectedPos;
                crownItem.transform.rotation = UnityEngine.Quaternion.identity;
                        
                // 使用Drop方法显示物品在地面
                var dropResult = crownItem.Drop(selectedPos, false, UnityEngine.Vector3.up, 0f);
            }
        }


        /// <summary>
        /// 临时调查方法：遍历农场镇的地面物品（LootSpawner）并打印生成结果
        /// </summary>
        private void InvestigateLootSpawners()
        {
            try
            {
                // 获取 DLL 所在文件夹路径
                string dllPath = typeof(ModBehaviour).Assembly.Location;
                string dllFolder = System.IO.Path.GetDirectoryName(dllPath);
                string logFilePath = System.IO.Path.Combine(dllFolder, "LootSpawner_Investigation.log");

                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(logFilePath, false, System.Text.Encoding.UTF8))
                {
                    // 获取当前场景信息
                    var multiSceneCore = UnityEngine.Object.FindFirstObjectByType<MultiSceneCore>();
                    if (multiSceneCore == null)
                    {
                        writer.WriteLine("[调查] MultiSceneCore 未找到");
                        writer.Flush();
                        return;
                    }

                    var sceneInfo = multiSceneCore.SceneInfo;
                    if (sceneInfo == null)
                    {
                        writer.WriteLine("[调查] 场景信息未找到");
                        writer.Flush();
                        return;
                    }

                    string sceneName = sceneInfo.ID;
                    writer.WriteLine("\n========== 农场镇地面物品调查开始 ==========");
                    writer.WriteLine($"场景: {sceneName}");
                    
                    // 记录玩家当前位置
                    try
                    {
                        var levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
                        if (levelManager != null && levelManager.MainCharacter != null)
                        {
                            var playerPos = levelManager.MainCharacter.transform.position;
                            writer.WriteLine($"玩家位置: ({playerPos.x:F2}, {playerPos.y:F2}, {playerPos.z:F2})");
                        }
                    }
                    catch { }

                    // 获取所有 LootSpawner 组件
                    var lootSpawners = UnityEngine.Object.FindObjectsByType<LootSpawner>(FindObjectsSortMode.None);
                    
                    if (lootSpawners.Length == 0)
                    {
                        writer.WriteLine("[调查] 场景中未找到任何 LootSpawner 组件");
                        writer.Flush();
                        return;
                    }

                    writer.WriteLine($"[调查] 找到 {lootSpawners.Length} 个 LootSpawner 组件");

                    int totalCount = lootSpawners.Length;

                    for (int i = 0; i < lootSpawners.Length; i++)
                    {
                        var spawner = lootSpawners[i];
                        var position = spawner.transform.position;
                        var spawnerType = spawner.GetType();
                        
                        // 打印LootSpawner的所有字段信息
                        writer.WriteLine($"\n[调查 {i + 1}/{totalCount}] 位置: ({position.x:F2}, {position.y:F2}, {position.z:F2})");
                        writer.WriteLine($"类型: {spawnerType.Name}");
                        
                        // 列出所有字段
                        var fields = spawnerType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        writer.WriteLine($"字段总数: {fields.Length}");
                        
                        foreach (var field in fields)
                        {
                            try
                            {
                                object? value = field.GetValue(spawner);
                                if (value is Item item)
                                {
                                    writer.WriteLine($"  [{field.Name}] = Item(ID={item.TypeID})");
                                }
                                else if (field.Name == "fixedItems" && value is System.Collections.IList list)
                                {
                                    writer.Write($"  [{field.Name}] = [");
                                    for (int j = 0; j < list.Count; j++)
                                    {
                                        if (j > 0) writer.Write(", ");
                                        if (list[j] is int itemId)
                                        {
                                            string itemName = "未知";
                                            try
                                            {
                                                // 尝试获取物品信息
                                                var itemAssetsField = spawnerType.GetField("itemAssets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                                if (itemAssetsField != null)
                                                {
                                                    object? assetsObj = itemAssetsField.GetValue(spawner);
                                                    if (assetsObj is ItemAssetsCollection itemAssets)
                                                    {
                                                        var getItemMethod = itemAssets.GetType().GetMethod("GetItem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new[] { typeof(int) }, null);
                                                        if (getItemMethod != null)
                                                        {
                                                            object? invokeResult = getItemMethod.Invoke(itemAssets, new object[] { itemId });
                                                            if (invokeResult is Item foundItem)
                                                            {
                                                                itemName = foundItem.DisplayName;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            catch { }
                                            
                                            writer.Write($"{itemId} - {itemName}");
                                        }
                                        else
                                        {
                                            writer.Write(list[j]);
                                        }
                                    }
                                    writer.WriteLine("]");
                                }
                                else if (field.Name.Contains("itemAssets") || field.Name.Contains("ItemAssets"))
                                {
                                    writer.WriteLine($"  [{field.Name}] = {value?.GetType().Name ?? "null"}");
                                }
                                else if (value != null && value.GetType().Name.Contains("Item"))
                                {
                                    writer.WriteLine($"  [{field.Name}] = {value.GetType().Name}");
                                }
                                else if (field.Name.Contains("Item") || field.Name.Contains("Loot") || field.Name.Contains("item") || field.Name.Contains("loot"))
                                {
                                    writer.WriteLine($"  [{field.Name}] = {value}");
                                }
                            }
                            catch { }
                        }
                    }
                    writer.WriteLine($"========== 农场镇地面物品调查结束 ==========\n");
                    writer.WriteLine($"日志已保存至: {logFilePath}");
                    
                    writer.Flush();
                }
            }
            catch (System.Exception ex)
            {
                // 同时记录到 Debug 和文件
                string dllPath = typeof(ModBehaviour).Assembly.Location;
                string dllFolder = System.IO.Path.GetDirectoryName(dllPath);
                string errorLogPath = System.IO.Path.Combine(dllFolder, "LootSpawner_Investigation_Error.log");
                
                try
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(errorLogPath, true, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine($"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] 调查过程出错");
                        writer.WriteLine($"错误信息: {ex.Message}");
                        writer.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                        writer.WriteLine(new string('-', 80));
                        writer.Flush();
                    }
                }
                catch { }
                
                Debug.LogError($"[调查] 调查过程出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        
        /// <summary>
        /// 当Mod被销毁时调用
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
        }
    }
}