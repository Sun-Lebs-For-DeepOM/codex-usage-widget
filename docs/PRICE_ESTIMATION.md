# 金额估算说明

悬浮窗显示的金额是本地 API 等价估算，不是 ChatGPT/Codex 订阅账单、Credits 扣款或发票金额。

## 两种金额口径

### Standard API 等价金额

按每段日志记录的模型，以及输入、缓存读取、缓存写入和输出 Token，使用公开 Standard API 单价计算。没有公开匹配单价的部分不猜测价格，界面会显示相应覆盖率。

### 线程速度档位估算

独立读取本地日志中的线程档位，按模型和档位分别计算：

- `default`、`standard`：Standard；
- `priority`、`fast`：Fast；
- `flex`：Flex。

跨档位周期会分段累加，不使用统一倍率替代逐模型价表。

OpenAI 已于 2026 年 7 月 30 日将 Priority processing 更名为 Fast mode。为兼容历史日志，程序继续识别 `priority`，但界面统一显示为 Fast。

GPT-5.6 Sol 当前采用官方促销价格，官方说明该价格至少持续至 2026 年 11 月 21 日。`ultrafast` 已出现在接口档位枚举中，但当前公开定价页未列出其可核验价格；若日志出现该值，程序会将其作为未知档位按 Standard 推定，并计入“推定比例”。

## Token 分类

- 普通输入：输入 Token 扣除缓存读取后的部分；缓存写入包含在此显示类别中。
- 缓存读取：`cached_input_tokens`。
- 可见输出：输出 Token 扣除推理 Token。
- 推理输出：`reasoning_output_tokens`。

推理 Token 已包含在输出 Token 中，计算金额时不会重复相加。

## 全覆盖与推定比例

速度档位金额需要覆盖历史日志中的全部 Token，因此按以下顺序处理：

1. 模型和档位都有公开价格时，直接使用对应价表。
2. 日志缺少档位或档位无法识别时，按 Standard 推定。
3. 内部模型、未知模型或缺少公开缓存写入价格时，使用 `~\.codex\config.toml` 顶层 `model` 指向的公开模型作为同档位参考价。
4. 配置模型不可用时，使用 `gpt-5.6-terra` 作为默认参考模型。

界面中的“覆盖 100%”表示全部 Token 都纳入推算；“推定比例”用于说明其中多少 Token 使用了上述兜底规则。推定金额不等于官方公布的实际收费。

## 限制

- 本地 `service_tier` 表示线程设置，不保证与服务端最终采用的计费档位完全相同。
- 金额不包含 Web Search、容器或其他工具费用。
- 历史日志格式和公开价格可能发生变化。
- 额度和金额应以 OpenAI 官方界面及正式账单为准。

## 定价来源

内置价表核验日期为 `2026-09-07`。当前价表包含 GPT-6 Astra，以及 GPT-5.6 Sol、Terra、Luna 的 Standard、Flex 和 Fast 价格；超过 272K 输入 Token 的单次请求按官方长上下文价格计算：

- [OpenAI API Pricing](https://developers.openai.com/api/docs/pricing)
- [Prompt Caching](https://developers.openai.com/api/docs/guides/prompt-caching#requirements)
- [Reasoning](https://developers.openai.com/api/docs/guides/reasoning#how-reasoning-works)
- [Codex Tokens 与 Credits 说明](https://learn.chatgpt.com/docs/pricing#what-are-tokens-and-credits)
