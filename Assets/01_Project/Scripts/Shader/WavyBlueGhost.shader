Shader "Custom/WavyBlueGhost"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        [Header(Ghost Settings)]
        _GhostColor ("Ghost Color", Color) = (0.2, 0.6, 1.0, 0.8) // 預設為半透明的迷幻藍色
        _WaveSpeed ("Wave Speed", Float) = 5.0                    // 波浪滾動的速度
        _WaveFreq ("Wave Frequency", Float) = 15.0                // 波浪的密集度
        _WaveAmp ("Wave Amplitude", Float) = 0.03                 // 扭曲的幅度 (太大會破圖)
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        // 2D Sprite 標準的 Alpha 混合模式
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _GhostColor;
            float _WaveSpeed;
            float _WaveFreq;
            float _WaveAmp;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                // 接收 SpriteRenderer 傳來的顏色與透明度
                OUT.color = IN.color; 
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 【核心運算】：迷幻波浪扭曲 UV
                float2 uv = IN.texcoord;
                // 利用 UV 的 Y 軸與時間 _Time.y 來產生 Sine 波，並加到 X 軸上產生左右扭曲
                uv.x += sin(uv.y * _WaveFreq + _Time.y * _WaveSpeed) * _WaveAmp;

                // 讀取扭曲後的貼圖像素
                fixed4 texColor = tex2D(_MainTex, uv);

                // 【強制上色】：保留原本貼圖的形狀 (Alpha)，但把 RGB 洗成我們要的藍色
                fixed4 finalColor;
                finalColor.rgb = _GhostColor.rgb;
                finalColor.a = texColor.a * _GhostColor.a;

                // 疊加 SpriteRenderer 原本的設定 (確保 Fade out 等效果正常運作)
                finalColor *= IN.color;

                return finalColor;
            }
            ENDCG
        }
    }
}