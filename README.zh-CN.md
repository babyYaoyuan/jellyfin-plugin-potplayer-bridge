# Jellyfin PotPlayer Bridge

[![Build](https://github.com/babyYaoyuan/jellyfin-plugin-potplayer-bridge/actions/workflows/build.yml/badge.svg)](https://github.com/babyYaoyuan/jellyfin-plugin-potplayer-bridge/actions/workflows/build.yml)
[![License: GPL-3.0](https://img.shields.io/badge/license-GPL--3.0-blue.svg)](LICENSE)

[English](README.md)

直接在 Jellyfin Web 的电影或单集详情页唤起 PotPlayer。无需油猴脚本、浏览器扩展、
Windows 常驻助手或 SMB 盘符映射。

> 这是非官方社区插件，与 Jellyfin 和 PotPlayer 官方无隶属关系，也不包含或分发
> PotPlayer。

## 功能

- 在 Jellyfin 原播放按钮旁增加独立的黄色 **使用 PotPlayer 播放**按钮。
- 通过支持 HTTP Range 的地址把媒体库原文件交给 PotPlayer，不转换、不转码。
- 服务端只需安装本插件；Windows 客户端只需正常安装 PotPlayer，并保留其自带的
  `potplayer:` 协议关联。
- 外部播放器地址中不包含 Jellyfin Access Token。
- 使用随机、限时、单媒体条目票据，默认绑定发起请求的客户端 IP。
- 前端脚本在响应内存中注入，不修改 Jellyfin Web 的磁盘文件。
- 兼容 Chromium 130 及之后版本对自定义协议 URL 的解析变化。

## 兼容范围

- Jellyfin Server 10.11.x（`targetAbi` 10.11.0.0）
- Jellyfin Web 的电影和单集详情页
- 已安装 PotPlayer 并注册 `potplayer:` URL 协议的 Windows 客户端

第一次点击时，浏览器会要求确认“打开 PotPlayer”。这是浏览器的安全机制，服务端
插件无法绕过。

## 通过插件源安装

1. 打开 Jellyfin **控制台 → 插件 → 存储库**。
2. 新增名为 `PotPlayer Bridge` 的存储库，地址填写：

   ```text
   https://raw.githubusercontent.com/babyYaoyuan/jellyfin-plugin-potplayer-bridge/main/manifest.json
   ```

3. 进入**目录**，安装 **PotPlayer Bridge**，然后重启 Jellyfin。
4. 重启后对 Jellyfin Web 强制刷新一次。

## 手动安装

1. 从最新 GitHub Release 下载 ZIP。
2. 将 `Jellyfin.Plugin.PotPlayerBridge.dll` 解压到 Jellyfin 插件目录下的新文件夹：

   ```text
   /config/plugins/PotPlayerBridge_0.1.0.0/
   └── Jellyfin.Plugin.PotPlayerBridge.dll
   ```

3. 确保 Jellyfin 运行用户可以读取 DLL。
4. 重启 Jellyfin，并对 Jellyfin Web 强制刷新一次。

Docker 手动安装时不要复制源码中的 `meta.json`；安装目录里的清单文件应由 Jellyfin
自己创建和维护。

## 工作原理

1. 插件在 Jellyfin Web 首页响应中注入一段内置前端脚本。
2. 脚本在支持的详情页加入 PotPlayer 按钮。
3. 当前已登录的 Jellyfin 用户为目标媒体申请一个限时票据。
4. 按钮通过 PotPlayer 已有的 Windows 协议关联打开 `potplayer:http://...`。
5. PotPlayer 以 HTTP Range 方式读取原文件，因此可以拖动进度且无需 Jellyfin 转码。

这里使用 `potplayer:http://...` 是有意设计。Chromium 130 以后会把传统的
`potplayer://http://...` 规范化成错误的 `potplayer://http//...`。

## 配置

打开 Jellyfin **控制台 → 插件 → PotPlayer Bridge**：

- **媒体票据有效期：** 5～1440 分钟，默认 720 分钟。
- **绑定客户端地址：** 默认开启，建议保持开启。

如果可信反向代理导致浏览器和 PotPlayer 在 Jellyfin 看来具有不同来源地址，可以
关闭地址绑定；条件允许时，更建议正确配置代理转发的客户端地址。

## 安全设计

- 票据包含 256 位随机值。
- 每张票据只对应 Jellyfin 媒体库解析出的一个现有文件，客户端不能提交任意路径。
- 创建票据需要已认证且有权访问该媒体的 Jellyfin 用户。
- PotPlayer 地址不携带 Jellyfin Access Token。
- 响应禁止缓存，并受票据有效期限制。

安全问题与公网部署说明请参阅 [SECURITY.md](SECURITY.md)。

## 当前限制

- 暂不向 Jellyfin 回传 PotPlayer 播放进度。
- 暂不自动挂载外挂字幕；媒体文件内嵌字幕可以正常使用。
- 暂不支持播放列表、自动下一集和多版本选择。
- 不加载 Jellyfin Web 的原生电视/移动客户端不会显示按钮。
- 未来 Jellyfin Web DOM 变化可能需要更新插件。

## 构建

安装 .NET SDK 9，然后执行：

```powershell
dotnet restore .\PotPlayerBridge.slnx
dotnet test .\PotPlayerBridge.slnx -c Release --no-restore
dotnet publish .\src\Jellyfin.Plugin.PotPlayerBridge\Jellyfin.Plugin.PotPlayerBridge.csproj `
  -c Release --no-restore -o .\artifacts\publish
```

## 许可证

GPL-3.0-only，详见 [LICENSE](LICENSE)。
