# lilToon-Ho：基线与移植任务清单

记录日期：2026-10-26。当前阶段：**H01–H04 已实现并通过验证。**

## 1. 新仓库基线

| 项目 | 内容 |
| --- | --- |
| 本地目录 | `D:\Unity_Fork\lilToon-Ho` |
| 官方来源 | https://github.com/lilxyzw/lilToon.git |
| 官方远程名 | `upstream` |
| 基线分支 / 标签 | `upstream/master` / `2.4.1` |
| 基线提交 | `e698a2f791bd28dd1aabf227c74f14bedeca3587` |
| 上游提交日期 | 2026-10-20，`fix: Incorrect light probe brightness #378` |
| 包版本依据 | `Assets/lilToon/package.json`：`2.4.1`，Unity 最低版本声明为 `2022.3` |
| 发布分支 | `master`；开发提交保留在 `codex/ho-port-inventory` |
| GitHub 仓库 | https://github.com/HollowAmeChan/lilToon-Ho |

拉取时已用 `git ls-remote upstream HEAD` 核对官方基线。根目录旧 `version.json` 仍写着 `2.1.1`，不能以它判断此次版本。此仓库保留官方 Git 历史；GitHub 上是独立派生仓库，未覆盖原有 `HollowAmeChan/lilToon`。

定位：面向 VRC / Warudo 的 lilToon 小修版，沿用官方功能与渲染管线支持。旧 fork 的基线是 2.3.2，不直接用旧 fork 的当前文件覆盖新版。

功能变更分别提交，便于审查和合并上游更新。保留上游包名和 GUID。

## 2. 已明确的四项任务

以下四项已经用户确认，2026-10-26 开始实施。

| 任务 | 实现提交 | 状态 |
| --- | --- | --- |
| H01 顶点色描边兜底 | `988c92c` | 已实现 |
| H02 公共接收阴影 Mask | `2424cc8` | 已实现，包含平面阴影分支 |
| H03 描边 Z 偏移 Mask | `fb42406` | 已实现，普通 / Lite / Multi 接线 |
| H04 Material Variant 面板 | `48f2c48` | 已实现，含混选、嵌套变体与旧版 API 隔离 |

下文“2.4.1 现状”描述官方基线，历史来源保留用于追溯。

### H01：描边顶点色修正模式的空颜色兜底

- 历史来源：旧 `D:\Unity_Fork\lilToon` 的 `da8fb3f`，2026-05-05，`给rgba修正描边做了无顶点色排除`。
- 旧实现：新增 `lilGetOutlineVertexColorVector`，RGB 接近全黑（各分量 <= 0.0001）或全白（各分量 >= 0.9999）时使用网格原法线；其余仍按 `color.rgb * 2 - 1` 解码。
- 触点：`Assets/lilToon/Shader/Includes/lil_common_functions.hlsl`，普通与 Lite 两套描边位置计算。
- 2.4.1 现状：两处仍直接解码 RGB，没有上述兜底。
- 边界：这是按颜色值识别默认值的启发式，不是真正检测网格是否有 COLOR 通道。全黑 / 全白的有效编码也会被视为默认值。原提交保留 A 通道控制描边宽度；不能把它描述成修复了所有 RGBA 缺失情况。
- 后续验收：无 COLOR 通道、默认白、默认黑、部分顶点有修正色、有效修正色、A 通道宽度控制；普通 / Lite 描边分别检查。

### H02：三段接收阴影共用一张 Mask

