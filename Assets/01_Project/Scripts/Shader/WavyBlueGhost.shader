Shader "Custom/WavyBlueGhost"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        [Header(Ghost Settings)]
        // 把這裡的預設值改成偏淺的藍色，相乘後才不會太暗
        _GhostColor ("Ghost Tint Color", Color) = (0.5, 0.8, 1.0, 0.7) 
        _WaveSpeed ("Wave Speed", Float) = 5.0                    
        _WaveFreq ("Wave Frequency", Float) = 15.0                
        _WaveAmp ("Wave Amplitude", Float) = 0.03                 
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
                OUT.color = IN.color; 
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. 迷幻波浪扭曲 UV
                float2 uv = IN.texcoord;
                uv.x += sin(uv.y * _WaveFreq + _Time.y * _WaveSpeed) * _WaveAmp;

                // 2. 讀取扭曲後的貼圖像素 (這裡包含了美術畫的所有線條與明暗細節)
                fixed4 texColor = tex2D(_MainTex, uv);

                // 3. 【核心修改：保留細節並染色】
                // 將原本的圖片顏色與我們的 _GhostColor 進行相乘
                // 白色區域會變成藍色，黑色線條依然保持黑色，完美保留立體感與細節
                fixed4 finalColor = texColor * _GhostColor;

                // 4. 疊加 SpriteRenderer 本身的顏色與透明度設定
                finalColor *= IN.color;

                return finalColor;
            }
            ENDCG
        }
    }
}