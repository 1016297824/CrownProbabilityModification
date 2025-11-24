# 皇冠爆率修改 Mod (Crown Probability Modification)

## 项目简介

这是一个游戏Mod，用于在农场镇场景中根据设定的概率自动生成皇冠物品（ID: 1254）。玩家可以通过ModConfig配置界面灵活控制功能的启用状态和爆率百分比。

## 核心特性

- ✅ **概率生成**：根据用户设定的概率自动在农场镇生成皇冠物品
- ✅ **随机位置**：皇冠在4个预设位置中随机选择生成点
- ✅ **ModConfig集成**：通过ModConfig界面便捷配置功能
- ✅ **本地配置持久化**：配置自动保存到本地JSON文件，游戏重启后保留设置
- ✅ **场景自动重置**：每次进入农场场景都有机会生成一次皇冠（不会无限生成）
- ✅ **安全内存管理**：正确注销事件监听，避免内存泄漏

## 快速开始

### 安装方式
1. 将编译后的DLL文件放入游戏的Mod文件夹
2. 重启游戏，Mod将自动加载

### 基本使用
1. 打开ModConfig配置界面
2. 找到 "CrownProbabilityModification" 配置分组
3. 启用功能：勾选 "是否启用独立皇冠爆率功能" 复选框
4. 设置爆率：在 "皇冠爆率，范围 0.01% - 100%" 滑条中调整概率
5. 进入农场镇，根据设定的概率自动生成皇冠

## 配置说明

### 配置项

| 配置项 | 类型 | 范围 | 默认值 | 说明 |
|--------|------|------|--------|------|
| EnableCrownDropRate | 布尔值 | true/false | false | 是否启用皇冠爆率功能 |
| CrownDropRate | 浮点数 | 0.01 - 100 | 0.03 | 皇冠生成概率（百分比） |

### 配置文件位置

- **ModConfig配置**：通过游戏内ModConfig界面管理
- **本地备份文件**：`CrownConfig.json`（位于DLL同目录）
  ```json
  {
    "EnableCrownDropRate": false,
    "CrownDropRate": 0.03
  }
  ```

## 生成机制

### 皇冠生成流程

1. **场景检测**：进入农场镇场景（Level_Farm_Main）时触发
2. **概率判定**：根据设定的爆率进行随机判定
3. **随机位置**：在以下4个位置中随机选择：
   - 位置A1: (389.75, 0.00, 657.61)
   - 位置A2: (388.74, 0.00, 656.71)
   - 位置B1: (528.59, 0.00, 326.56)
   - 位置B2: (528.59, 0.00, 326.72)
4. **物品显示**：使用Drop方法将皇冠正确显示在地面上

### 生成规则

- 每次进入农场场景时，最多生成**一次**皇冠
- 同一次农场会话中不会重复生成
- 离开农场后，再次进入时会重置生成标志，又可以生成一次
- 概率基于0-100%的随机数生成，完全取决于爆率设置

## 技术架构

### 核心类

| 类名 | 职责 | 关键方法 |
|------|------|----------|
| **ModBehaviour** | Mod主入口，管理生成逻辑和场景检测 | Update(), OnFarmLevelLoaded(), GenerateRandomCrown() |
| **CrownConfig** | 配置管理，处理ModConfig和本地文件同步 | InitializeConfig(), LoadConfig(), SaveLocalConfig() |
| **ModConfigAPI** | ModConfig系统的安全接口封装 | SafeAddBoolDropdownList(), SafeAddInputWithSlider() |

### 工作流程

```
游戏启动
  ↓
ModBehaviour.OnEnable() - 加载Harmony库，注册场景加载事件
  ↓
ModBehaviour.Start() - 初始化配置系统
  ↓
CrownConfig.InitializeConfig() - 从ModConfig和本地文件加载配置
  ↓
每帧更新
  ↓
ModBehaviour.Update() - 检测是否进入农场镇
  ↓
场景加载事件 - 重置生成标志 (sceneLevelLoaded = false)
  ↓
OnFarmLevelLoaded() - 执行概率判定和皇冠生成
```

## 配置优先级

1. **ModConfig配置** ✅ 优先级最高，是主配置源
2. **本地JSON文件** - 作为备份和容错机制
3. **代码默认值** - 当以上都不可用时使用

### 配置同步流程

- 用户在ModConfig界面修改设置 → 保存到ModConfig
- ModBehaviour检测到配置变更 → 从ModConfig加载新值
- 新值与本地文件不同 → 自动同步到CrownConfig.json
- 游戏重启时 → 依次从本地文件和ModConfig加载配置

## 故障排查

### 问题：皇冠没有生成

**检查项：**
1. 确认功能是否启用（EnableCrownDropRate = true）
2. 确认爆率设置是否大于0（CrownDropRate > 0）
3. 确认是否在农场镇场景中（场景ID需要是 Level_Farm_Main）
4. 尝试多次进入农场，概率可能较低

### 问题：重启游戏后设置丢失

**原因分析：**
- ModConfig未加载或不可用时，使用本地配置文件
- 如果本地配置文件损坏，Mod会自动使用代码默认值

**解决方案：**
1. 检查DLL目录下是否有CrownConfig.json文件
2. 如果没有，手动创建配置文件或重新配置一次

### 问题：性能问题

**优化措施：**
- Mod已优化Update()方法，只在需要时执行检测
- 每进入农场只会进行一次概率判定和生成
- 使用标志位避免重复执行，确保性能高效

## 文件列表

```
CrownProbabilityModification/
├── ModBehaviour.cs          # 主逻辑类
├── CrownConfig.cs           # 配置管理类
├── ModConfigApi.cs          # ModConfig安全接口封装
├── HarmonyLoad.cs           # Harmony库加载
├── CrownConfig.json         # 本地配置文件（自动生成）
└── README.md                # 本说明文档
```

## 常见问题 (FAQ)

**Q: 可以修改爆率范围吗？**
A: 可以。在CrownConfig.cs中修改Clamp的范围参数即可。

**Q: 为什么要同时支持ModConfig和本地文件？**
A: 提高容错性。当ModConfig不可用时，本地文件可以作为备份配置源。

**Q: 皇冠物品可以更换吗？**
A: 可以。在GenerateRandomCrown()方法中修改itemId = 1254为其他物品ID。

**Q: 生成位置可以自定义吗？**
A: 可以。在GenerateRandomCrown()方法中修改targetPositions数组中的坐标值。

## 开发信息

- **Mod框架**：基于Harmony补丁系统
- **配置系统**：ModConfig API + 本地JSON持久化
- **编译环境**：.NET Standard 2.1
- **目标平台**：Unity游戏引擎

## 许可证

本Mod仅供学习和游戏使用，不得用于商业目的。

## 更新日志

### v1.0.0 (初始版本)
- ✅ 实现皇冠概率生成基础功能
- ✅ 集成ModConfig配置界面
- ✅ 实现本地JSON配置文件持久化
- ✅ 优化场景检测和生成逻辑
- ✅ 完善事件监听和内存管理