- 历史来源：`760d117`（2026-05-02，最初添加三张）与 `b3195ab`（2026-05-11，`接收阴影蒙版三合一，UI显示提前`）。
- 注意：`760d117` 的标题写的是 shadowcast，实际 diff 修改的是 **receive shadow**，不是投影裁切 Mask。
- 目标：只保留 `_ShadowReceiveMask`，采样 R 通道。第一、二、三段分别使用自身接收强度乘同一个 Mask；仍保留三个强度滑块。
- 约定：默认白图保持旧材质行为，黑色区域不接收该路阴影，灰色连续调节。面板只有一个公共贴图槽，位于三段影色设置之前。
- 不新增 `_Shadow2ndReceiveMask` / `_Shadow3rdReceiveMask`，不先移植三张再清理。
- 触点：`CustomShaderResources/Properties/Default.lilblock`、`DefaultAll.lilblock`；Inspector 属性绑定与阴影分组；`lilToonSetting.cs` 的特性开关、贴图扫描和动画属性检测；`lil_common_input.hlsl`、`lil_common_frag.hlsl`、`lil_replace_keywords.hlsl`。
- 2.4.1 现状：无上述 Mask；三段仍直接使用 `_ShadowReceive` / `_Shadow2ndReceive` / `_Shadow3rdReceive`。
- 已补齐的历史遗漏：上游还有 `_ShadowMaskType == 1` 的平面阴影分支，`lnFlat` 单独使用 `_ShadowReceive`；历史三合一改动未覆盖这一处。本次已让它使用公共 Mask，并通过 GPU 像素测试验证。
- 后续验收：空槽 / 白 / 黑 / 灰、三段分别启用、平面阴影模式、普通 / Multi、特性裁剪、已有材质。Lite 原本走简化阴影流程，不凭空承诺三段控制。

### H03：描边 Z 偏移 Mask

- 历史来源：`14ff18d`，2026-09-05，`描边z偏移蒙版`。
- 目标：新增 `_OutlineZBiasMask`，默认白；在顶点阶段以 UV0、LOD 0 采样 R 通道，最终 Z 偏移为 `_OutlineZBias * mask.r`。白色保留原偏移，黑色关闭局部偏移。
- 旧实现覆盖普通 / Lite，且保持 ShadowCaster 和 URP DepthOnly 原有的零偏移规则。
- 触点：`Default` / `DefaultAll` / `DefaultLite` 属性模板；普通 / Lite 描边位置函数及调用；纹理声明和特性宏；Inspector 属性、显示与本地化；特性扫描、Lite 材质转换、无用贴图清理。
- 2.4.1 现状：只有 `_OutlineZBias` 标量，无 Mask。
- 移植时提取功能代码；原提交顺带修改的 `Editor/CurrentRP.txt` 不作为功能需求移植。
- 后续验收：空槽 / 白 / 黑 / 渐变、正负偏移、普通 / Lite / Multi、透视 / 正交、ShadowCaster / DepthOnly、VR 双眼显示。

### H04：Material Variant 面板支持

- 2026-10-26 用户追加确认。来源：`0a70470`。
- 面板展示父材质并支持定位；子材质继续编辑贴图、颜色和数值覆写。
- Shader、渲染模式、透明子模式、描边启用由父材质控制；阻止在变体上触发切换 Shader 的操作。
- 批量编辑包含变体时也要保护继承关系；独立材质照常工作。保留官方 Unity 版本兼容条件，新增文案本地化。
- 验收：独立材质、父材质、子变体、嵌套变体、独立材质与变体混选；父材质不被子材质操作改写，普通参数覆写仍有效。

建议实施顺序：H01 → H02 → H03 → H04，每项单独提交。改动模板和共享实现后，通过 lilToon 的生成流程验证 Shader，不能只改生成的 `.shader` 文件。

## 3. 额外找到的候选项（未决定纳入，未实施）

