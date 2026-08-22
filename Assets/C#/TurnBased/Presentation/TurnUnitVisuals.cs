using UnityEngine;

namespace TurnBased
{
    /// <summary>
    /// 运行时程序化生成单位表现（白块 + 染色），避免依赖美术资产即可在 Play Mode 观察。
    /// 后续里程碑接入真实 Sprite 资产后可移除。
    /// </summary>
    public static class TurnUnitVisuals
    {
        private static Sprite _white;

        public static Sprite WhiteSprite
        {
            get
            {
                if (_white != null) return _white;

                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var pixels = new Color[16 * 16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();

                _white = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
                _white.name = "TurnUnitWhite";
                return _white;
            }
        }
    }
}