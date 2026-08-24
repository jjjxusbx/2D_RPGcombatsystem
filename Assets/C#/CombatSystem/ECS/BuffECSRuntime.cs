using Unity.Entities;
using UnityEngine;

// ============================================================
// Buff ECS 运行时装配入口（组合根）
//
// 在本项目里，Buff 系统被建模为独立的 DOTS 子系统。这里把相关系统
// 注册进「默认世界」的 SimulationSystemGroup，使它们随主循环每帧更新。
//
// 注意：需要项目安装 Unity Entities（com.unity.entities）。若你的工程
// 尚未安装该包，本目录（含 .asmdef）将无法编译，需先在 Package Manager
// 安装 Entities 版本（与 Unity 6 配套的 Entities 1.x）。
// ============================================================

namespace Game.ECS.Buff
{
    public static class BuffEcsRuntime
    {
        static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            if (_registered)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            // 让 Buff 系统每帧随 SimulationSystemGroup 更新。
            world.GetOrCreateSystemManaged<BuffPositionSyncSystem>();
            world.GetOrCreateSystemManaged<BuffManagementSystem>();
            world.GetOrCreateSystemManaged<TickBuffSystem>();
            world.GetOrCreateSystemManaged<AttributeRefreshSystem>();
            world.GetOrCreateSystemManaged<AuraSearchSystem>();
            world.GetOrCreateSystemManaged<AOETriggerSystem>();

            _registered = true;
        }
    }
}
