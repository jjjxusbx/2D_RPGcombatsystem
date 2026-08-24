using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Roguelike.Run;

namespace Roguelike.UI
{
    /// <summary>
    /// 局会话结算面板：展示本局碎片 / 击杀数 / 结算晶核，并提供「再开一局」与「回营地」。
    /// 结算晶核经 RunCurrencyStore（无状态、版本化、原子写）持久化。
    /// </summary>
    [DisallowMultipleComponent]
    public class SettlementUI : MonoBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private Roguelike.Flow.RogueRoomFlowController flow;

        [Header("文本引用")]
        [SerializeField] private Text resultText;
        [SerializeField] private Text fragmentText;
        [SerializeField] private Text killText;
        [SerializeField] private Text crystalText;

        [Header("按钮引用")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button campButton;

        private void Awake()
        {
            if (runManager == null) runManager = FindObjectOfType<RunManager>();
            if (flow == null) flow = FindObjectOfType<Roguelike.Flow.RogueRoomFlowController>();

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestart);
            }

            if (campButton != null)
            {
                campButton.onClick.AddListener(OnCamp);
            }

            if (flow != null)
            {
                flow.onSettled += Show;
            }
        }

        /// <summary>弹出结算面板并填充数据。isWin 用于通关/失败文案。</summary>
        public void Show(bool isWin)
        {
            if (resultText != null)
            {
                resultText.text = isWin ? "通关成功" : "冒险失败";
            }

            RunManager.RunSession session = runManager != null ? runManager.Session : new RunManager.RunSession();

            if (fragmentText != null)
            {
                fragmentText.text = "本局碎片：" + session.fragments;
            }

            if (killText != null)
            {
                killText.text = "击杀数：" + session.kills;
            }

            int currency = 0;
            RunCurrencyStore.LoadCurrency(out currency);

            if (crystalText != null)
            {
                crystalText.text = "累计晶核：" + currency;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnRestart()
        {
            if (flow != null)
            {
                flow.StartRun();
                Hide();
            }
            else
            {
                ReloadScene();
            }
        }

        private void OnCamp()
        {
            ReloadScene();
        }

        private static void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
