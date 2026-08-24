# 文档索引

> 本文件提供项目所有文档的快速导航。

---

## 📘 核心设计文档

| 文档 | 用途 | 更新日期 |
|------|------|---------|
| [README.md](../README.md) | 项目入口（快速开始/架构概览/版本路线图） | 2026-08-24 |
| [核心框架设计.md](核心框架设计.md) | v0.1 架构决策 + 装配链 + 诊断契约 | 2026-08-10 |
| [版本计划.md](版本计划.md) | 里程碑规划 + 验收清单 + 复盘模板 | 2026-08-24 |
| [测试报告.md](测试报告.md) | 编译/功能/兼容/性能/安全验证 | 2026-08-10 |
| [AGENTS.md](../../AGENTS.md) | 项目能力盘点 + 可复用边界 + 已知问题 | 2026-08-24 |

---

## 🎮 游戏设计文档

| 文档 | 用途 | 阅读对象 |
|------|------|---------|
| [策划流程与双档方案.md](策划流程与双档方案.md) | 策划方法论 + 极简/标准版 Rogue 双档方案 | 策划 + 主策 |
| [UI作品集设计说明.md](UI作品集设计说明.md) | 6 个界面设计决策 + 视觉体系 | UI/UX 设计师 |
| [战斗属性ECS框架.md](战斗属性ECS框架.md) | Buff ECS 组件设计 | 系统程序员 |

---

## 🛠️ 系统配置文档

| 文档 | 用途 | 阅读对象 |
|------|------|---------|
| [怪物巡逻系统使用与配置文档.md](怪物巡逻系统使用与配置文档.md) | NavMesh 巡逻参数 + 场景配置 | 关卡策划 + 开发 |
| [配置步骤.md](配置步骤.md) | 场景布线清单（Rogue 房间 + 流程） | 关卡策划 |

---

## 📋 PRD 产品需求文档

| 文档 | 版本 | 状态 | 更新日期 |
|------|------|------|---------|
| [v0.2-PRD-输入系统迁移.md](v0.2-PRD-输入系统迁移.md) | v0.2 | 📋 PRD 完成，待排期开发 | 2026-08-24 |

---

## 📂 Spec 驱动开发目录

位于 [`.trae/specs/`](../../.trae/specs/)，包含功能规格与任务拆解：

| Spec | 用途 | 状态 |
|------|------|------|
| `p0-code-cleanup/` | 代码清理（探针/死代码/警告） | ✅ 已完成 |
| `create-monster-patrol/` | 怪物巡逻系统初始化 | ✅ 已完成 |
| `arog-playable-movement-controller/` | 可玩角色移动控制器 | 📋 进行中 |

---

## 🔗 外部资源

- **Unity 官方文档**: https://docs.unity3d.com/6000.0/Documentation/Manual/
- **Input System 文档**: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/
- **Cinemachine 文档**: https://docs.unity3d.com/Packages/com.unity.cinemachine@2.9/manual/
- **AI Navigation 文档**: https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/

---

**维护规则**:
- 每个新功能/PRD 完成后更新本索引
- Spec 目录新增功能需在此添加条目
- 文档重大变更需更新"更新日期"列
