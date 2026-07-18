# ApeRadar 现代化重构计划

## 当前基线

- 应用类型：Windows WPF 桌面应用，无独立后端进程。
- 目标框架：`net10.0-windows`。
- 代码规模：约 5,900 行 C#、2,600 行 XAML，船只目录约 4,900 行 JSON。
- 当前测试：15 个测试全部通过。
- 当前分支：`refactor/fluent-modernization`。
- 基线提交：`5c1770d`。
- 架构审查报告：`C:\Users\DSUK\AppData\Local\Temp\architecture-review-20260719-aperadar.html`。

## 目标

1. 保持战斗文件解析、统计结果、输出模板、关注名单、更新和用户数据兼容。
2. 让应用窗口形成单一 Fluent 视觉语言，支持深色和浅色主题。
3. 缩短冷启动到可交互的时间，减少空闲 CPU、内存和无效 UI 刷新。
4. 将业务规则、外部统计源、文件系统和 WPF 视图放在清晰的 Module 中。
5. 让每个关键 Module 的 Interface 都可以被 fixture、golden test 或替身 Adapter 验证。

## 目标结构

```text
ApeRadar.Core
  Player、Battlefield、胜率、分类、排序、输出模板

ApeRadar.Application
  BattleIntake、重试、取消、应用状态和用例编排

ApeRadar.Infrastructure
  tempArenaInfo、统计源、WatchList、配置、船只目录、更新

ApeRadar.Desktop
  WPF View、ViewModel、Fluent 主题、图表、Windows 集成
```

## 执行阶段

### Phase 0：基线与工程卫生

- [x] 初始化 Git 和 `refactor/fluent-modernization` 分支。
- [x] 添加构建产物和本地 IDE 文件忽略规则。
- [x] 固定当前测试基线。
- [x] 添加 `global.json`，明确 SDK 策略。
- [ ] 统一 README、目标框架、发布脚本和运行时说明。
- [ ] 解决 `NU1701` 旧 .NET Framework 依赖警告。

### Phase 1：Fluent 设计系统

- [x] 令牌层：背景、表面、文本、边框、强调色、状态色、间距、圆角、字体。
- [x] 深色/浅色只替换令牌，不在页面中写死颜色。
- [x] 全局控件样式覆盖按钮、输入框、下拉框、复选框、滑块、Tab、DataGrid、菜单、提示和滚动条。
- [ ] 为 LiveCharts 建立 Fluent 图表适配器。
- [ ] 用自定义 Fluent Dialog 替换应用内 MessageBox。
- [ ] 建立组件展示页和深浅主题截图回归。

### Phase 2：战斗加载 Module 深化

- [ ] 将 `MainWindow` 中的加载状态移到 `BattleIntake` Module。
- [ ] 统一打开、刷新、拖放和自动检测四种入口。
- [ ] 用 `FileSystemWatcher + 防抖` 代替无效轮询，保留轮询后备。
- [ ] 保留现有重试、错误消息和取消语义。

### Phase 3：统计源和数据契约

- [ ] 将 `ApiUtils` 拆为强类型统计源 Adapter。
- [ ] 使用 DTO 解析 WG、Vortex 和 Yuyuko 响应。
- [ ] 统一超时、限流、重试、错误分类和日志。
- [ ] 使用固定响应 fixture 做契约测试。

### Phase 4：应用状态与配置迁移

- [ ] 将 `Settings.Default` 收敛到版本化设置 Module。
- [ ] 保留旧 `user.config`、`WatchList.json` 和 `placement.config` 的迁移。
- [ ] 将配置窗口改为 `SettingsViewModel`。
- [ ] 将通知、主题和窗口状态通过应用状态 Interface 暴露给 View。

### Phase 5：视觉工作台重做

- [ ] 主窗口改为 Fluent CommandBar + 队伍数据区 + 图表区 + 输出区。
- [ ] 配置窗口改为左侧导航、右侧设置内容。
- [ ] 所有控件统一焦点、悬停、按下、禁用和选中状态。
- [ ] 使用统一 Fluent 图标，不用 emoji 作为命令按钮图标。

### Phase 6：发布和性能验证

- [ ] 建立冷启动、首屏、战斗加载、内存和 CPU 基线。
- [ ] 延迟非必要网络和资源初始化。
- [ ] 验证 ReadyToRun、单文件和 self-contained 的实际收益。
- [ ] 完成 Windows 10/11、无网络、损坏文件和升级回滚测试。

## 不变量

- 同一战斗 fixture 的玩家字段、队伍划分、排序、图表输入和输出文本必须保持一致。
- 用户已有设置和运行时数据不能因升级丢失。
- 更新包不能覆盖 `WatchList.json`、窗口位置、日志和截图。
- API 错误、隐藏战绩、服务器自动识别失败必须继续映射到现有用户提示。

## 第一批实际改动

本分支第一批代码已引入 Fluent 设计令牌、控件样式基础和主窗口布局外壳，不改变业务加载、API 请求和持久化行为。现有测试已扩展为 17 个，并通过真实 `.wowsreplay` 烟测；最新顶部摘要卡片布局已通过编译，但因桌面验证被用户按 Escape 停止，尚未完成最终截图确认。
