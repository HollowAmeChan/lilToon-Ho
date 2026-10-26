# lilToon-Ho

基于官方 lilToon 2.4.1，为 VRChat / Warudo 保留上游功能并添加四项小修：

- 描边顶点色修正模式下，默认黑 / 白 RGB 回退到网格法线。
- 三段接收阴影共用一张 `_ShadowReceiveMask`，包括平面阴影路径。
- `_OutlineZBiasMask` 控制描边局部 Z 偏移。
- Material Variant 面板显示父材质、支持定位，并保护继承的 Shader 设置。

UPM Git URL：`https://github.com/HollowAmeChan/lilToon-Ho.git?path=Assets/lilToon#master`。
包名和 GUID 与官方相同；安装时替换已有 lilToon，避免同时安装两份。
新增 Mask 默认白色，保留原有效果。顶点色修正仍由 A 通道控制描边宽度。

支持 Git UPM 与本地 `file:` 包安装，编辑器的文件读写会解析到 UPM 实际包目录。
从 VCC/VPM 版本切换时，先备份并移出 `Packages/jp.lilxyzw.liltoon`，同时移除
`Packages/vpm-manifest.json` 中该包的 `dependencies` 和 `locked` 记录，再添加 UPM 依赖。
已有嵌入包会优先于 Git 依赖；保留 VPM 记录也可能让官方包在后续解析时被重新安装。

来源、范围和验证记录见 [Ho 移植任务清单](docs/Ho移植任务清单.md)。以下为上游说明与文档入口。

## English
Drag and drop unitypackage to the Unity window, or import `https://github.com/lilxyzw/lilToon.git?path=Assets/lilToon#master` from UPM.  
[Download](https://github.com/lilxyzw/lilToon/releases) / [Documentation](https://lilxyzw.github.io/lilToon/index.html#/en-us/) / [How to distribute your works that use lilToon](https://lilxyzw.github.io/lilToon/#/en-us/first?id=how-to-distribute-your-works-that-use-liltoon)

## 日本語
unitypackageをUnityウィンドウにドラッグ＆ドロップ、もしくはUPMから`https://github.com/lilxyzw/lilToon.git?path=Assets/lilToon#master`をインポートしてください。  
[ダウンロード](https://github.com/lilxyzw/lilToon/releases) / [ドキュメント](https://lilxyzw.github.io/lilToon/index.html#/ja-jp/) / [lilToonを用いた制作物の配布手順について](https://lilxyzw.github.io/lilToon/#/ja-jp/first?id=liltoonを用いた制作物の配布について)
