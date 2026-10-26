# lilToon-Ho：基线与移植任务清单

记录日期：2026-10-26。当前阶段：**已确认 H01–H04，开始逐项移植。**

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
| 当前工作分支 | `codex/ho-port-inventory` |
| GitHub 仓库 | https://github.com/HollowAmeChan/lilToon-Ho |

已用 `git ls-remote upstream HEAD` 再次核对，远程 HEAD 与本地基线一致。根目录旧 `version.json` 仍写着 `2.1.1`，不能以它判断此次版本。

定位：面向 VRC / Warudo 的 lilToon 小修版，沿用官方功能与渲染管线支持。旧 fork 的基线是 2.3.2，不直接用旧 fork 的当前文件覆盖新版。

功能变更分别提交，便于审查和合并上游更新。保留上游包名和 GUID。

## 2. 已明确的四项任务

以下四项已经用户确认，2026-10-26 开始实施。

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
- 实现前需处理的遗漏：上游还有 `_ShadowMaskType == 1` 的平面阴影分支，`lnFlat` 单独使用 `_ShadowReceive`；历史三合一改动未覆盖这一处。移植时需明确它也使用公共 Mask，防止另一条路径继续接收阴影。
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
- 本轮完成的是 Git 基线与静态源码调查，没有执行 Unity 编译、VRC 上传或 Warudo 运行验证，也不声称候选改动已经适配这些环境。
- 开始实现后，优先验证 VRC 使用的 BRP 路径；按实际 Warudo 工程的 Unity 版本和管线验证对应路径。保留上游其余路径，涉及共享函数签名的变更同时检查普通、Lite、Multi 和相关 pass。

当前明确实施范围：**H01、H02、H03、H04**。其余候选项保持待选。
