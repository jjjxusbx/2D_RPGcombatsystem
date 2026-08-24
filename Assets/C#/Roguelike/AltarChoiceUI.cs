using UnityEngine;
using UnityEngine.UI;
using Roguelike.Run;
using Roguelike.Flow;

namespace Roguelike.UI
{
    /// <summary>单个祭坛候选强化（在内存中构成三选一）。</summary>
    [System.Serializable]
    public class RogueAltarOption
    {
        public string statId;   // 对应 ChaState 属性: Atk / MaxHealth / AttackRate / MoveSpeed / Growth
        public ModifierType type;
        public float value;
        public string label;    // 展示文本

        public RogueAltarOption(string statId, ModifierType type, float value, string label)
        {
            this.statId = statId;
            this.type = type;
            this.value = value;
            this.label = label;
        }
    }

    /// <summary>
    /// 祭坛三选一面板：从 5 项基础强化中随机抽 3 项展示，选中后经 RunManager.ApplyRoomBuff 施加，
    /// 并通知 RogueRoomFlowController 推进房间。UI 元素由编辑器装配工具创建并回填引用。
    /// </summary>
    [DisallowMultipleComponent]
    public class AltarChoiceUI : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private RogueRoomFlowController flow;

        [Header("UI 引用（长度 3）")]
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Text[] choiceLabels;

        [Header("基础强化池（随机抽 3）")]
        [SerializeField] private string[] statIds = { "Atk", "MaxHealth", "AttackRate", "MoveSpeed", "Growth" };
        [SerializeField] private ModifierType[] types =
        {
            ModifierType.AddValue, ModifierType.AddValue,
            ModifierType.PercentAdd, ModifierType.AddValue, ModifierType.PercentAdd
        };
        [SerializeField] private float[] values = { 5f, 25f, 0.15f, 1f, 0.2f };
        [SerializeField] private string[] labels = { "攻击 +5", "生命上限 +25", "攻速 +15%", "移速 +1", "成长 +20%" };

        private RogueAltarOption[] _current;

        public void Show()
        {
            _current = PickThree();
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    int index = i;
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoose(index));
                }

                if (choiceLabels[i] != null && _current[i] != null)
                {
                    choiceLabels[i].text = _current[i].label;
                }
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnChoose(int index)
        {
            if (_current == null || index < 0 || index >= _current.Length || _current[index] == null)
            {
                return;
            }

            RogueAltarOption opt = _current[index];
            AttributeModifier mod = new AttributeModifier(opt.statId, opt.type, opt.value);

            RogueRoomFlowController controller = flow != null ? flow : FindObjectOfType<RogueRoomFlowController>();
            if (controller != null)
            {
                // OnAltarChosen 内部完成 ApplyRoomBuff 与房间推进，避免重复施加。
                controller.OnAltarChosen(mod);
            }
            else
            {
                RunManager rm = runManager != null ? runManager : FindObjectOfType<RunManager>();
                rm?.ApplyRoomBuff(mod);
            }

            Hide();
        }

        /// <summary>从池中随机抽 3 个不重复选项（池不足 3 时取实际个数）。</summary>
        private RogueAltarOption[] PickThree()
        {
            int count = Mathf.Min(3, statIds.Length);
            var result = new RogueAltarOption[count];

            bool[] used = new bool[statIds.Length];
            for (int i = 0; i < count; i++)
            {
                int pick = Random.Range(0, statIds.Length);
                int guard = 0;
                while (used[pick] && guard++ < statIds.Length)
                {
                    pick = Random.Range(0, statIds.Length);
                }
                used[pick] = true;

                result[i] = new RogueAltarOption(statIds[pick], types[pick], values[pick], labels[pick]);
            }

            return result;
        }
    }
}
