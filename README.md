# Endless Racing Demo

Unity 6 的极简竞速 demo：柏林噪声程序化生成无限赛道，支持漂移甩尾、
轮胎痕迹、过门加分、撞车结算与最高分记录。玩法逻辑全部是纯 C#，不依赖
任何脚本热更框架。

## 运行

1. 使用 Unity 6000.3 打开项目；
2. 打开 `Assets/Scenes/MainMenu.unity`；
3. 点击 Play（游戏内按 Enter 或点击开始按钮进入赛道）。

## 玩法

- 转向：A/D（或左右方向键）拨动水平轴，或按住鼠标左键点击屏幕左/右半区；
- 穿过金色门框 +100 分，每秒自动 +1 分；
- 撞上障碍物游戏结束，结算页会记录并保存最高分；
- 漂移时车辆压到草地会扬起粒子并留下轮胎痕。

## 结构

```
Assets/
├─ Scenes/                 主菜单与游戏场景
├─ Script/                 全部玩法逻辑（纯 C#）
│  ├─ MainMenu.cs          主菜单：Enter / 按钮进游戏
│  ├─ GameManager.cs       计时、计分、结算与最高分
│  ├─ Car.cs               输入、转向、漂移痕迹
│  ├─ WorldGenerator.cs    程序化生成无限赛道
│  ├─ BasicMovement.cs     地块移动与配合转弯
│  ├─ Gate.cs / Obstacle.cs
│  ├─ CameraFollow.cs      第三人称平滑跟拍
│  └─ Music.cs             跨场景音乐
├─ Procedural Racing/      美术、动画、预制体与音频
└─ Settings/               URP 渲染管线配置
```

## 技术点

- 程序化地形：圆柱网格按柏林噪声逐块生成，相邻地块按上一块末排顶点插值
  做无缝过渡，地块移出视野后销毁并向前补新块，形成无限赛道；
- 纯 C# 玩法层：各场景组件直接实现游戏规则，组件之间通过公开方法互相
  汇报事件（比如 Gate 加分走 `Gate → GameManager.UpdateScore`），
  场景里只序列化参数，不藏逻辑；
- 输入使用 InputSystem 生成的 ActionAsset，同时保留旧 Input 处理菜单按键。
