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
- `ultrafast`：Ultrafast，当前公开价表仅包含 GPT-6 Astra。

跨档位周期会分段累加，不使用统一倍率替代逐模型价表。

OpenAI 已于 2026 年 7 月 30 日将 Priority processing 更名为 Fast mode。为兼容历史日志，程序继续识别 `priority`，但界面统一显示为 Fast。

GPT-5.6 Sol 当前采用官方促销价格，官方说明该价格至少持续至 2026 年 11 月 21 日。GPT-6 Astra 的 Ultrafast 使用公开单价；其他模型的 Ultrafast 没有匹配公开价格时，以 Astra 的 Ultrafast 价格作为参考并计入“推定比例”，不将其误归为 Standard。

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

参考模型也必须支持相应 Token 类别与上下文长度；Ultrafast 的默认参考模型为当前有公开价格的 `gpt-6-astra`。GPT-5.5／5.4 的 Fast 长上下文价格未在当前价表列出，不擅自外推倍率，而采用上述同档位参考规则并标记推定。

界面中的“覆盖 100%”表示全部 Token 都纳入推算；“推定比例”用于说明其中多少 Token 使用了上述兜底规则。推定金额不等于官方公布的实际收费。

## 限制

- 本地 `service_tier` 表示线程设置，不保证与服务端最终采用的计费档位完全相同。
- 金额不包含 Web Search、容器或其他工具费用。
- 历史日志格式和公开价格可能发生变化。
- 当前程序用本次内置价表重算所选周期，不是按请求发生日期还原历史账单；更新后累积估算金额可能变化。
- 额度和金额应以 OpenAI 官方界面及正式账单为准。

## 定价来源

内置价表核验日期为 `2026-09-30`。新增 GPT-6.1 Sol、GPT-6 Sol、GPT-6 Luna 的 Standard、Flex、Fast，Astra 的 Ultrafast，以及 GPT-5.3 Codex 的 Fast。已有 GPT-5.6 和 Astra 常规价格核对后不变。

以下为新增／补齐项目的短上下文价格，单位为美元／百万 Token；每格按“普通输入／缓存读取／缓存写入／输出”排列：

| 模型 | Standard | Flex | Fast | Ultrafast |
| --- | --- | --- | --- | --- |
| GPT-6.1 Sol | 2 / 0.1 / 2.5 / 10 | 1 / 0.05 / 1.25 / 5 | 4 / 0.2 / 5 / 20 | 未公布 |
| GPT-6 Sol | 2 / 0.2 / 2.5 / 10 | 1 / 0.1 / 1.25 / 5 | 4 / 0.4 / 5 / 20 | 未公布 |
| GPT-6 Luna | 0.1 / 0.01 / 0.125 / 0.5 | 0.05 / 0.005 / 0.0625 / 0.25 | 0.2 / 0.02 / 0.25 / 1 | 未公布 |
| GPT-6 Astra | 10 / 1 / 12.5 / 50 | 5 / 0.5 / 6.25 / 25 | 20 / 2 / 25 / 100 | 60 / 6 / 75 / 300 |
| GPT-5.3 Codex | 1.75 / 0.175 / — / 14 | 未公布 | 3.5 / 0.35 / — / 28 | 未公布 |

GPT-6 系列上述已公布档位：输入不超过 272,000 Token 时使用短上下文价；超过时普通输入、缓存读取、缓存写入为短上下文的 2 倍，输出为 1.5 倍。推理输出不重复计费。GPT-5.4 Flex 的长上下文缓存读取按官方明确的 0.25 计算，而不是将短价 0.13 翻倍为 0.26。

来源：

- [OpenAI API Pricing](https://developers.openai.com/api/docs/pricing)
- [Prompt Caching](https://developers.openai.com/api/docs/guides/prompt-caching#requirements)
- [Reasoning](https://developers.openai.com/api/docs/guides/reasoning#how-reasoning-works)
- [Codex Tokens 与 Credits 说明](https://learn.chatgpt.com/docs/pricing#what-are-tokens-and-credits)
