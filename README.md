# Codex 额度悬浮窗

一个可自由调整大小与外观的 Windows 置顶悬浮窗。用双层圆环同时查看 Codex **剩余额度与距离重置的剩余时间**，也可切换为卡片视图；支持当日／累积 Token 统计、Standard API 等价金额和独立的速度档位金额估算。

本仓库只包含源码、测试和设计预览，不包含用户会话、登录凭据或本机生成的数据。本项目是社区工具，不是 OpenAI 官方产品。

版本变化见 [CHANGELOG.md](./CHANGELOG.md)。

<details>
<summary>早期设计预览（非当前版本截图）</summary>

![早期环形额度设计预览](./design/circular-style-period-options-preview-v3.png)

图中的额度、Token、金额与时间均为虚构示例数据。当前版本已增加时间内圈、图例、紧凑统计与响应式布局，请以下方功能说明为准。

</details>

## 直接使用

双击 [`启动悬浮窗.cmd`](./启动悬浮窗.cmd)。首次从源码启动需要 .NET 8 SDK 自动构建，之后会直接打开本地生成的单文件程序。若仓库发布了预编译版本，请从 GitHub Releases 下载，不要从源码分支获取未知二进制文件。

## 悬浮窗怎么看

### 额度与时间：同一个组件，内外两圈

- **外圈彩色部分：剩余额度**；中心数字显示剩余百分比，灰色部分表示已消耗额度。
- **青色内圈：本周期剩余时间**；随着重置时刻临近，彩色部分逐渐减少，灰色部分表示已过时间。
- 图例统一居中放在圆环区域下方，说明内外圈与颜色的含义；正常视图显示已过与剩余时长，紧凑视图保留简短倒计时，悬停可查看准确重置时刻。
- 额度周期依据 Codex 返回的实际窗口长度识别。可在右键菜单或齿轮设置中关闭“显示 5 小时额度”，仅保留周额度并居中显示；缺少周期数据时不绘制时间内圈。
- 可切换为卡片额度视图。重置卡常驻底部，数量为 0 时仍显示，未同步时显示占位符；额外 Credits／个人额度可在提示中查看。

### 拖动缩放：先重新排版，再压缩文字

拖动窗口可改变位置，拖动四边或四角可调整大小，拉伸区域与可见外框对齐。支持 `150 × 120` 至 `720 × 900` DIP（逻辑像素）。

- **宽窗口**：额度组件并排；**窄高窗口**：根据可用空间纵向排列。
- **高度不足时**：自动采用紧凑布局，保留圆环、重置倒计时、图例和重置卡；Token 面板开启时仍保留 Token 总量与主金额，详细分类及档位金额通过“展开详细统计”查看。
- **常规尺寸**：优先调整排列、间距与信息密度，小字不低于 11 DIP，不将文字横向或纵向单独拉伸。
- **极小尺寸**：宽度低于 300 或高度低于 240 DIP 后，紧凑内容与文字才一起等比缩小，因此实际显示文字可以小于 11 DIP；拉大后自动恢复。

### Token 与金额：可选显示，独立统计

- 支持“当日”与“累积”统计；累积周期可选近 7／30／90 天、全部或自定义日期。
- 完整面板显示 Token 总量、分类分布、Standard API 等价金额与独立的速度档位金额估算；档位估算不覆盖 Standard 金额。
- Token 面板可单独隐藏，也可从右键菜单或外观设置恢复。四类 Token 与金额口径见下方说明。

### 外观与操作

- 点击齿轮调整环形／卡片风格、显示选项、统计周期、窗口宽高和颜色；支持预设色与 `#RRGGBB` 自定义颜色，浅色背景自动搭配深色文字。
- 背景与文字不透明度分别支持 **0%–100%**，即时生效并保存；0% 为完全透明，100% 为完全不透明。
- 玻璃风格采用自绘的柔和色膜、细边与高光，当前不使用系统 Acrylic 背景模糊，也不是 iOS Liquid Glass 的实时折射效果。
- 右键可刷新、调整置顶状态、打开外观设置或官方 Usage 面板。窗口位置、尺寸、颜色、透明度与显示选项会自动保存。
- 单实例运行；重复启动会通知已有实例恢复窗口，关闭窗口时回收后台子进程。

## 数据来源与隐私

- 额度信息来自本机 Codex，并显示服务端提供的剩余百分比与重置时间；接口暂不可用时会明确标为“缓存”。
- Token 统计来自本地 Codex 会话日志，只处理计数、模型、档位和时间信息。
- 程序不读取登录凭据，不展示提示词或回复正文，也不上传使用统计。

完整的数据边界见 [PRIVACY.md](./PRIVACY.md)。

## Token 统计与金额估算

Token 面板按本地日期汇总，并将使用量分为四个互不重叠的类别：

- 普通输入：输入 Token 扣除缓存读取后的部分，其中缓存写入仍属于普通输入显示；
- 缓存读取：`cached_input_tokens`；
- 可见输出：输出 Token 扣除推理 Token；
- 推理输出：`reasoning_output_tokens`。

界面同时提供 Standard API 等价金额和线程速度档位估算。历史日志信息不完整时，档位金额会采用参考价格补全，并明确显示推定 Token 比例。

金额仅用于本地估算，不是 ChatGPT/Codex 订阅账单或实际扣款。计算口径、兜底规则及定价来源见 [金额估算说明](./docs/PRICE_ESTIMATION.md)。

## 构建、测试与发布

要求 Windows 10/11 与 .NET 8 Desktop Runtime；从源码构建需要 .NET 8 SDK。

```powershell
dotnet build .\src\CodexUsageWidget\CodexUsageWidget.csproj -c Release
dotnet run --project .\tests\CodexUsageWidget.SmokeTests\CodexUsageWidget.SmokeTests.csproj -c Release -- --integration
dotnet run --project .\tests\CodexUsageWidget.LayoutTests\CodexUsageWidget.LayoutTests.csproj -c Release
.\发布.ps1
```

布局测试在 Windows 桌面会话中运行，使用合成数据验证尺寸切换、时间内圈、显示开关、重置卡和不透明度设置；预览 PNG 写入测试的 `bin` 输出目录，不上传真实使用记录。

`发布.ps1` 默认生成依赖本机 .NET 8 Desktop Runtime 的小体积单文件。若要发给未安装 .NET 8 的 Windows 电脑：

```powershell
.\发布.ps1 -SelfContained
```

发布结果位于 `artifacts\publish\CodexUsageWidget.exe`。窗口位置、尺寸、样式、Token 口径和外观设置保存在 `%LOCALAPPDATA%\CodexQuotaWidget\settings.json`。

## 许可证

本项目采用 [MIT License](./LICENSE)。