| 候选项 | 旧仓库来源提交 | 调查结论与依赖 |
| --- | --- | --- |
| 描边阴影强度 | `5b91fe0`、`bc80e4b`、`bd47087` | `_OutlineShadowStrength` 复用主体 toon 阴影；后两项分别补 Multi 声明、修复背面强制阴影把倒壳描边压成全暗。可单独评估；需剥离后续 HoAO 联动，不能只搬第一条。 |
| 本地化在 file: 包安装下的修复 | `16649b5` | 修复虚拟 Packages 路径被当成磁盘路径、中文区域名与 .po 名称不匹配、列语言文件方式。2.4.1 仍有 `File.Exists` / `Directory.GetFiles` 对虚拟资源路径的使用，值得作为小修优先评估；需两种安装方式实测。 |
| 避免重复生成和导入未变化的 Shader | `edf2b33` | 比较内容后再写配置和生成文件，仅导入变化项；启动时避免无意义重写 RP 记录。属于编辑器优化，需保留官方全部管线及自定义 .lilcontainer 导入行为。 |
| Inspector Shader 查找缓存 | `3f5633c` | 缓存 Shader.Find，资源变化后失效；可能适合小修，但历史文件含自定义 Shader 家族，不能整文件复制。 |
| 材质贴图搜索、角色匹配与暂存应用 | `a4c5a1f`、`e2dcf51`、`0f3d26b`、`f2f43ce`、`cbfce96` 等 | 需要一组 UI 后续修复，规模明显超过三项小修，先保留线索。 |
| MPB 覆写检查 | `69cb227`、`9797ceb` | 场景 Renderer 与参数的只读检查；后续改为按需扫描。依赖旧新版 Inspector 结构，作为工具候选。 |
| 场景级批量材质管理器 | `8760c3d` 起，后续至 `fd89272` 等 | 独立工具，含多选、属性分组、混值与广播修复链；不是单条可移植小补丁，待单独决定。 |
| 厚度图假 SSS | `3fa41d5` | 基于厚度图和背光的材质效果；原接线面向 URP。跨 BRP / Warudo 需重新验证，先作为较大扩展候选。 |
| HDR / 多光源增强 | `7c963a0`、`699051c` | 旧实现主要面向 URP。官方后来有 `252fd8c`（2.3.4）将 URP 附加光移入像素阶段，必须先比对新版，避免覆盖官方改进；不列入首批。 |

这些只是历史功能候选，记录并不代表已同意全部加入。没有把旧 fork 的 UI 整体重构、裁剪计划或完整渲染体系当作此次需求。

## 4. 与此次小修定位不符的历史改动

- **URP 固定与功能剥离**：`d310513`、`d94ae6e`、`4c73861`、`073542d`、`d34d2f0`、`7864022`；会移除非 URP 路径、AudioLink、VRChat fallback / SDK、VRC Light Volumes、LTCGI、ID Mask、UDIM 或细分等能力，与此次用途相冲突。
- **旧 fork 后续删除 Shader 家族、转换器、优化工具**：如 `42a71b4`、`18f3133`、`2eef4bc`、`b1d40c3`；此次不照搬裁剪。
- **Ho 管线联动**：HoAO / HTrace、ObjectBuffer、SurfaceBuffer、GeometryData 描边修正、独立 CharacterShadow、PLR / OIT、角色特化与 Tesion 等；依赖隔壁渲染扩展或自定义管线，未列入此次小修任务。
- **SSGI 描边白边调查**：隔壁 `Documentation~/Ho-已知问题-描边SSGI白边.md` 记录了实验回退及当时暂不修复结论；不能当作一个已完成的小修移植。该问题与 H03 的 Z 偏移 Mask 是两件事。
- **RP 自动生成产物**：如 `ea076da` 中的大批生成 Shader 与 `CurrentRP.txt` 差异；这些不是独立原生增强功能。

## 5. 调查来源与后续验证范围

- 主要来源：`D:\Unity_Fork\lilToon` 的本地完整 Git 历史，调查时 HEAD 为 `b124f7e`。该仓库有既存未提交修改，本轮没有改动它。
- 补充来源：`D:\Unity_Fork\lilToon-URP-Extensions` 的提交历史和相关文档，调查时 HEAD 为 `3dc98144`。
- 新旧代码核对：已核对三项核心功能的真实 diff 与官方 2.4.1 当前实现，而非只按提交标题判断。旧 `LILTOON魔改流程案例.md` 还描述三张接收阴影 Mask；以之后 `b3195ab` 的实际代码为准。
- 首轮完成 Git 基线与静态调查；第二轮实施 H01–H04，验证记录见下节。
- 优先验证 VRC 使用的 BRP 路径；按本地 Warudo 工程的 Unity 2021.3 版本补查旧 API 兼容。上游其余管线路径保留，本轮未声称已经完成 URP / HDRP / XR 实机验证。

当前明确实施范围：**H01、H02、H03、H04**。其余候选项保持待选。

## 6. 2026-10-26 验证记录

- 隔离工程，BRP、Windows D3D11，未修改现有 VRC / Warudo 工程。
- Unity **2022.3.22f1**：65 项通过。调用官方生成器；普通、描边、Cutout 描边、Transparent 描边、Lite 描边、Multi、Multi 描边的各 Pass 同步编译；检查生成的公共 Mask 属性、无多余第二 / 三张 Mask 属性。
- Unity **2021.3.45f2**：55 项通过。相同 Shader 编译、GPU 像素与特性扫描；该版本没有 Material Variant API，相关检查按条件跳过，生产代码已通过该版本编译。
- 2021.3.18f1 在启动阶段受本机许可证限制，改用本机另一 Warudo 工程所用的 2021.3.45f2 完成验证，未改动许可证或工程配置。
- GPU 检查直接调用生产 HLSL：默认黑 / 白顶点色回退与有效 RGB 解码；顶点阶段 Z Mask 白 / 黑 / 灰、负偏移；三段共用接收阴影 Mask；平面阴影分支；裁掉 Mask 特性后保持原标量行为。
- Material Variant 检查使用实际持久化父材质、子变体与嵌套变体：Shader 切换保护、混选检测、Multi 转换保护、颜色 / 数值 / 贴图覆写、父材质变更后的继承传播。
- 同步 63 份预生成 Shader，135 行新增，提取已验证生成结果中的新属性与宏；保留上游其余生成配置（VRC Light Volumes、变体裁剪等），不把隔离工程的环境差异带入仓库。
- 使用 `-ho-verify-generated` 再次验证实际交付的预生成 Shader，Unity 2022.3.22f1 的 65 项检查全部通过。
- 验证脚本及运行说明位于 `Tests~/`，Unity 安装包不会默认导入该测试目录。
- 未执行 VRC 上传、Warudo 运行时加载、交互式 Inspector 点击或 VR 设备测试。

## 7. UPM 路径修复与 VCC 工程接入（2026-10-27）

- 修复提交：`8e6e3c2`。在 `System.IO` 边界使用 `PackageInfo.resolvedPath` 解析包虚拟路径，保留 AssetDatabase 和 Shader include 使用的虚拟路径。
- 覆盖包版本读取、启动 RP 记录、Shader 模板读取与生成、优化输入文件、贴图读取、配置锁和诊断信息读取。
- 本地化通过 AssetDatabase 枚举、加载 .po，支持 zh-CN → zh-Hans 等区域名映射。
- Unity 2022.3.22f1 隔离工程的本地 UPM 模式：9 项 UPM 检查及 65 项完整回归检查通过。
- Git UPM 安装验证的批处理被自动审批拒绝，工具仅返回 `blocked by policy`，未提供具体原因；本轮没有声称已经完成 Git 来源的运行验证。
- `VCC_Break_VRC` 已从 VPM lilToon 2.3.4 切为本地 UPM 依赖：`file:D:/Unity_Fork/lilToon-Ho/Assets/lilToon`。此方式直接使用新仓库，不依赖 GitHub 下载；在另一台机器上需要调整本地路径或完成 Git UPM 验证后改用 Git URL。
- VPM 中 lilToon 的 dependencies / locked 记录已移除，其他依赖保留，UPM lock 由 Unity 重新生成。
- 在该工程正在运行的 Unity 中完成验证：2.4.1 包解析正确；现有 12 个材质的 Shader GUID 保持一致；普通、描边、Cutout 描边、Transparent 描边、Lite 描边、Multi、Multi 描边的各 Pass 编译通过；五种语言均能加载。
- 旧嵌入包和依赖配置备份在 `D:/runtime_scratch/liltoon-ho-vcc-migration-20261026`；临时编辑器验证脚本已移出工程，验证报告为该目录中的 `success.txt`。
